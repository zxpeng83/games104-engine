using OpenTK.Mathematics;

namespace G104.Engine.Physics;

/// <summary>自研运动学角色规则的参数；尺寸和距离使用米，时间使用秒。</summary>
public sealed class CharacterSettings
{
    /// <summary>查询胶囊半径，必须大于接触Skin。</summary>
    public float Radius { get; set; } = 0.35f;
    /// <summary>胶囊含两端半球的总高度，必须大于两倍半径。</summary>
    public float Height { get; set; } = 1.9f;
    /// <summary>接地水平运动遇阻后允许抬升的最大台阶高度。</summary>
    public float StepHeight { get; set; } = 0.35f;
    /// <summary>可行走坡面与水平面的最大夹角，单位度。</summary>
    public float MaxSlopeDegrees { get; set; } = 45;
    /// <summary>角色向下加速度的正值大小，单位米/秒平方。</summary>
    public float Gravity { get; set; } = 18;
    /// <summary>接地跳跃时赋予的向上速度，单位米/秒。</summary>
    public float JumpSpeed { get; set; } = 6;
    /// <summary>扫掠停止和接地吸附保留的接触间隙。</summary>
    public float Skin { get; set; } = 0.025f;
    /// <summary>下降或接地时向下寻找支撑的吸附距离。</summary>
    public float GroundSnap { get; set; } = 0.12f;
}

/// <summary>单个逻辑步的角色结果，位置来自查询控制器，动画只读取这些事实。</summary>
/// <param name="Position">世界空间逻辑脚底位置，单位米。</param>
/// <param name="Velocity">实际水平位移速度及控制器竖直速度，单位米/秒。</param>
/// <param name="Grounded">本步是否获得可走支撑。</param>
/// <param name="GroundNormal">接地支撑的世界法线；无支撑时为世界+Y。</param>
/// <param name="Jumped">本步接地跳跃请求是否实际起跳。</param>
/// <param name="Landed">本步是否从未接地转为接地。</param>
/// <param name="Blocked">本步水平运动是否遇阻或进展不足。</param>
public readonly record struct CharacterState(Vector3 Position, Vector3 Velocity, bool Grounded, Vector3 GroundNormal,
    bool Jumped, bool Landed, bool Blocked)
{
    /// <summary>实际水平位移反馈的速度大小，单位米/秒。</summary>
    public float Speed => new Vector2(Velocity.X, Velocity.Z).Length;
    /// <summary>控制器累计的竖直速度，避免Skin修正伪装动画运动。</summary>
    public float VerticalVelocity => Velocity.Y;
}

// feet是逻辑脚底；Jolt只做查询，跨步、坡度、墙滑和跳跃都由本类决策。
/// <summary>以Jolt精确查询执行穿透恢复、扫掠墙滑、坡台及跳跃规则，位置以脚底为准。</summary>
public sealed class KinematicCharacter
{
    /// <summary>借用场景物理世界执行查询，角色不拥有或释放世界。</summary>
    private readonly PhysicsWorld _world;
    /// <summary>独立积分的竖直速度，接地或撞顶时清零，单位米/秒。</summary>
    private float _verticalVelocity;
    /// <summary>本角色使用的可变规则参数，构造时校验初始值。</summary>
    public CharacterSettings Settings { get; }
    /// <summary>最近一次Tick或Teleport提交的权威逻辑状态。</summary>
    public CharacterState State { get; private set; }
    /// <summary>将最大坡角换算为法线Y下限，用于支撑和墙面分类。</summary>
    private float WalkableY => MathF.Cos(Settings.MaxSlopeDegrees * MathF.PI / 180);

    /// <summary>校验参数并建立初始未接地状态，不把查询胶囊注册为刚体。</summary>
    public KinematicCharacter(PhysicsWorld world, Vector3 feet, CharacterSettings? settings = null)
    {
        _world = world;
        Settings = settings ?? new CharacterSettings();
        var values = new[] { Settings.Radius, Settings.Height, Settings.StepHeight, Settings.MaxSlopeDegrees, Settings.Gravity, Settings.JumpSpeed, Settings.Skin, Settings.GroundSnap };
        if (values.Any(value => !float.IsFinite(value)) || Settings.Radius <= 0 || Settings.Height <= Settings.Radius * 2 || Settings.StepHeight < 0 ||
            Settings.MaxSlopeDegrees is < 0 or > 80 || Settings.Gravity <= 0 || Settings.JumpSpeed < 0 || Settings.Skin <= 0 || Settings.Skin >= Settings.Radius || Settings.GroundSnap < 0)
            throw new ArgumentOutOfRangeException(nameof(settings));
        State = new CharacterState(feet, Vector3.Zero, false, Vector3.UnitY, false, false, false);
    }

