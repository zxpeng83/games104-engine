using G104.Engine.Scene;
using JoltPhysicsSharp;
using OpenTK.Mathematics;
using NMatrix = System.Numerics.Matrix4x4;
using NQuaternion = System.Numerics.Quaternion;
using NVector = System.Numerics.Vector3;

namespace G104.Engine.Physics;

/// <summary>物理射线最近命中的世界空间信息。</summary>
/// <param name="ObjectId">被命中的场景对象标识。</param>
/// <param name="Position">世界空间接触点，单位米。</param>
/// <param name="Normal">世界空间表面法线。</param>
/// <param name="Distance">从射线起点到命中的距离，单位米。</param>
public readonly record struct PhysicsRayHit(Guid ObjectId, Vector3 Position, Vector3 Normal, float Distance);
/// <summary>胶囊沿位移扫掠时的最早阻挡信息。</summary>
/// <param name="ObjectId">阻挡物的场景对象标识。</param>
/// <param name="Position">阻挡物表面的世界空间接触点。</param>
/// <param name="Normal">从阻挡物朝查询胶囊的世界空间法线。</param>
/// <param name="Fraction">沿本次位移的命中比例，范围[0,1]。</param>
/// <param name="Depth">命中的穿透深度，单位米。</param>
public readonly record struct PhysicsSweepHit(Guid ObjectId, Vector3 Position, Vector3 Normal, float Fraction, float Depth);
/// <summary>胶囊重叠查询中的单个世界空间接触。</summary>
/// <param name="ObjectId">重叠物体的场景对象标识。</param>
/// <param name="Position">物体表面的世界空间接触点。</param>
/// <param name="Normal">从物体朝查询胶囊的分离方向。</param>
/// <param name="Depth">穿透深度，单位米。</param>
public readonly record struct PhysicsContact(Guid ObjectId, Vector3 Position, Vector3 Normal, float Depth);
/// <summary>已应用场景世界变换的碰撞几何描述。</summary>
/// <param name="ObjectId">所属场景对象标识。</param>
/// <param name="Kind">受支持的Box或Capsule形状。</param>
/// <param name="Center">世界空间几何中心，单位米。</param>
/// <param name="Rotation">世界空间旋转。</param>
/// <param name="Size">已应用世界缩放的完整尺寸；胶囊X/Z为直径、Y为总高。</param>
public sealed record WorldCollider(Guid ObjectId, ColliderKind Kind, Vector3 Center, Quaternion Rotation, Vector3 Size);

// 单个场景拥有系统、刚体、形状和过滤器；库级Init单独计数，允许新旧场景同时准备。
/// <summary>拥有一个场景的Jolt系统、刚体及查询资源；全局初始化按活动世界计数。</summary>
public sealed class PhysicsWorld : IDisposable
{
    /// <summary>同步全局Jolt初始化计数，允许候选场景与旧场景同时存活。</summary>
    private static readonly object FoundationLock = new();
    /// <summary>已成功取得Jolt全局初始化所有权的世界数量。</summary>
    private static int _foundationOwners;
    /// <summary>构造时传入的同文档场景图引用，用于保持世界与场景关联。</summary>
    private readonly SceneGraph _graph;
    /// <summary>本世界拥有的原生物理系统，失败或释放后置空。</summary>
    private PhysicsSystem? _system;
    /// <summary>本世界更新使用的Jolt工作线程池。</summary>
    private JobSystemThreadPool? _jobs;
    /// <summary>将静态/动态对象层映射到宽相层的原生表。</summary>
    private BroadPhaseLayerInterfaceTable? _broadPhase;
    /// <summary>控制静态与动态层组合是否参与碰撞的原生过滤表。</summary>
    private ObjectLayerPairFilterTable? _pairs;
    /// <summary>对象层与宽相层的原生过滤接口，由本世界释放。</summary>
    private ObjectVsBroadPhaseLayerFilterTable? _objectVsBroad;
    /// <summary>按场景GUID索引本世界刚体及其形状所有权。</summary>
    private readonly Dictionary<Guid, BodyEntry> _bodies = [];
    /// <summary>将Jolt刚体标识反查为场景GUID，供命中结果返回。</summary>
    private readonly Dictionary<uint, Guid> _bodyIds = [];
    /// <summary>复用的原生射线命中缓冲，每次查询前清空。</summary>
    private readonly List<RayCastResult> _rayResults = [];
    /// <summary>复用的原生形状扫掠命中缓冲。</summary>
    private readonly List<ShapeCastResult> _castResults = [];
    /// <summary>复用的原生重叠结果缓冲，随后转换为引擎接触记录。</summary>
    private readonly List<CollideShapeResult> _overlapResults = [];
    /// <summary>按半径与总高缓存不注册刚体的查询胶囊，随世界统一释放。</summary>
    private readonly Dictionary<(float Radius, float Height), CapsuleShape> _queryCapsules = [];
    /// <summary>记录本实例是否已增加全局计数，保证部分构造失败也只释放一次。</summary>
    private bool _ownsFoundation;
    /// <summary>幂等释放标记，阻止资源释放后继续更新或查询。</summary>
    private bool _disposed;

