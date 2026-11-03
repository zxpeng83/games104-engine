using G104.Engine.Scene;
using G104.Sandbox.Gameplay;
using OpenTK.Mathematics;

namespace G104.Sandbox.Tools;

// 只记录/断言实际窗口经过的路线；输入、固定步、动画、相机、音频和绘制仍走TrainingWindow。
/// <summary>为真实TrainingWindow生成有限接触路线并收集实际固定步/绘制证据，不替代生产模拟。</summary>
internal sealed class ContactWindowExercise
{
    /// <summary>全部路线需要的显示帧预算，总计800帧。</summary>
    public const int RequiredFrames = 800;
    /// <summary>覆盖六个显示球、上坡、下坡和高侧阻挡的顺序路线。</summary>
    private readonly Route[] _routes;
    /// <summary>原始种子场景的序列化基线，每条路线开始前检查设计未变。</summary>
    private readonly string _seedJson;
    /// <summary>_routeIndex为当前路线下标；_firstFrame为起帧；_steps/_renders为实际固定步/绘制次数。</summary>
    /// <remarks>_grounded/_support/_blocked/_moving分别统计接地、目标支撑、阻挡、移动步；_walk/_run统计步行/奔跑步。</remarks>
    private int _routeIndex = -1, _firstFrame, _steps, _renders, _grounded, _support, _blocked, _moving, _walk, _run;
    /// <summary>已完成且通过断言的路线数量。</summary>
    private int _completed;
    /// <summary>_minimumDistance为距目标最小水平米数；_maximumY为最高世界Y；_travel为累计水平米数。</summary>
    /// <remarks>_radius/_height为本路线玩家胶囊半径/高度，单位米。</remarks>
    private float _minimumDistance, _maximumY, _travel, _radius, _height;
    /// <summary>_last为上一观测玩家世界位置；_targetPosition为路线目标世界位置，单位米。</summary>
    private Vector3 _last, _targetPosition;
    /// <summary>本路线是否实际扫掠接触过指定目标。</summary>
    private bool _targetContact;
    /// <summary>当前运行场景中玩家的稳定标识，用于核对模拟位置回写。</summary>
    private Guid _playerId;
    /// <summary>记录玩家/NPC初始设计缩放，逐步检查运行未污染单位缩放。</summary>
    private readonly Dictionary<Guid, Float3> _actorScales = [];

    /// <summary>验证种子场景的显示球/NPC/坡道前提，并按原始坡面计算路线起点。</summary>
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

    /// <summary>所有路线均执行并通过各自结尾断言时为真。</summary>
    public bool Complete => _completed == _routes.Length;

    /// <summary>在路线帧预算到期后验收旧路线并启动下一条；返回是否需要重新准备运行场景。</summary>
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

    /// <summary>确认设计仍等于种子，再复制玩家变换并设为当前路线起点与Y轴朝向。</summary>
    public TransformData StartTransform(SceneDocument design)
    {
        Require(SceneSerializer.Serialize(design) == _seedJson, "Each route must start from the unchanged seed.");
        var route = _routes[_routeIndex];
        var transform = TransformMath.Clone(design.Objects.Single(item => item.Kind == ObjectKind.Player).Transform);
        transform.Position = Float3.From(route.Start);
        transform.Rotation = RotationData.From(Quaternion.FromAxisAngle(Vector3.UnitY, route.StartYaw));
        return transform;
    }

    /// <summary>运行场景准备后记录玩家尺寸/目标/角色缩放，并检查起点胶囊可占据及原NPC存在。</summary>
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

    /// <summary>把世界路线方向映射成生产移动输入，路线后半段切换奔跑。</summary>
    public (Vector2 Move, bool Sprint) Input(int frame)
    {
        if (Complete) return (Vector2.Zero, false);
        var route = _routes[_routeIndex];
        return (new Vector2(route.Direction.X, -route.Direction.Z), frame - _firstFrame >= route.Frames / 2);
    }

    /// <summary>从实际模拟步累计运动/接触事实，并检查场景位置回写、有限矩阵与角色缩放。</summary>
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
        // 向下查目标支撑，再沿路线向前查阻挡，区分显示球穿越与坡面接触。
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

    /// <summary>记录真实完成的显示帧，并在总帧预算末尾验收最后路线。</summary>
    public void Rendered(int frame)
    {
        if (Complete) return;
        _renders++;
        if (frame >= RequiredFrames) FinishRoute();
    }

    /// <summary>要求全部路线完成后输出整体PASS，不用单纯运行时长代替行为证据。</summary>
    public void VerifyComplete()
    {
        Require(Complete, $"Only {_completed}/{_routes.Length} contact routes completed.");
        Console.WriteLine("PASS contact window: 6 visual spheres crossed; original Ramp climbed, descended and blocked its high side; walk/run, player/NPC unit scale and rendered frames verified.");
    }

    /// <summary>检查实际固定步/绘制/步跑覆盖，再按路线类型断言穿越、升降或阻挡结果。</summary>
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

    /// <summary>将未满足的路线前提或行为证据转为失败异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    /// <summary>Sphere检查显示球穿越；Uphill检查上坡；Downhill检查下坡；Side检查坡道高侧阻挡。</summary>
    private enum RouteKind { Sphere, Uphill, Downhill, Side }
    /// <summary>一条定向接触路线的不可变输入配置。</summary>
    /// <param name="Name">日志中使用的路线名称。</param>
    /// <param name="Target">需要观测接触或穿越的场景对象GUID。</param>
    /// <param name="Start">玩家脚底世界起点，单位米。</param>
    /// <param name="Direction">水平世界移动方向。</param>
    /// <param name="Frames">本路线显示帧预算。</param>
    /// <param name="Kind">决定结尾行为断言的路线类型。</param>
    /// <param name="StartYaw">初始Y轴旋转，单位弧度。</param>
    private sealed record Route(string Name, Guid Target, Vector3 Start, Vector3 Direction, int Frames, RouteKind Kind, float StartYaw);
}
