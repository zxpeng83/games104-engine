using G104.Engine.Navigation;
using G104.Engine.Physics;
using G104.Engine.Rendering;
using G104.Engine.Scene;
using OpenTK.Mathematics;

namespace G104.Sandbox.Gameplay;

public readonly record struct GameInput(Vector2 Move, bool Sprint, bool Jump, bool Interact, float CameraYaw);
public readonly record struct SimulationEvent(string Sound, Vector3 Position, Vector4 Color, bool Particles);
public enum NpcMode { Patrol, Follow, Search, Return }

// 专属玩法只依赖Engine查询和角色规则；运行场景应是设计Document的独立副本。
public sealed class TrainingSimulation : IDisposable
{
    private readonly SceneDocument _document;
    private readonly SceneGraph _graph;
    private readonly SceneObjectData _playerObject;
    private readonly SceneObjectData? _npcObject;
    private readonly SceneObjectData? _button;
    private readonly SceneObjectData? _door;
    private readonly SceneObjectData? _goal;
    private readonly KinematicCharacter _player;
    private readonly KinematicCharacter? _npc;
    private readonly NavigationGrid? _navigation;
    private readonly Vector3 _npcHome;
    private readonly Vector3 _closedDoorPosition;
    private readonly List<SimulationEvent> _events = [];
    private readonly List<Vector3> _patrolTargets = [];
    private Vector3 _lastSeen;
    private Vector3 _npcIntent;
    private Vector3 _pathGoal;
    private NavigationPath? _path;
    private int _pathCursor;
    private int _patrolCursor;
    private float _senseTimer;
    private float _pathTimer;
    private float _lostTimer;
    private float _searchTimer;
    private float _stuckTimer;
    private float _footstepTimer;
    private float _npcFacing;
    private bool _disposed;

    public TrainingSimulation(SceneDocument document, SceneGraph graph)
    {
        _document = document;
        _graph = graph;
        _playerObject = document.Objects.SingleOrDefault(item => item.Kind == ObjectKind.Player) ??
            throw new InvalidOperationException("Play requires one Player object.");
        _npcObject = document.Objects.FirstOrDefault(item => item.Kind == ObjectKind.Npc);
        _button = document.Objects.FirstOrDefault(item => item.Kind == ObjectKind.Button);
        // 显式引用是唯一链接；编辑器选择<none>后不能隐式改控场景中的第一扇门。
        _door = _button?.TargetId is Guid target ? document.Objects.FirstOrDefault(item => item.Id == target && item.Kind == ObjectKind.Door) : null;
        _goal = document.Objects.FirstOrDefault(item => item.Kind == ObjectKind.Goal);
        _closedDoorPosition = _door is null ? Vector3.Zero : graph.WorldPosition(_door.Id);
        World = new PhysicsWorld(document, graph);
        try
        {
            _player = new KinematicCharacter(World, graph.WorldPosition(_playerObject.Id), CharacterParameters(_playerObject));
            if (_npcObject is not null)
            {
                _npcHome = graph.WorldPosition(_npcObject.Id);
                _npc = new KinematicCharacter(World, _npcHome, CharacterParameters(_npcObject));
                _navigation = new NavigationGrid(World, new NavigationSettings
                {
                    Minimum = new Vector2(Parameter(_npcObject, "navMinX", -12), Parameter(_npcObject, "navMinZ", -12)),
                    Maximum = new Vector2(Parameter(_npcObject, "navMaxX", 12), Parameter(_npcObject, "navMaxZ", 12)),
                    PlaneY = Parameter(_npcObject, "navPlaneY", 0), CellSize = Parameter(_npcObject, "navCellSize", 0.65f),
                    AgentRadius = _npc.Settings.Radius, AgentHeight = _npc.Settings.Height
                });
                CreatePatrolTargets();
            }
        }
        catch { World.Dispose(); throw; }
    }

    public PhysicsWorld World { get; }
    public Vector3 PlayerPosition => _player.State.Position;
    public CharacterState PlayerState => _player.State;
    public CharacterState? NpcState => _npc?.State;
    public NpcMode NpcMode { get; private set; } = NpcMode.Patrol;
    public PathTaskState NpcTask { get; private set; } = PathTaskState.Idle;
    public NavigationResult? NpcNavigationResult => _path?.Result;
    public string NpcReason { get; private set; } = "Patrol route";
    public IReadOnlyList<Vector3> NpcPath => _path?.Waypoints ?? [];
    public NavigationGrid? Navigation => _navigation;
    public bool DoorOpen { get; private set; }
    public bool Completed { get; private set; }
    public string Feedback { get; private set; } = "Reach the button, open the door, then enter the goal.";
    public IReadOnlyList<SimulationEvent> Events => _events;

