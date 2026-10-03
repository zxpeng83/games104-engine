using G104.Engine.Scene;
using JoltPhysicsSharp;
using OpenTK.Mathematics;
using NMatrix = System.Numerics.Matrix4x4;
using NQuaternion = System.Numerics.Quaternion;
using NVector = System.Numerics.Vector3;

namespace G104.Engine.Physics;

public readonly record struct PhysicsRayHit(Guid ObjectId, Vector3 Position, Vector3 Normal, float Distance);
public readonly record struct PhysicsSweepHit(Guid ObjectId, Vector3 Position, Vector3 Normal, float Fraction, float Depth);
public readonly record struct PhysicsContact(Guid ObjectId, Vector3 Position, Vector3 Normal, float Depth);
public sealed record WorldCollider(Guid ObjectId, ColliderKind Kind, Vector3 Center, Quaternion Rotation, Vector3 Size);

// 单个场景拥有系统、刚体、形状和过滤器；库级Init单独计数，允许新旧场景同时准备。
public sealed class PhysicsWorld : IDisposable
{
    private static readonly object FoundationLock = new();
    private static int _foundationOwners;
    private readonly SceneGraph _graph;
    private PhysicsSystem? _system;
    private JobSystemThreadPool? _jobs;
    private BroadPhaseLayerInterfaceTable? _broadPhase;
    private ObjectLayerPairFilterTable? _pairs;
    private ObjectVsBroadPhaseLayerFilterTable? _objectVsBroad;
    private readonly Dictionary<Guid, BodyEntry> _bodies = [];
    private readonly Dictionary<uint, Guid> _bodyIds = [];
    private readonly List<RayCastResult> _rayResults = [];
    private readonly List<ShapeCastResult> _castResults = [];
    private readonly List<CollideShapeResult> _overlapResults = [];
    private readonly Dictionary<(float Radius, float Height), CapsuleShape> _queryCapsules = [];
    private bool _ownsFoundation;
    private bool _disposed;

    private sealed record BodyEntry(BodyID Id, Shape Shape, WorldCollider Collider, bool Dynamic)
    {
        public bool Enabled { get; set; } = true;
    }

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
                var size = collider.Size.ToVector() * transform.Scale.ToVector();
                var center = Vector3.TransformPosition(collider.Center.ToVector(), matrix);
                AddBody(new WorldCollider(item.Id, collider.Kind, center, transform.Rotation.ToQuaternion(), size), false, collider.Friction);
            }
            _system.OptimizeBroadPhase();
        }
        catch { Dispose(); throw; }
    }

    public int BodyCount => _bodies.Count;
    public static int ActiveWorlds { get { lock (FoundationLock) return _foundationOwners; } }
    public IEnumerable<WorldCollider> Colliders => _bodies.Values.Where(body => body.Enabled).Select(body => body.Collider);
    public bool IsEnabled(Guid id) => _bodies.TryGetValue(id, out var body) && body.Enabled;

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

    public void SetEnabled(Guid id, bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_bodies.TryGetValue(id, out var body)) return;
        if (body.Enabled == enabled) return;
        if (enabled) System.BodyInterface.AddBody(body.Id, body.Dynamic ? Activation.Activate : Activation.DontActivate);
        else System.BodyInterface.RemoveBody(body.Id);
        body.Enabled = enabled;
    }

    public void Step(float dt)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!float.IsFinite(dt) || dt <= 0 || dt > 0.1f) throw new ArgumentOutOfRangeException(nameof(dt));
        var error = System.Update(dt, 1, _jobs!);
        if (error != PhysicsUpdateError.None) throw new InvalidOperationException($"Jolt update exceeded capacity: {error}.");
    }

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

    public bool CanOccupy(Vector3 feet, float radius, float height, float tolerance = 0.003f) =>
        !OverlapCapsule(feet, radius, height).Any(hit => hit.Depth > tolerance);

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
    private static NMatrix QueryTransform(Vector3 feet, float height) =>
        NMatrix.Transpose(NMatrix.CreateTranslation(ToNumerics(feet + Vector3.UnitY * (height * 0.5f))));

    // 刚体烟测入口；训练场角色不通过此刚体驱动。
    public Guid CreateDynamicBox(Vector3 center, Vector3 size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var id = Guid.NewGuid();
        AddBody(new WorldCollider(id, ColliderKind.Box, center, Quaternion.Identity, size), true, 0.6f);
        return id;
    }
    public Vector3 BodyPosition(Guid id) => FromNumerics(System.BodyInterface.GetPosition(_bodies[id].Id));
    public bool BodySleeping(Guid id) => !System.BodyInterface.IsActive(_bodies[id].Id);

    private PhysicsSystem System => _system ?? throw new ObjectDisposedException(nameof(PhysicsWorld));
    internal static NVector ToNumerics(Vector3 value) => new(value.X, value.Y, value.Z);
    internal static Vector3 FromNumerics(NVector value) => new(value.X, value.Y, value.Z);
    private static NQuaternion ToNumerics(Quaternion value) => new(value.X, value.Y, value.Z, value.W);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
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
