using OpenTK.Mathematics;

namespace G104.Engine.Scene;

// 缓存只派生自Document；结构改动在主线程安全点Rebuild，物理读取逻辑姿态。
public sealed class SceneGraph
{
    private Dictionary<Guid, SceneObjectData> _objects = [];
    private Dictionary<Guid, Matrix4> _world = [];
    private Dictionary<Guid, TransformData> _previous = [];
    private List<Guid> _ordered = [];

    public SceneGraph(SceneDocument document)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Rebuild();
        ResetInterpolation();
    }

    public SceneDocument Document { get; }
    public IReadOnlyList<Guid> OrderedIds => _ordered;
    public SceneObjectData Object(Guid id) => _objects.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException($"Scene object {id} does not exist.");
    public Matrix4 WorldMatrix(Guid id) => _world.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException($"Scene object {id} does not exist.");
    public Vector3 WorldPosition(Guid id) => WorldMatrix(id).ExtractTranslation();

    public void Rebuild()
    {
        SceneValidator.Validate(Document);
        var objects = Document.Objects.ToDictionary(item => item.Id);
        var world = new Dictionary<Guid, Matrix4>();
        var ordered = new List<Guid>();
        void Visit(SceneObjectData item)
        {
            if (world.ContainsKey(item.Id)) return;
            if (item.ParentId is Guid parent) Visit(objects[parent]);
            var matrix = TransformMath.Compose(item.Transform) * (item.ParentId is Guid parentId ? world[parentId] : Matrix4.Identity);
            if (!TransformMath.Finite(matrix)) throw new SceneValidationException($"World transform overflow for {item.Name} ({item.Id}).");
            world.Add(item.Id, matrix);
            ordered.Add(item.Id);
        }
        foreach (var item in Document.Objects) Visit(item);
        _objects = objects;
        _world = world;
        _ordered = ordered;
        foreach (var id in _previous.Keys.Where(id => !objects.ContainsKey(id)).ToArray()) _previous.Remove(id);
        foreach (var item in Document.Objects)
            if (!_previous.ContainsKey(item.Id)) _previous[item.Id] = TransformMath.Clone(item.Transform);
    }

    public void SetWorldPosition(Guid id, Vector3 position)
    {
        if (!TransformMath.Finite(position)) throw new SceneValidationException("World position must be finite.");
        var world = WorldMatrix(id);
        world.Row3 = new Vector4(position, 1);
        SetWorldMatrix(id, world);
    }

    public void SetWorldRotation(Guid id, Quaternion rotation)
    {
        var world = TransformMath.Decompose(WorldMatrix(id));
        world.Rotation = RotationData.From(TransformMath.Normalize(rotation));
        SetWorldMatrix(id, TransformMath.Compose(world));
    }

    public void SetWorldMatrix(Guid id, Matrix4 world)
    {
        var item = Object(id);
        var local = world * (item.ParentId is Guid parent ? TransformMath.Inverse(WorldMatrix(parent)) : Matrix4.Identity);
        var replacement = TransformMath.Decompose(local);
        var old = item.Transform;
        item.Transform = replacement;
        try { Rebuild(); }
        catch { item.Transform = old; throw; }
    }

    public void CapturePrevious()
    {
        foreach (var item in Document.Objects) _previous[item.Id] = TransformMath.Clone(item.Transform);
    }

    public void ResetInterpolation() => CapturePrevious();

    public Matrix4 InterpolatedWorldMatrix(Guid id, float alpha)
    {
        if (!float.IsFinite(alpha)) throw new ArgumentOutOfRangeException(nameof(alpha));
        var item = Object(id);
        var local = TransformMath.Compose(TransformMath.Interpolate(_previous.GetValueOrDefault(id, item.Transform), item.Transform, alpha));
        return local * (item.ParentId is Guid parent ? InterpolatedWorldMatrix(parent, alpha) : Matrix4.Identity);
    }

    public IReadOnlyList<Guid> Subtree(Guid root)
    {
        Object(root);
        var found = new HashSet<Guid> { root };
        foreach (var id in _ordered)
            if (Object(id).ParentId is Guid parent && found.Contains(parent)) found.Add(id);
        return _ordered.Where(found.Contains).ToArray();
    }
}
