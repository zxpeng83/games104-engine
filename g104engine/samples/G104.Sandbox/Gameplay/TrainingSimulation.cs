using G104.Engine.Navigation;
using G104.Engine.Physics;
using G104.Engine.Rendering;
using G104.Engine.Scene;
using OpenTK.Mathematics;

namespace G104.Sandbox.Gameplay;

/// <summary>固定逻辑步消费的持续移动状态与一次输入边沿。</summary>
/// <param name="Move">相机相对水平输入，X向右、Y向前，长度超过1时归一化。</param>
/// <param name="Sprint">当前是否请求跑步速度。</param>
/// <param name="Jump">本步一次跳跃请求，是否起跳仍由角色接地规则决定。</param>
/// <param name="Interact">本步一次机关交互请求。</param>
/// <param name="CameraYaw">输入采集时确定的相机水平角，单位弧度。</param>
public readonly record struct GameInput(Vector2 Move, bool Sprint, bool Jump, bool Interact, float CameraYaw);
/// <summary>当前逻辑步的一次玩法事实，声音和粒子在Tick后消费。</summary>
/// <param name="Sound">音频资源键。</param>
/// <param name="Position">事实发生的世界位置。</param>
/// <param name="Color">粒子反馈使用的颜色。</param>
/// <param name="Particles">是否同时产生粒子反馈。</param>
public readonly record struct SimulationEvent(string Sound, Vector3 Position, Vector4 Color, bool Particles);
/// <summary>NPC决策模式：Patrol巡逻；Follow跟随可见或记忆位置；Search搜索；Return返回巡逻原点。</summary>
public enum NpcMode { Patrol, Follow, Search, Return }

// 专属玩法只依赖Engine查询和角色规则；运行场景应是设计Document的独立副本。
/// <summary>在独立运行场景上协调玩家、NPC、门和目标事实，拥有本轮物理世界。</summary>
public sealed class TrainingSimulation : IDisposable
{
    /// <summary>调用者提供的运行文档引用，运行场景应与设计文档隔离。</summary>
    private readonly SceneDocument _document;
    /// <summary>运行态逻辑场景图，接受控制器权威位置和机关姿态提交。</summary>
    private readonly SceneGraph _graph;
    /// <summary>场景唯一玩家的运行数据，用于参数读取和逻辑姿态提交。</summary>
    private readonly SceneObjectData _playerObject;
    /// <summary>可选首个NPC运行数据，不实现群体决策。</summary>
    private readonly SceneObjectData? _npcObject;
    /// <summary>可选交互按钮，仅其显式TargetId可控制门。</summary>
    private readonly SceneObjectData? _button;
    /// <summary>由按钮显式引用的有效门对象；无引用时交互拒绝。</summary>
    private readonly SceneObjectData? _door;
    /// <summary>可选目标触发对象，用于锁存本轮完成状态。</summary>
    private readonly SceneObjectData? _goal;
    /// <summary>玩家权威脚底和接触事实的唯一角色控制器。</summary>
    private readonly KinematicCharacter _player;
    /// <summary>NPC使用的同一运动学角色规则，执行上一逻辑步生成的意图。</summary>
    private readonly KinematicCharacter? _npc;
    /// <summary>NPC平面路径网格，与门开闭使用同一物理碰撞事实。</summary>
    private readonly NavigationGrid? _navigation;
    /// <summary>运行开始时的NPC世界脚底，作为巡逻基准与返回目标。</summary>
    private readonly Vector3 _npcHome;
    /// <summary>门关闭时的世界显示位置，开门上移仍保留此基准。</summary>
    private readonly Vector3 _closedDoorPosition;
    /// <summary>仅保存当前Tick事实，下个Tick开始前清空，使用者应及时消费。</summary>
    private readonly List<SimulationEvent> _events = [];
    /// <summary>启动时从可走格选取并去重的巡逻目标。</summary>
    private readonly List<Vector3> _patrolTargets = [];
    /// <summary>最近一次真实看见玩家时记下的位置，不在遮挡后跟踪实时坐标。</summary>
    private Vector3 _lastSeen;
    /// <summary>NPC下一固定步执行的期望世界水平速度，单位米/秒。</summary>
    private Vector3 _npcIntent;
    /// <summary>最近一次搜索的目标位置，移动足够距离后触发重规划。</summary>
    private Vector3 _pathGoal;
    /// <summary>NPC当前搜索结果及所用网格版本。</summary>
    private NavigationPath? _path;
    /// <summary>路径执行中下一个尚未到达的路点索引。</summary>
    private int _pathCursor;
    /// <summary>当前巡逻目标索引，到达后循环切换。</summary>
    private int _patrolCursor;
    /// <summary>距离下一次感知采样的秒数，采样周期为0.2秒。</summary>
    private float _senseTimer;
    /// <summary>距离下一次周期重规划的秒数，受阻或模式切换时归零。</summary>
    private float _pathTimer;
    /// <summary>自上次看见玩家以来的秒数，用于跟随记忆超时。</summary>
    private float _lostTimer;
    /// <summary>当前搜索模式已持续的秒数，超时切返回模式。</summary>
    private float _searchTimer;
    /// <summary>有移动意图却缺少实际水平进展的累计秒数。</summary>
    private float _stuckTimer;
    /// <summary>玩家玩法脚步音效倒计时，与渲染动画事件队列分开。</summary>
    private float _footstepTimer;
    /// <summary>幂等释放标记，阻止释放后继续推进模拟。</summary>
    private bool _disposed;

