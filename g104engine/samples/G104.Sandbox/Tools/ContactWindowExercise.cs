using G104.Engine.Scene;
using G104.Sandbox.Gameplay;
using OpenTK.Mathematics;

namespace G104.Sandbox.Tools;

// 只记录/断言实际窗口经过的路线；输入、固定步、动画、相机、音频和绘制仍走TrainingWindow。
internal sealed class ContactWindowExercise
{
    public const int RequiredFrames = 800;
    private readonly Route[] _routes;
    private readonly string _seedJson;
    private int _routeIndex = -1, _firstFrame, _steps, _renders, _grounded, _support, _blocked, _moving, _walk, _run;
    private int _completed;
    private float _minimumDistance, _maximumY, _travel, _radius, _height;
    private Vector3 _last, _targetPosition;
    private bool _targetContact;
    private Guid _playerId;
    private readonly Dictionary<Guid, Float3> _actorScales = [];

    public ContactWindowExercise(SceneDocument seed)
    {
        _seedJson = SceneSerializer.Serialize(seed);
        var spheres = seed.Objects.Where(item => item.Name.StartsWith("PBR sample ", StringComparison.Ordinal)).ToArray();
        Require(spheres.Length == 6 && spheres.All(item => item.Collider is null), "Expected six original render-only PBR spheres.");
        Require(seed.Objects.Count(item => item.Kind == ObjectKind.Npc) > 0, "The original NPC must remain active.");
        var routes = new List<Route>();
        foreach (var sphere in spheres)
        {
            var start = sphere.Transform.Position.ToVector() - Vector3.UnitZ * 2.2f; start.Y = 0.05f;
            routes.Add(new Route(sphere.Name + " south walk/run", sphere.Id, start, Vector3.UnitZ, 70, RouteKind.Sphere, 0));
        }
        var ramp = seed.Objects.Single(item => item.Name == "Ramp");
        Require(ramp.Collider is { Kind: ColliderKind.Box }, "The original ramp collider must remain unchanged.");
        var top = Vector3.TransformPosition(ramp.Collider!.Center.ToVector() + Vector3.UnitY * ramp.Collider.Size.Y * 0.5f,
            TransformMath.Compose(ramp.Transform));
        var normal = Vector3.TransformVector(Vector3.UnitY, Matrix4.CreateFromQuaternion(ramp.Transform.Rotation.ToQuaternion())).Normalized();
        float topAtStart = top.Y - (normal.X * (9 - top.X) + normal.Z * (3 - top.Z)) / normal.Y;
        routes.Add(new Route("Ramp uphill south walk/run", ramp.Id, new Vector3(9, 0.05f, -3.6f), Vector3.UnitZ, 120, RouteKind.Uphill, 0));
        routes.Add(new Route("Ramp downhill north walk/run", ramp.Id, new Vector3(9, topAtStart + 0.05f, 3), -Vector3.UnitZ, 120, RouteKind.Downhill, MathF.PI));
        routes.Add(new Route("Ramp high side east walk/run", ramp.Id, new Vector3(6.2f, 0.05f, 2), Vector3.UnitX, 140, RouteKind.Side, 0));
        _routes = routes.ToArray();
        Require(_routes.Sum(route => route.Frames) == RequiredFrames, "Contact route frame budget changed.");
    }

    public bool Complete => _completed == _routes.Length;

    public bool BeginFrame(int frame)
    {
        if (Complete) return false;
        if (_routeIndex >= 0 && frame < _firstFrame + _routes[_routeIndex].Frames) return false;
        if (_routeIndex >= 0) FinishRoute();
        if (Complete) return false;
        _routeIndex++; _firstFrame = frame;
        _steps = _renders = _grounded = _support = _blocked = _moving = _walk = _run = 0;
        _minimumDistance = float.MaxValue; _maximumY = _travel = 0; _targetContact = false;
        return true;
    }

    public TransformData StartTransform(SceneDocument design)
    {
        Require(SceneSerializer.Serialize(design) == _seedJson, "Each route must start from the unchanged seed.");
        var route = _routes[_routeIndex];
        var transform = TransformMath.Clone(design.Objects.Single(item => item.Kind == ObjectKind.Player).Transform);
        transform.Position = Float3.From(route.Start);
        transform.Rotation = RotationData.From(Quaternion.FromAxisAngle(Vector3.UnitY, route.StartYaw));
        return transform;
    }