    /// <summary>保存单个刚体句柄、形状所有权和创建描述。</summary>
    /// <param name="Id">本世界中的Jolt刚体句柄。</param>
    /// <param name="Shape">随刚体销毁后由世界释放的形状。</param>
    /// <param name="Collider">建立刚体时使用的世界空间几何描述。</param>
    /// <param name="Dynamic">是否采用动态运动类型，重新启用时据此激活。</param>
    private sealed record BodyEntry(BodyID Id, Shape Shape, WorldCollider Collider, bool Dynamic)
    {
        /// <summary>刚体是否已加入物理系统；关闭的门仍保留刚体及形状供重新启用。</summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>从同一文档场景图建立静态环境；构造失败时清理已取得的原生资源。</summary>
    public PhysicsWorld(SceneDocument document, SceneGraph graph)
    {
        ArgumentNullException.ThrowIfNull(document);
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        if (!ReferenceEquals(document, graph.Document)) throw new ArgumentException("Physics and scene graph must share a document.");
        try
        {
            lock (FoundationLock)
            {
                if (_foundationOwners == 0 && !Foundation.Init(false)) throw new InvalidOperationException("Jolt initialization failed.");
                _foundationOwners++;
                _ownsFoundation = true;
            }
            _pairs = new ObjectLayerPairFilterTable(2);
            _pairs.EnableCollision(new ObjectLayer(0), new ObjectLayer(1));
            _pairs.EnableCollision(new ObjectLayer(1), new ObjectLayer(1));
            _broadPhase = new BroadPhaseLayerInterfaceTable(2, 2);
            _broadPhase.MapObjectToBroadPhaseLayer(new ObjectLayer(0), new BroadPhaseLayer(0));
            _broadPhase.MapObjectToBroadPhaseLayer(new ObjectLayer(1), new BroadPhaseLayer(1));
            _objectVsBroad = new ObjectVsBroadPhaseLayerFilterTable(_broadPhase, 2, _pairs, 2);
            _system = new PhysicsSystem(new PhysicsSystemSettings
            {
                MaxBodies = 2048, MaxBodyPairs = 8192, MaxContactConstraints = 2048,
                ObjectLayerPairFilter = _pairs, BroadPhaseLayerInterface = _broadPhase, ObjectVsBroadPhaseLayerFilter = _objectVsBroad
            });
            _system.Gravity = new NVector(0, -9.81f, 0);
            _jobs = new JobSystemThreadPool(new JobSystemThreadPoolConfig
            {
                maxJobs = (uint)Foundation.MaxPhysicsJobs, maxBarriers = (uint)Foundation.MaxPhysicsBarriers, numThreads = 1
            });
            foreach (var item in document.Objects)
            {
                if (item.Collider is not { Kind: not ColliderKind.None } collider ||
                    item.Kind is ObjectKind.Player or ObjectKind.Npc or ObjectKind.Goal or ObjectKind.Group or ObjectKind.PointLight) continue;
                var matrix = graph.WorldMatrix(item.Id);
                var transform = TransformMath.Decompose(matrix);
                // 局部尺寸乘世界缩放一次，局部中心通过完整矩阵变换一次。
                var size = collider.Size.ToVector() * transform.Scale.ToVector();
                var center = Vector3.TransformPosition(collider.Center.ToVector(), matrix);
                AddBody(new WorldCollider(item.Id, collider.Kind, center, transform.Rotation.ToQuaternion(), size), false, collider.Friction);
            }
            _system.OptimizeBroadPhase();
        }
        catch { Dispose(); throw; }
    }