    /// <summary>直接设置逻辑脚底并清竖直速度及瞬时事实，后续Tick重新探测接地。</summary>
    public void Teleport(Vector3 feet)
    {
        _verticalVelocity = 0;
        State = new CharacterState(feet, Vector3.Zero, false, Vector3.UnitY, false, false, false);
    }

    /// <summary>每固定步消费期望水平米/秒速度与跳跃请求，提交实际位置及接触事实。</summary>
    public CharacterState Tick(float dt, Vector3 desiredHorizontalVelocity, bool jump)
    {
        if (!float.IsFinite(dt) || dt <= 0 || dt > 0.1f) throw new ArgumentOutOfRangeException(nameof(dt));
        // 先恢复已有穿透，再探测支撑；只有接地状态接受跳跃，否则积分重力。
        var start = State.Position;
        var position = RecoverPenetration(start);
        var wasGrounded = State.Grounded;
        var ground = FindGround(position, Settings.GroundSnap + Settings.Skin);
        var grounded = _verticalVelocity <= 0 && ground is { } initial && initial.Normal.Y >= WalkableY;
        if (grounded)
        {
            position += -Vector3.UnitY * MathF.Max(0, HitDistance(ground!.Value, Settings.GroundSnap + Settings.Skin) - Settings.Skin);
            _verticalVelocity = 0;
        }
        var jumped = jump && grounded;
        if (jumped) { grounded = false; _verticalVelocity = Settings.JumpSpeed; }
        else if (!grounded) _verticalVelocity = MathF.Max(-40, _verticalVelocity - Settings.Gravity * dt);

        var horizontal = new Vector3(desiredHorizontalVelocity.X, 0, desiredHorizontalVelocity.Z) * dt;
        if (grounded && ground is { } support)
        {
            // 可走斜坡沿切面推进，保持输入的水平速度。
            horizontal.Y = -(horizontal.X * support.Normal.X + horizontal.Z * support.Normal.Z) / MathF.Max(support.Normal.Y, WalkableY);
        }
        // 先处理水平墙滑及有限跨步，再处理竖直扫掠，避免动画位移参与碰撞。
        var beforeHorizontal = position;
        var moved = Slide(position, horizontal, true, out var blocked, out _);
        if (blocked && grounded && horizontal.LengthSquared > 1e-8f && TryStep(position, horizontal, out var stepPosition))
        {
            moved = stepPosition;
            blocked = false;
        }
        position = moved;
        var vertical = Vector3.UnitY * (_verticalVelocity * dt);
        position = Slide(position, vertical, false, out _, out var verticalHit);
        if (verticalHit is { } hit)
        {
            if (_verticalVelocity > 0 && hit.Normal.Y < -0.1f) _verticalVelocity = 0;
            if (_verticalVelocity <= 0 && hit.Normal.Y >= WalkableY) { grounded = true; _verticalVelocity = 0; ground = hit; }
        }
        // 只有非起跳且不向上的步才允许吸附，防止刚起跳又被拉回地面。
        if (!jumped && _verticalVelocity <= 0)
        {
            var snap = FindGround(position, Settings.GroundSnap + Settings.Skin);
            if (snap is { } down && down.Normal.Y >= WalkableY)
            {
                position.Y -= MathF.Max(0, HitDistance(down, Settings.GroundSnap + Settings.Skin) - Settings.Skin);
                grounded = true; _verticalVelocity = 0; ground = down;
            }
            else grounded = false;
        }
        position = RecoverPenetration(position);
        var velocity = (position - start) / dt;
        // 反馈实际水平位移；垂直速度用控制器状态，避免接地Skin调整影响动画。
        velocity.Y = _verticalVelocity;
        blocked |= horizontal.LengthSquared > 0.00001f && new Vector2(position.X - beforeHorizontal.X, position.Z - beforeHorizontal.Z).Length < horizontal.Xz.Length * 0.25f;
        State = new CharacterState(position, velocity, grounded, grounded && ground is { } g ? g.Normal : Vector3.UnitY,
            jumped, grounded && !wasGrounded && !jumped, blocked);
        return State;
    }

