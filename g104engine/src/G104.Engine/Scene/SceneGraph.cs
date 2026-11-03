using OpenTK.Mathematics;

namespace G104.Engine.Scene;

// 缓存只派生自Document；结构改动在主线程安全点Rebuild，物理读取逻辑姿态。
/// <summary>从设计文档派生对象索引、世界矩阵和显示插值历史。</summary>
public sealed class SceneGraph
{
    /// <summary>稳定ID到当前DTO的索引，不拥有物理或GPU资源。</summary>
    private Dictionary<Guid, SceneObjectData> _objects = [];
    /// <summary>当前逻辑世界矩阵缓存，供物理与玩法查询。</summary>
    private Dictionary<Guid, Matrix4> _world = [];
    /// <summary>上一固定步的局部TRS副本，供显示插值。</summary>
    private Dictionary<Guid, TransformData> _previous = [];
    /// <summary>父级先于子级的稳定ID求值顺序。</summary>
    private List<Guid> _ordered = [];

    /// <summary>绑定文档并完整校验构图，初始插值历史与当前姿态一致。</summary>
    public SceneGraph(SceneDocument document)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Rebuild();
        ResetInterpolation();
    }

    /// <summary>本图绑定的可变设计或运行文档；改动后需按合同重建缓存。</summary>
    public SceneDocument Document { get; }
    /// <summary>返回父先子后的对象顺序，供层级求值与子树查询。</summary>
    public IReadOnlyList<Guid> OrderedIds => _ordered;
    /// <summary>按稳定ID取当前对象，缺失时抛出明确异常。</summary>
    public SceneObjectData Object(Guid id) => _objects.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException($"Scene object {id} does not exist.");
    /// <summary>取得当前逻辑世界矩阵，不进行显示插值。</summary>
    public Matrix4 WorldMatrix(Guid id) => _world.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException($"Scene object {id} does not exist.");
    /// <summary>从逻辑世界矩阵读取平移，单位米。</summary>
    public Vector3 WorldPosition(Guid id) => WorldMatrix(id).ExtractTranslation();

    /// <summary>先完整校验并在临时集合求世界矩阵，全部成功后替换缓存。</summary>
    public void Rebuild()
    {
        SceneValidator.Validate(Document);
        var objects = Document.Objects.ToDictionary(item => item.Id);
        var world = new Dictionary<Guid, Matrix4>();
        var ordered = new List<Guid>();
        // 递归先求父级；行向量世界矩阵为local * parentWorld。
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
        // 候选对象、层级顺序和所有世界矩阵成功后才一起提交。
        _objects = objects;
        _world = world;
        _ordered = ordered;
        foreach (var id in _previous.Keys.Where(id => !objects.ContainsKey(id)).ToArray()) _previous.Remove(id);
        foreach (var item in Document.Objects)
            if (!_previous.ContainsKey(item.Id)) _previous[item.Id] = TransformMath.Clone(item.Transform);
    }

    /// <summary>把世界位置转换回局部平移，保留原旋转与缩放。</summary>
    public void SetWorldPosition(Guid id, Vector3 position)
    {
        if (!TransformMath.Finite(position)) throw new SceneValidationException("World position must be finite.");
        var item = Object(id);
        var replacement = TransformMath.Clone(item.Transform);
        replacement.Position = Float3.From(item.ParentId is Guid parent
            ? Vector3.TransformPosition(position, TransformMath.Inverse(WorldMatrix(parent)))
            : position);
        SetLocalTransform(item, replacement);
    }

    /// <summary>把世界四元数转换回局部旋转，保留原平移与缩放。</summary>
    public void SetWorldRotation(Guid id, Quaternion rotation)
    {
        var item = Object(id);
        rotation = TransformMath.Normalize(rotation);
        if (item.ParentId is Guid parent)
        {
            var parentRotation = TransformMath.Decompose(WorldMatrix(parent)).Rotation.ToQuaternion();
            // 行矩阵 local * parent 对应四元数 parent * local；父级只允许正统一缩放。
            rotation = TransformMath.Normalize(Quaternion.Invert(parentRotation) * rotation);
        }
        var replacement = TransformMath.Clone(item.Transform);
        replacement.Rotation = RotationData.From(rotation);
        SetLocalTransform(item, replacement);
    }

    /// <summary>将世界矩阵转换为局部TRS；不可逆父级或剪切等非法矩阵会拒绝。</summary>
    public void SetWorldMatrix(Guid id, Matrix4 world)
    {
        var item = Object(id);
        var local = world * (item.ParentId is Guid parent ? TransformMath.Inverse(WorldMatrix(parent)) : Matrix4.Identity);
        var replacement = TransformMath.Decompose(local);
        SetLocalTransform(item, replacement);
    }

    // 位移/旋转单字段更新不反复分解自身矩阵，保留无关的已存TRS，尤其角色的精确单位缩放。
    /// <summary>暂换对象TRS并重建；失败恢复原TRS，成功保留新逻辑姿态。</summary>
    private void SetLocalTransform(SceneObjectData item, TransformData replacement)
    {
        var old = item.Transform;
        item.Transform = replacement;
        try { Rebuild(); }
        catch { item.Transform = old; throw; }
    }

    /// <summary>在固定步开始前复制局部TRS，为本步显示插值保存起点。</summary>
    public void CapturePrevious()
    {
        foreach (var item in Document.Objects) _previous[item.Id] = TransformMath.Clone(item.Transform);
    }

    /// <summary>让上一姿态追上当前姿态，用于瞬移、加载或模式切换。</summary>
    public void ResetInterpolation() => CapturePrevious();

    /// <summary>按同一alpha插值各层局部TRS再组合，避免直接插值世界矩阵产生剪切。</summary>
    public Matrix4 InterpolatedWorldMatrix(Guid id, float alpha)
    {
        if (!float.IsFinite(alpha)) throw new ArgumentOutOfRangeException(nameof(alpha));
        var item = Object(id);
        var local = TransformMath.Compose(TransformMath.Interpolate(_previous.GetValueOrDefault(id, item.Transform), item.Transform, alpha));
        return local * (item.ParentId is Guid parent ? InterpolatedWorldMatrix(parent, alpha) : Matrix4.Identity);
    }

    /// <summary>返回包含根自身的全部后代ID，保持父先子后顺序。</summary>
    public IReadOnlyList<Guid> Subtree(Guid root)
    {
        Object(root);
        // 利用父先子后顺序，单次扫描即可把已收集父级的孩子加入子树。
        var found = new HashSet<Guid> { root };
        foreach (var id in _ordered)
            if (Object(id).ParentId is Guid parent && found.Contains(parent)) found.Add(id);
        return _ordered.Where(found.Contains).ToArray();
    }
}