    /// <summary>已分配刚体数量，包含暂时停用的刚体。</summary>
    public int BodyCount => _bodies.Count;
    /// <summary>线程同步地读取Jolt全局所有者数量，供生命周期检查使用。</summary>
    public static int ActiveWorlds { get { lock (FoundationLock) return _foundationOwners; } }
    /// <summary>枚举已启用刚体的创建描述，不实时回写动态刚体运动。</summary>
    public IEnumerable<WorldCollider> Colliders => _bodies.Values.Where(body => body.Enabled).Select(body => body.Collider);
    /// <summary>查询指定场景对象的刚体是否存在且已启用。</summary>
    public bool IsEnabled(Guid id) => _bodies.TryGetValue(id, out var body) && body.Enabled;

    /// <summary>校验尺寸并创建形状与刚体，注册GUID映射；失败释放新建形状。</summary>
    private void AddBody(WorldCollider collider, bool dynamic, float friction)
    {
        if (!TransformMath.Finite(collider.Center) || !TransformMath.Finite(collider.Size) ||
            collider.Size.X <= 0 || collider.Size.Y <= 0 || collider.Size.Z <= 0)
            throw new ArgumentException("Physics collider dimensions must be finite and positive.");
        if (collider.Kind == ColliderKind.Capsule && (MathF.Abs(collider.Size.X - collider.Size.Z) > 0.0001f || collider.Size.Y <= collider.Size.X))
            throw new ArgumentException("A static capsule needs equal X/Z diameters and height greater than its diameter.");
        Shape shape = collider.Kind switch
        {
            ColliderKind.Box => new BoxShape(ToNumerics(collider.Size * 0.5f), MathF.Min(0.02f, MathF.Min(collider.Size.X, MathF.Min(collider.Size.Y, collider.Size.Z)) * 0.1f)),
            ColliderKind.Capsule => new CapsuleShape(MathF.Max(0.001f, collider.Size.Y * 0.5f - collider.Size.X * 0.5f), collider.Size.X * 0.5f),
            _ => throw new ArgumentOutOfRangeException(nameof(collider))
        };
        try
        {
            using var settings = new BodyCreationSettings(shape, ToNumerics(collider.Center), ToNumerics(collider.Rotation),
                dynamic ? MotionType.Dynamic : MotionType.Static, new ObjectLayer(dynamic ? 1u : 0u))
            { Friction = friction, Restitution = 0, AllowSleeping = true };
            var id = System.BodyInterface.CreateAndAddBody(settings, dynamic ? Activation.Activate : Activation.DontActivate);
            if (id.IsInvalid) throw new InvalidOperationException($"Jolt could not allocate body {collider.ObjectId}.");
            _bodies.Add(collider.ObjectId, new BodyEntry(id, shape, collider, dynamic));
            _bodyIds.Add(id.ID, collider.ObjectId);
        }
        catch { shape.Dispose(); throw; }
    }

    /// <summary>将现有刚体移出或重新加入同一物理世界，不销毁其形状。</summary>
    public void SetEnabled(Guid id, bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_bodies.TryGetValue(id, out var body)) return;
        if (body.Enabled == enabled) return;
        if (enabled) System.BodyInterface.AddBody(body.Id, body.Dynamic ? Activation.Activate : Activation.DontActivate);
        else System.BodyInterface.RemoveBody(body.Id);
        body.Enabled = enabled;
    }