    public void Tick(float dt, GameInput input)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!float.IsFinite(dt) || dt <= 0 || dt > 0.1f) throw new ArgumentOutOfRangeException(nameof(dt));
        _events.Clear();
        // 机关命令先提交；NPC执行上一模拟步已确定的意图，之后感知/决策产生下一步意图。
        if (input.Interact) Interact();
        World.Step(dt);
        var movement = input.Move;
        if (movement.LengthSquared > 1) movement.Normalize();
        var sin = MathF.Sin(input.CameraYaw); var cos = MathF.Cos(input.CameraYaw);
        var right = new Vector3(cos, 0, -sin);
        var forward = new Vector3(-sin, 0, -cos);
        var playerDirection = right * movement.X + forward * movement.Y;
        var playerSpeed = Parameter(_playerObject, input.Sprint ? "runSpeed" : "walkSpeed", input.Sprint ? 5.5f : 3);
        var playerState = _player.Tick(dt, playerDirection * playerSpeed, input.Jump);
        SubmitCharacter(_playerObject.Id, playerState, playerDirection, dt);
        if (_npc is not null && _npcObject is not null)
        {
            var previous = _npc.State.Position;
            var npcState = _npc.Tick(dt, _npcIntent, false);
            SubmitCharacter(_npcObject.Id, npcState, _npcIntent, dt);
            if (_npcIntent.LengthSquared > 0.1f && (npcState.Position - previous).Xz.Length < 0.1f * dt) _stuckTimer += dt;
            else _stuckTimer = MathF.Max(0, _stuckTimer - dt * 2);
            if (_stuckTimer > 1.2f)
            {
                NpcTask = PathTaskState.Blocked; NpcReason = "Character movement is blocked; replanning";
                _pathTimer = 0; _stuckTimer = 0;
            }
            UpdateNpc(dt);
        }
        if (playerState.Jumped) Emit("jump", PlayerPosition, new Vector4(0.7f, 0.85f, 1, 1), false);
        if (playerState.Landed) Emit("land", PlayerPosition, new Vector4(0.65f, 0.75f, 0.85f, 1), true);
        if (playerState.Grounded && playerState.Speed > 0.3f)
        {
            _footstepTimer -= dt;
            if (_footstepTimer <= 0) { Emit("footstep", PlayerPosition, Vector4.One, false); _footstepTimer = playerState.Speed > 4 ? 0.28f : 0.42f; }
        }
        else _footstepTimer = 0;
        CheckGoal();
    }

    public AnimationInputs AnimationFor(Guid id)
    {
        CharacterState? state = id == _playerObject.Id ? _player.State : id == _npcObject?.Id ? _npc?.State : null;
        return state is { } value ? new AnimationInputs(value.Speed, value.Grounded, value.VerticalVelocity) : new AnimationInputs(0, true, 0);
    }

    private void SubmitCharacter(Guid id, CharacterState state, Vector3 intent, float dt)
    {
        _graph.SetWorldPosition(id, state.Position);
        var horizontal = new Vector2(intent.X, intent.Z);
        if (horizontal.LengthSquared < 0.0025f) return;
        var desiredYaw = MathF.Atan2(-intent.X, -intent.Z);
        var desired = Quaternion.FromAxisAngle(Vector3.UnitY, desiredYaw);
        var current = _graph.WorldMatrix(id).ExtractRotation();
        _graph.SetWorldRotation(id, Quaternion.Slerp(current, desired, Math.Clamp(dt * 12, 0, 1)));
        if (id == _npcObject?.Id) _npcFacing = desiredYaw;
    }

    private void Interact()
    {
        if (_button is null || _door is null)
        {
            Feedback = "The button has no valid Door target.";
            Emit("denied", PlayerPosition, new Vector4(1, 0.25f, 0.15f, 1), false);
            return;
        }
        var buttonPosition = _graph.WorldPosition(_button.Id);
        var reach = Parameter(_button, "interactDistance", 2);
        if ((buttonPosition - (PlayerPosition + Vector3.UnitY * 0.8f)).Length > reach)
        {
            Feedback = "Move closer to the button.";
            Emit("denied", PlayerPosition, new Vector4(1, 0.5f, 0.15f, 1), false);
            return;
        }
        var origin = PlayerPosition + Vector3.UnitY * 0.8f;
        var toButton = buttonPosition - origin;
        if (toButton.Length > 0.05f && World.Raycast(origin, toButton, toButton.Length) is { } obstruction &&
            obstruction.ObjectId != _button.Id && obstruction.Distance < toButton.Length - 0.15f)
        {
            Feedback = "The button is behind an obstacle.";
            Emit("denied", PlayerPosition, new Vector4(1, 0.5f, 0.15f, 1), false);
            return;
        }
        if (DoorOpen)
        {
            // 先暂时启用原位碰撞并检查角色外壳；失败恢复，不发布关闭事实。
            World.SetEnabled(_door.Id, true);
            var occupied = World.OverlapCapsule(PlayerPosition, _player.Settings.Radius, _player.Settings.Height).Any(hit => hit.ObjectId == _door.Id && hit.Depth > 0.002f);
            if (_npc is not null) occupied |= World.OverlapCapsule(_npc.State.Position, _npc.Settings.Radius, _npc.Settings.Height).Any(hit => hit.ObjectId == _door.Id && hit.Depth > 0.002f);
            if (occupied)
            {
                World.SetEnabled(_door.Id, false);
                Feedback = "The doorway is occupied; closing was rejected.";
                Emit("denied", buttonPosition, new Vector4(1, 0.4f, 0.1f, 1), false);
                return;
            }
            _graph.SetWorldPosition(_door.Id, _closedDoorPosition);
            DoorOpen = false;
        }
        else
        {
            _graph.SetWorldPosition(_door.Id, _closedDoorPosition + Vector3.UnitY * Parameter(_door, "openHeight", 3.5f));
            World.SetEnabled(_door.Id, false);
            DoorOpen = true;
        }
        _navigation?.Rebuild();
        _path = null; _pathTimer = 0; _npcIntent = Vector3.Zero;
        Feedback = DoorOpen ? "Door opened. Enter the goal zone." : "Door closed.";
        Emit("button", buttonPosition, new Vector4(0.15f, 0.9f, 0.5f, 1), true);
        Emit("door", _closedDoorPosition, new Vector4(0.2f, 0.8f, 1, 1), true);
    }

    private void CheckGoal()
    {
        if (Completed || _goal is null || (_door is not null && !DoorOpen)) return;
        bool inside;
        if (_goal.Collider is { Kind: ColliderKind.Box } trigger)
        {
            var local = Vector3.TransformPosition(PlayerPosition + Vector3.UnitY * 0.1f, Matrix4.Invert(_graph.WorldMatrix(_goal.Id))) - trigger.Center.ToVector();
            var half = trigger.Size.ToVector() * 0.5f;
            inside = MathF.Abs(local.X) <= half.X && MathF.Abs(local.Y) <= half.Y && MathF.Abs(local.Z) <= half.Z;
        }
        else
        {
            var distance = PlayerPosition - _graph.WorldPosition(_goal.Id);
            inside = distance.Xz.Length <= Parameter(_goal, "triggerRadius", Parameter(_goal, "radius", 1.3f)) && MathF.Abs(distance.Y) < 2;
        }
        if (!inside) return;
        Completed = true; Feedback = "Training complete! Stop and Play to repeat.";
        Emit("complete", PlayerPosition + Vector3.UnitY, new Vector4(0.2f, 1, 0.6f, 1), true);
    }

    private void UpdateNpc(float dt)
    {
        if (_npc is null || _npcObject is null || _navigation is null) return;
        _senseTimer -= dt; _pathTimer -= dt;
        _lostTimer += dt;
        if (_senseTimer <= 0)
        {
            _senseTimer = 0.2f;
            if (CanSeePlayer())
            {
                _lastSeen = PlayerPosition; _lostTimer = 0; _searchTimer = 0;
                SetNpcMode(NpcMode.Follow, "Player is visible");
            }
            else if (NpcMode == NpcMode.Follow && _lostTimer > Parameter(_npcObject, "memoryTime", 1.2f))
            {
                _searchTimer = 0; SetNpcMode(NpcMode.Search, "Lost sight; search the last seen position");
            }
        }
        if (NpcMode == NpcMode.Search)
        {
            _searchTimer += dt;
            if (_searchTimer > Parameter(_npcObject, "searchTime", 4)) SetNpcMode(NpcMode.Return, "Search expired; return to patrol origin");
        }
        var target = NpcMode switch
        {
            NpcMode.Follow or NpcMode.Search => _lastSeen,
            NpcMode.Return => _npcHome,
            _ => _patrolTargets.Count > 0 ? _patrolTargets[_patrolCursor] : _npcHome
        };
        var distance = (target - _npc.State.Position).Xz.Length;
        var arrival = NpcMode == NpcMode.Follow ? Parameter(_npcObject, "followDistance", 1.7f) : 0.3f;
        if (distance <= arrival)
        {
            _npcIntent = Vector3.Zero; NpcTask = PathTaskState.Arrived;
            if (NpcMode == NpcMode.Patrol && _patrolTargets.Count > 0)
            { _patrolCursor = (_patrolCursor + 1) % _patrolTargets.Count; _path = null; }
            else if (NpcMode == NpcMode.Return) SetNpcMode(NpcMode.Patrol, "Returned to patrol origin");
            return;
        }
        var goalChanged = (target - _pathGoal).Xz.Length > 0.65f;
        if (_path is null || _path.Revision != _navigation.Revision || goalChanged || _pathTimer <= 0)
        {
            _path = _navigation.FindPath(_npc.State.Position, target);
            _pathGoal = target; _pathCursor = 0;
            _pathTimer = _path.Result == NavigationResult.Success ? 0.8f : 1.2f;
            if (_path.Result != NavigationResult.Success)
            { NpcTask = PathTaskState.NoPath; NpcReason = $"Navigation result: {_path.Result}"; _npcIntent = Vector3.Zero; return; }
        }
        if (_path.Result != NavigationResult.Success) { _npcIntent = Vector3.Zero; return; }
        while (_pathCursor < _path.Waypoints.Count && (_path.Waypoints[_pathCursor] - _npc.State.Position).Xz.Length < 0.22f) _pathCursor++;
        if (_pathCursor >= _path.Waypoints.Count) { _npcIntent = Vector3.Zero; _pathTimer = 0; return; }
        var direction = _path.Waypoints[_pathCursor] - _npc.State.Position; direction.Y = 0;
        if (direction.LengthSquared < 1e-8f) { _npcIntent = Vector3.Zero; return; }
        var speed = Parameter(_npcObject, "walkSpeed", 2.3f);
        if (NpcMode == NpcMode.Follow) speed = MathF.Min(Parameter(_npcObject, "runSpeed", 3.8f), MathF.Max(0.5f, (distance - arrival) * 2));
        _npcIntent = direction.Normalized() * speed;
        if (NpcTask != PathTaskState.Blocked) NpcTask = PathTaskState.Following;
    }

    private bool CanSeePlayer()
    {
        if (_npc is null || _npcObject is null) return false;
        var toPlayer = PlayerPosition - _npc.State.Position;
        var horizontal = toPlayer.Xz;
        var range = Parameter(_npcObject, "sightRange", 8);
        if (toPlayer.Length > range) return false;
        var forward = new Vector2(-MathF.Sin(_npcFacing), -MathF.Cos(_npcFacing));
        if (horizontal.Length > 1.8f && Vector2.Dot(horizontal.Normalized(), forward) < MathF.Cos(Parameter(_npcObject, "fieldOfView", 150) * MathF.PI / 360)) return false;
        var origin = _npc.State.Position + Vector3.UnitY * 1.45f;
        var direction = PlayerPosition + Vector3.UnitY * 1.1f - origin;
        return World.Raycast(origin, direction, direction.Length) is not { } hit || hit.Distance >= direction.Length - 0.05f;
    }

    private void SetNpcMode(NpcMode mode, string reason)
    {
        if (NpcMode == mode) return;
        NpcMode = mode; NpcReason = reason; _path = null; _pathTimer = 0; _npcIntent = Vector3.Zero; NpcTask = PathTaskState.Idle;
    }
    private void CreatePatrolTargets()
    {
        if (_navigation is null || _npcObject is null) return;
        var radius = Parameter(_npcObject, "patrolRadius", 3);
        var candidates = new[] { _npcHome, _npcHome + Vector3.UnitX * radius, _npcHome + Vector3.UnitZ * radius, _npcHome - Vector3.UnitX * radius };
        foreach (var candidate in candidates)
        {
            var nearest = _navigation.WalkableCells().OrderBy(point => (point - candidate).LengthSquared).FirstOrDefault();
            if ((_navigation.WalkableCount > 0) && !_patrolTargets.Any(existing => (existing - nearest).Length < 0.4f)) _patrolTargets.Add(nearest);
        }
    }
    private static CharacterSettings CharacterParameters(SceneObjectData item) => new()
    {
        Radius = Parameter(item, "radius", 0.35f), Height = Parameter(item, "height", 1.9f),
        StepHeight = Parameter(item, "stepHeight", 0.35f), MaxSlopeDegrees = Parameter(item, "maxSlopeDegrees", 45),
        Gravity = Parameter(item, "gravity", 18), JumpSpeed = Parameter(item, "jumpSpeed", 6)
    };
    private static float Parameter(SceneObjectData item, string name, float fallback) => item.Parameters.GetValueOrDefault(name, fallback);
    private void Emit(string sound, Vector3 position, Vector4 color, bool particles) => _events.Add(new SimulationEvent(sound, position, color, particles));
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _events.Clear(); World.Dispose();
    }
}
