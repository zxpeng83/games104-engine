using OpenTK.Mathematics;

namespace G104.Engine.Physics;

public sealed class CharacterSettings
{
    public float Radius { get; set; } = 0.35f;
    public float Height { get; set; } = 1.9f;
    public float StepHeight { get; set; } = 0.35f;
    public float MaxSlopeDegrees { get; set; } = 45;
    public float Gravity { get; set; } = 18;
    public float JumpSpeed { get; set; } = 6;
    public float Skin { get; set; } = 0.025f;
    public float GroundSnap { get; set; } = 0.12f;
}

public readonly record struct CharacterState(Vector3 Position, Vector3 Velocity, bool Grounded, Vector3 GroundNormal,
    bool Jumped, bool Landed, bool Blocked)
{
    public float Speed => new Vector2(Velocity.X, Velocity.Z).Length;
    public float VerticalVelocity => Velocity.Y;
}

// feet是逻辑脚底；Jolt只做查询，跨步、坡度、墙滑和跳跃都由本类决策。
public sealed class KinematicCharacter
{
    private readonly PhysicsWorld _world;
    private float _verticalVelocity;
    public CharacterSettings Settings { get; }
    public CharacterState State { get; private set; }
    private float WalkableY => MathF.Cos(Settings.MaxSlopeDegrees * MathF.PI / 180);

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

    public void Teleport(Vector3 feet)
    {
        _verticalVelocity = 0;
        State = new CharacterState(feet, Vector3.Zero, false, Vector3.UnitY, false, false, false);
    }

    public CharacterState Tick(float dt, Vector3 desiredHorizontalVelocity, bool jump)
    {
        if (!float.IsFinite(dt) || dt <= 0 || dt > 0.1f) throw new ArgumentOutOfRangeException(nameof(dt));
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

    private static float HitDistance(PhysicsSweepHit hit, float length) => hit.Fraction * length;
    private PhysicsSweepHit? FindGround(Vector3 feet, float distance) =>
        _world.SweepCapsule(feet, Settings.Radius, Settings.Height, -Vector3.UnitY * distance);

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

    private Vector3 Slide(Vector3 feet, Vector3 displacement, bool horizontalMotion, out bool blocked, out PhysicsSweepHit? lastHit)
    {
        blocked = false;
        lastHit = null;
        var remainder = displacement;
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