    /// <summary>以有效秒时间推进一次Jolt更新，并明确报告后端容量错误。</summary>
    public void Step(float dt)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!float.IsFinite(dt) || dt <= 0 || dt > 0.1f) throw new ArgumentOutOfRangeException(nameof(dt));
        var error = System.Update(dt, 1, _jobs!);
        if (error != PhysicsUpdateError.None) throw new InvalidOperationException($"Jolt update exceeded capacity: {error}.");
    }

    /// <summary>归一化方向后查询指定米距离内最近表面；零方向或未命中返回null。</summary>
    public PhysicsRayHit? Raycast(Vector3 origin, Vector3 direction, float maxDistance)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!TransformMath.Finite(origin) || !TransformMath.Finite(direction) || !float.IsFinite(maxDistance) || maxDistance <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDistance));
        if (direction.LengthSquared < 1e-10f) return null;
        var ray = new Ray(ToNumerics(origin), ToNumerics(direction.Normalized() * maxDistance));
        _rayResults.Clear();
        System.NarrowPhaseQuery.CastRay(ray, new RayCastSettings(), CollisionCollectorType.ClosestHit, _rayResults);
        if (_rayResults.Count == 0) return null;
        var hit = _rayResults[0];
        var point = ray.GetPointOnRay(hit.Fraction);
        var bodyId = hit.BodyID;
        var bodyLock = System.BodyLockInterface;
        var transformed = System.BodyInterface.GetTransformedShape(bodyLock, bodyId);
        var subShape = new SubShapeID(hit.subShapeID2);
        var normal = transformed.GetWorldSpaceSurfaceNormal(subShape, point);
        return new PhysicsRayHit(_bodyIds.GetValueOrDefault(bodyId.ID), FromNumerics(point), FromNumerics(normal), hit.Fraction * maxDistance);
    }

    // 胶囊不注册为刚体，因此角色彼此不推挤，也无需将查询混入动画位置。
    /// <summary>从逻辑脚底沿世界位移扫掠胶囊，返回最早朝向运动的阻挡接触。</summary>
    public PhysicsSweepHit? SweepCapsule(Vector3 feet, float radius, float height, Vector3 displacement)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (displacement.LengthSquared < 1e-12f) return null;
        var shape = Capsule(radius, height);
        var matrix = QueryTransform(feet, height);
        var movement = ToNumerics(displacement);
        var baseOffset = NVector.Zero;
        _castResults.Clear();
        System.NarrowPhaseQuery.CastShape(shape, matrix, movement, new ShapeCastSettings { ReturnDeepestPoint = true }, baseOffset,
            CollisionCollectorType.AllHit, _castResults);
        // 只保留迎向运动的有限法线接触，过滤平行或正在分离的命中。
        PhysicsSweepHit? closest = null;
        foreach (var hit in _castResults)
        {
            var normal = FromNumerics(-NVector.Normalize(hit.PenetrationAxis));
            if (!TransformMath.Finite(normal) || Vector3.Dot(displacement, normal) >= -0.00001f) continue;
            if (closest is null || hit.Fraction < closest.Value.Fraction)
                closest = new PhysicsSweepHit(_bodyIds.GetValueOrDefault(hit.BodyID2.ID), FromNumerics(hit.ContactPointOn2), normal,
                    Math.Clamp(hit.Fraction, 0, 1), hit.PenetrationDepth);
        }
        return closest;
    }

    /// <summary>返回逻辑脚底处胶囊的全部有效接触，供穿透恢复和门洞占用判断。</summary>
    public IReadOnlyList<PhysicsContact> OverlapCapsule(Vector3 feet, float radius, float height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var shape = Capsule(radius, height);
        var matrix = QueryTransform(feet, height);
        var scale = NVector.One;
        var baseOffset = NVector.Zero;
        _overlapResults.Clear();
        System.NarrowPhaseQuery.CollideShape(shape, scale, matrix, new CollideShapeSettings(), baseOffset,
            CollisionCollectorType.AllHit, _overlapResults);
        var contacts = new List<PhysicsContact>(_overlapResults.Count);
        foreach (var hit in _overlapResults)
        {
            var normal = FromNumerics(-NVector.Normalize(hit.PenetrationAxis));
            if (TransformMath.Finite(normal)) contacts.Add(new PhysicsContact(_bodyIds.GetValueOrDefault(hit.BodyID2.ID), FromNumerics(hit.ContactPointOn2), normal, hit.PenetrationDepth));
        }
        return contacts;
    }

    /// <summary>检查胶囊是否存在超过米制容差的穿透，用于净空判断。</summary>
    public bool CanOccupy(Vector3 feet, float radius, float height, float tolerance = 0.003f) =>
        !OverlapCapsule(feet, radius, height).Any(hit => hit.Depth > tolerance);

    /// <summary>校验半径和总高，并取得由本世界拥有的可复用查询形状。</summary>
    private CapsuleShape Capsule(float radius, float height)
    {
        if (!float.IsFinite(radius) || !float.IsFinite(height) || radius <= 0 || height <= 2 * radius)
            throw new ArgumentOutOfRangeException(nameof(radius), "Capsule height must exceed its diameter.");
        var key = (radius, height);
        if (!_queryCapsules.TryGetValue(key, out var shape))
            _queryCapsules[key] = shape = new CapsuleShape(height * 0.5f - radius, radius);
        return shape;
    }

    // 固定2.22绑定的ToJolt会转置Matrix4x4，公开查询参数实际采用列向量矩阵；
    // 将Numerics行向量TRS在此边界转置一次，不能直接传CreateTranslation的原值。
    /// <summary>将脚底上移半高得到胶囊中心，并在固定Jolt绑定边界转置查询矩阵。</summary>
    private static NMatrix QueryTransform(Vector3 feet, float height) =>
        NMatrix.Transpose(NMatrix.CreateTranslation(ToNumerics(feet + Vector3.UnitY * (height * 0.5f))));

    // 刚体烟测入口；训练场角色不通过此刚体驱动。
    /// <summary>创建独立动态箱用于后端重力、接触和睡眠检查，不驱动训练场角色。</summary>
    public Guid CreateDynamicBox(Vector3 center, Vector3 size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var id = Guid.NewGuid();
        AddBody(new WorldCollider(id, ColliderKind.Box, center, Quaternion.Identity, size), true, 0.6f);
        return id;
    }
    /// <summary>读取指定刚体当前世界中心位置，单位米。</summary>
    public Vector3 BodyPosition(Guid id) => FromNumerics(System.BodyInterface.GetPosition(_bodies[id].Id));
    /// <summary>查询指定刚体是否处于未激活状态，供稳定落地检查使用。</summary>
    public bool BodySleeping(Guid id) => !System.BodyInterface.IsActive(_bodies[id].Id);

    /// <summary>取得仍存活的原生系统；系统不可用时抛出释放异常。</summary>
    private PhysicsSystem System => _system ?? throw new ObjectDisposedException(nameof(PhysicsWorld));
    /// <summary>按相同XYZ分量转换查询向量，不改变世界坐标轴。</summary>
    internal static NVector ToNumerics(Vector3 value) => new(value.X, value.Y, value.Z);
    /// <summary>将Jolt返回向量转换为引擎向量，保持世界坐标分量。</summary>
    internal static Vector3 FromNumerics(NVector value) => new(value.X, value.Y, value.Z);
    /// <summary>按XYZW分量转换刚体旋转，不经过查询矩阵转置路径。</summary>
    private static NQuaternion ToNumerics(Quaternion value) => new(value.X, value.Y, value.Z, value.W);

    /// <summary>依次销毁刚体、形状、系统及辅助资源，最后一个世界释放时关闭Jolt全局库。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // 先移出并销毁刚体，再释放其形状；查询形状也必须早于系统清理。
        if (_system is not null)
        {
            foreach (var body in _bodies.Values)
            {
                if (body.Enabled) _system.BodyInterface.RemoveBody(body.Id);
                _system.BodyInterface.DestroyBody(body.Id);
                body.Shape.Dispose();
            }
            _bodies.Clear();
            _bodyIds.Clear();
            foreach (var capsule in _queryCapsules.Values) capsule.Dispose();
            _queryCapsules.Clear();
            _system.Dispose();
            _system = null;
        }
        _jobs?.Dispose(); _jobs = null;
        _objectVsBroad?.Dispose(); _objectVsBroad = null;
        _broadPhase?.Dispose(); _broadPhase = null;
        _pairs?.Dispose(); _pairs = null;
        lock (FoundationLock)
        {
            if (_ownsFoundation)
            {
                _ownsFoundation = false;
                if (--_foundationOwners == 0) Foundation.Shutdown();
            }
        }
    }
}