    /// <summary>定位角色和显式机关关系，创建物理及可选导航；准备失败释放已创建世界。</summary>
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

    /// <summary>本次模拟拥有的物理世界，随Dispose确定性释放。</summary>
    public PhysicsWorld World { get; }
    /// <summary>玩家控制器当前世界脚底，单位米。</summary>
    public Vector3 PlayerPosition => _player.State.Position;
    /// <summary>玩家最近一次逻辑步的实际运动与接触结果。</summary>
    public CharacterState PlayerState => _player.State;
    /// <summary>可选NPC控制器状态，无NPC时为null。</summary>
    public CharacterState? NpcState => _npc?.State;
    /// <summary>当前感知和记忆驱动的NPC决策模式。</summary>
    public NpcMode NpcMode { get; private set; } = NpcMode.Patrol;
    /// <summary>当前路径执行状态，区分到达、角色受阻和寻路失败。</summary>
    public PathTaskState NpcTask { get; private set; } = PathTaskState.Idle;
    /// <summary>最近一次路径搜索的结果；尚无路径时为null。</summary>
    public NavigationResult? NpcNavigationResult => _path?.Result;
    /// <summary>最近一次NPC模式或路径异常的调试说明。</summary>
    public string NpcReason { get; private set; } = "Patrol route";
    /// <summary>当前实际搜索路径的世界路点，供窗口调试绘制。</summary>
    public IReadOnlyList<Vector3> NpcPath => _path?.Waypoints ?? [];
    /// <summary>公开可选NPC导航网格，供诊断与验证读取。</summary>
    public NavigationGrid? Navigation => _navigation;
    /// <summary>已提交的门状态，与碰撞停启和导航重建在同一交互边界更新。</summary>
    public bool DoorOpen { get; private set; }
    /// <summary>本轮目标完成锁存位，只在重新创建模拟时复位。</summary>
    public bool Completed { get; private set; }
    /// <summary>当前训练提示或最近交互结果，供界面展示。</summary>
    public string Feedback { get; private set; } = "Reach the button, open the door, then enter the goal.";
    /// <summary>当前Tick事实的只读视图，下次Tick会清空原列表。</summary>
    public IReadOnlyList<SimulationEvent> Events => _events;

    /// <summary>按固定秒步执行交互、物理和角色，再生成NPC下一步意图及一次玩法反馈。</summary>
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
        // 相机yaw构建世界水平右/前向，保持局部前方为-Z。
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

    /// <summary>从实际角色速度、接地和竖直速度生成只读动画输入，其他对象使用静止默认值。</summary>
    public AnimationInputs AnimationFor(Guid id)
    {
        CharacterState? state = id == _playerObject.Id ? _player.State : id == _npcObject?.Id ? _npc?.State : null;
        return state is { } value ? new AnimationInputs(value.Speed, value.Grounded, value.VerticalVelocity) : new AnimationInputs(0, true, 0);
    }

    /// <summary>将控制器脚底提交到逻辑场景，并以运动意图平滑朝向，不改变角色单位缩放。</summary>
    private void SubmitCharacter(Guid id, CharacterState state, Vector3 intent, float dt)
    {
        _graph.SetWorldPosition(id, state.Position);
        var horizontal = new Vector2(intent.X, intent.Z);
        if (horizontal.LengthSquared < 0.0025f) return;
        var desiredYaw = MathF.Atan2(-intent.X, -intent.Z);
        var desired = Quaternion.FromAxisAngle(Vector3.UnitY, desiredYaw);
        // Player/Npc由场景校验强制为根节点；直接读取已存四元数，避免每帧从矩阵反求近180度旋转。
        var current = TransformMath.Normalize(_graph.Object(id).Transform.Rotation.ToQuaternion());
        _graph.SetWorldRotation(id, Quaternion.Slerp(current, desired, Math.Clamp(dt * 12, 0, 1)));
    }