    public void Prepared(SceneGraph graph, TrainingSimulation simulation)
    {
        var player = graph.Document.Objects.Single(item => item.Kind == ObjectKind.Player);
        _playerId = player.Id; _radius = player.Parameters.GetValueOrDefault("radius", 0.35f); _height = player.Parameters.GetValueOrDefault("height", 1.9f);
        _last = simulation.PlayerPosition; _targetPosition = graph.WorldPosition(_routes[_routeIndex].Target);
        _actorScales.Clear();
        foreach (var actor in graph.Document.Objects.Where(item => item.Kind is ObjectKind.Player or ObjectKind.Npc))
        {
            Require(actor.Transform.Scale == Float3.One, "The seed actors must use exact unit scale.");
            _actorScales.Add(actor.Id, actor.Transform.Scale);
        }
        Require(simulation.NpcState is not null && simulation.World.CanOccupy(_last, _radius, _height), "The original NPC and a legal starting capsule are required.");
        Console.WriteLine($"Contact window begin {_routeIndex + 1}/{_routes.Length}: {_routes[_routeIndex].Name}; start={_last}; target={_routes[_routeIndex].Target}.");
    }

    public (Vector2 Move, bool Sprint) Input(int frame)
    {
        if (Complete) return (Vector2.Zero, false);
        var route = _routes[_routeIndex];
        return (new Vector2(route.Direction.X, -route.Direction.Z), frame - _firstFrame >= route.Frames / 2);
    }

    public void Observe(SceneGraph graph, TrainingSimulation simulation, bool sprint)
    {
        if (Complete) return;
        var route = _routes[_routeIndex]; var state = simulation.PlayerState;
        _steps++; if (sprint) _run++; else _walk++;
        _grounded += state.Grounded ? 1 : 0; _blocked += state.Blocked ? 1 : 0;
        _moving += state.Velocity.Xz.Length > 0.2f ? 1 : 0;
        _travel += (state.Position - _last).Xz.Length; _last = state.Position;
        _maximumY = MathF.Max(_maximumY, state.Position.Y);
        _minimumDistance = MathF.Min(_minimumDistance, (state.Position - _targetPosition).Xz.Length);
        var support = simulation.World.SweepCapsule(state.Position, _radius, _height, -Vector3.UnitY * 0.2f);
        if (support is { } hit && hit.ObjectId == route.Target)
        {
            _targetContact = true;
            if (state.Grounded && hit.Normal.Y > 0.8f) _support++;
        }
        var ahead = simulation.World.SweepCapsule(state.Position, _radius, _height, route.Direction * 0.15f);
        _targetContact |= ahead is { } contact && contact.ObjectId == route.Target;
        Require(TransformMath.Finite(state.Position) && (graph.WorldPosition(_playerId) - state.Position).Length < 1e-5f,
            "The real controller must submit its finite position to SceneGraph.");
        foreach (var actor in graph.Document.Objects.Where(item => item.Kind is ObjectKind.Player or ObjectKind.Npc))
            Require(actor.Transform.Scale == _actorScales[actor.Id] && TransformMath.Finite(graph.WorldMatrix(actor.Id)),
                $"{actor.Name} changed its authored unit scale or produced a nonfinite matrix.");
    }

    public void Rendered(int frame)
    {
        if (Complete) return;
        _renders++;
        if (frame >= RequiredFrames) FinishRoute();
    }

    public void VerifyComplete()
    {
        Require(Complete, $"Only {_completed}/{_routes.Length} contact routes completed.");
        Console.WriteLine("PASS contact window: 6 visual spheres crossed; original Ramp climbed, descended and blocked its high side; walk/run, player/NPC unit scale and rendered frames verified.");
    }

    private void FinishRoute()
    {
        var route = _routes[_routeIndex];
        string metrics = $"{route.Name}; steps={_steps}; renders={_renders}; end={_last}; travel={_travel:F3}; minTarget={_minimumDistance:F3}; maxY={_maximumY:F3}; support={_support}; blocked={_blocked}; walk/run={_walk}/{_run}";
        Console.WriteLine("Contact window observed: " + metrics);
        Require(_steps >= route.Frames - 3 && _renders > 10 && _grounded > 10 && _moving > 10 && _walk > 10 && _run > 10,
            "The route did not execute the actual fixed-step/render walk/run chain: " + metrics);
        bool expected = route.Kind switch
        {
            RouteKind.Sphere => _minimumDistance < 0.1f && _last.Z > _targetPosition.Z + 1.5f && !_targetContact,
            RouteKind.Uphill => _targetContact && _support > 10 && _last.Z > 1.8f && _last.Y > 0.8f,
            RouteKind.Downhill => _support > 10 && _last.Z < -2.8f && _last.Y < 0.35f,
            RouteKind.Side => _targetContact && _blocked > 10 && _last.X is > 7 and < 7.3f && _last.Y < 0.2f,
            _ => false
        };
        Require(expected, "Contact route behavior failed: " + metrics);
        _completed++;
        Console.WriteLine("PASS contact window route: " + route.Name);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private enum RouteKind { Sphere, Uphill, Downhill, Side }
    private sealed record Route(string Name, Guid Target, Vector3 Start, Vector3 Direction, int Frames, RouteKind Kind, float StartYaw);
}