    /// <summary>将扫掠比例换算为本次查询长度上的米距离。</summary>
    private static float HitDistance(PhysicsSweepHit hit, float length) => hit.Fraction * length;
    /// <summary>从脚底沿世界-Y扫掠指定距离，支撑坡度由调用处判断。</summary>
    private PhysicsSweepHit? FindGround(Vector3 feet, float distance) =>
        _world.SweepCapsule(feet, Settings.Radius, Settings.Height, -Vector3.UnitY * distance);

    /// <summary>每次重查最深接触并有限次推出，限制单次修正以免过度位移。</summary>
    private Vector3 RecoverPenetration(Vector3 feet)
    {
        // 每次重查深度，避免旧接触在修正后重复把角色推出场景。
        for (int iteration = 0; iteration < 8; iteration++)
        {
            var contacts = _world.OverlapCapsule(feet, Settings.Radius, Settings.Height);
            var deepest = contacts.Where(hit => hit.Depth > 0.001f).OrderByDescending(hit => hit.Depth).FirstOrDefault();
            if (deepest.Depth <= 0.001f) break;
            feet += deepest.Normal * MathF.Min(deepest.Depth + Settings.Skin, 0.5f);
        }
        return feet;
    }

    /// <summary>迭代扫掠并将剩余位移投影到接触平面，水平运动另行拒绝陡坡上爬。</summary>
    private Vector3 Slide(Vector3 feet, Vector3 displacement, bool horizontalMotion, out bool blocked, out PhysicsSweepHit? lastHit)
    {
        blocked = false;
        lastHit = null;
        var remainder = displacement;
        // 保留本次运动已遇到的接触面，后续投影不得重新穿回旧平面。
        var planes = new List<Vector3>(3);
        for (int iteration = 0; iteration < 6 && remainder.LengthSquared > 1e-10f; iteration++)
        {
            var hit = _world.SweepCapsule(feet, Settings.Radius, Settings.Height, remainder);
            if (hit is null) { feet += remainder; break; }
            lastHit = hit;
            var length = remainder.Length;
            var travel = MathF.Max(0, hit.Value.Fraction - Settings.Skin / length);
            feet += remainder * travel;
            remainder *= 1 - travel;
            var normal = hit.Value.Normal;
            if (horizontalMotion && normal.Y < WalkableY && normal.Y > 0)
            {
                // 不可走斜坡当作水平墙处理，禁止沿陡坡投影获得向上的速度。
                normal.Y = 0;
                normal.Normalize();
            }
            if (horizontalMotion && normal.Y < WalkableY) blocked = true;
            planes.Add(normal);
            foreach (var plane in planes)
            {
                var inward = Vector3.Dot(remainder, plane);
                if (inward < 0) remainder -= plane * inward;
            }
            if (planes.Any(plane => Vector3.Dot(remainder, plane) < -0.0001f)) remainder = Vector3.Zero;
        }
        return feet;
    }

    /// <summary>依次检查上方、前进、可走支撑及最终净空，只在抬升限内提交跨步。</summary>
    private bool TryStep(Vector3 feet, Vector3 horizontal, out Vector3 result)
    {
        result = feet;
        if (Settings.StepHeight <= 0) return false;
        var up = Vector3.UnitY * (Settings.StepHeight + Settings.Skin);
        if (_world.SweepCapsule(feet, Settings.Radius, Settings.Height, up) is not null) return false;
        var elevated = feet + up;
        var forward = new Vector3(horizontal.X, 0, horizontal.Z);
        var moved = Slide(elevated, forward, true, out _, out _);
        var progress = new Vector2(moved.X - feet.X, moved.Z - feet.Z).Length;
        if (progress < forward.Length * 0.8f) return false;
        var downLength = Settings.StepHeight + Settings.GroundSnap + Settings.Skin * 2;
        // 圆形胶囊尚未越过台阶棱时，当前位置下扫只会命中陡的边缘法线。
        // 支撑探测向前看一个半径，实际XZ仍只提交本步输入位移。
        var probe = moved + forward.Normalized() * (Settings.Radius + Settings.Skin);
        var hit = FindGround(probe, downLength);
        if (hit is not { } landing || landing.Normal.Y < WalkableY || landing.Depth > 0.001f) return false;
        moved.Y -= MathF.Max(0, landing.Fraction * downLength - Settings.Skin);
        if (moved.Y - feet.Y > Settings.StepHeight + Settings.Skin * 1.5f || !_world.CanOccupy(moved, Settings.Radius, Settings.Height)) return false;
        result = moved;
        return true;
    }
}