    /// <summary>校验按钮引用、距离、视线和关闭占用后同步提交门画面、碰撞及导航。</summary>
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
        // 门状态成功后再使导航和旧路径一起失效，下一次NPC决策重新搜索。
        _navigation?.Rebuild();
        _path = null; _pathTimer = 0; _npcIntent = Vector3.Zero;
        Feedback = DoorOpen ? "Door opened. Enter the goal zone." : "Door closed.";
        Emit("button", buttonPosition, new Vector4(0.15f, 0.9f, 0.5f, 1), true);
        Emit("door", _closedDoorPosition, new Vector4(0.2f, 0.8f, 1, 1), true);
    }

    /// <summary>存在有效关联门时要求其已打开，再检查Box或水平半径触发范围；完成只锁存并发布一次事实。</summary>
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

    /// <summary>按当前世界感知更新FSM与路径任务，生成下一步角色水平速度意图。</summary>
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
        // 目标移动、网格版本变化、路径失效或重规划计时到期均重新查询。
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
        // 消费已到达的路径点；意图仍由下一步角色查询执行，路径本身不直接改位置。
        while (_pathCursor < _path.Waypoints.Count && (_path.Waypoints[_pathCursor] - _npc.State.Position).Xz.Length < 0.22f) _pathCursor++;
        if (_pathCursor >= _path.Waypoints.Count) { _npcIntent = Vector3.Zero; _pathTimer = 0; return; }
        var direction = _path.Waypoints[_pathCursor] - _npc.State.Position; direction.Y = 0;
        if (direction.LengthSquared < 1e-8f) { _npcIntent = Vector3.Zero; return; }
        var speed = Parameter(_npcObject, "walkSpeed", 2.3f);
        if (NpcMode == NpcMode.Follow) speed = MathF.Min(Parameter(_npcObject, "runSpeed", 3.8f), MathF.Max(0.5f, (distance - arrival) * 2));
        _npcIntent = direction.Normalized() * speed;
        if (NpcTask != PathTaskState.Blocked) NpcTask = PathTaskState.Following;
    }

    /// <summary>按距离、当前逻辑-Z朝向的水平视锥及物理遮挡判断玩家可见性。</summary>
    private bool CanSeePlayer()
    {
        if (_npc is null || _npcObject is null) return false;
        var toPlayer = PlayerPosition - _npc.State.Position;
        var horizontal = toPlayer.Xz;
        var range = Parameter(_npcObject, "sightRange", 8);
        if (toPlayer.Length > range) return false;
        // 感知读取本步已提交的逻辑朝向；保留设计旋转，并随转向Slerp逐步改变视锥。
        var forward = Vector3.TransformVector(-Vector3.UnitZ, _graph.WorldMatrix(_npcObject.Id)).Xz;
        if (horizontal.Length > 1.8f && (forward.LengthSquared < 1e-8f ||
            Vector2.Dot(horizontal.Normalized(), forward.Normalized()) < MathF.Cos(Parameter(_npcObject, "fieldOfView", 150) * MathF.PI / 360))) return false;
        var origin = _npc.State.Position + Vector3.UnitY * 1.45f;
        var direction = PlayerPosition + Vector3.UnitY * 1.1f - origin;
        return World.Raycast(origin, direction, direction.Length) is not { } hit || hit.Distance >= direction.Length - 0.05f;
    }

    /// <summary>切决策模式并清旧路径和移动意图，保留同模式的现有任务。</summary>
    private void SetNpcMode(NpcMode mode, string reason)
    {
        if (NpcMode == mode) return;
        NpcMode = mode; NpcReason = reason; _path = null; _pathTimer = 0; _npcIntent = Vector3.Zero; NpcTask = PathTaskState.Idle;
    }
    /// <summary>把巡逻候选投到最近可走格并去除近重复点，不额外创建导航区域。</summary>
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
    /// <summary>从对象参数生成角色几何与运动规则，未开放项保留控制器默认值。</summary>
    private static CharacterSettings CharacterParameters(SceneObjectData item) => new()
    {
        Radius = Parameter(item, "radius", 0.35f), Height = Parameter(item, "height", 1.9f),
        StepHeight = Parameter(item, "stepHeight", 0.35f), MaxSlopeDegrees = Parameter(item, "maxSlopeDegrees", 45),
        Gravity = Parameter(item, "gravity", 18), JumpSpeed = Parameter(item, "jumpSpeed", 6)
    };
    /// <summary>读取运行对象参数，未设置时采用该调用处的既定默认值。</summary>
    private static float Parameter(SceneObjectData item, string name, float fallback) => item.Parameters.GetValueOrDefault(name, fallback);
    /// <summary>向本Tick事实列表追加音效与可选粒子描述，不在模拟中直接播放资源。</summary>
    private void Emit(string sound, Vector3 position, Vector4 color, bool particles) => _events.Add(new SimulationEvent(sound, position, color, particles));
    /// <summary>清理当前事实并释放本模拟拥有的物理世界，可重复调用。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _events.Clear(); World.Dispose();
    }
}
