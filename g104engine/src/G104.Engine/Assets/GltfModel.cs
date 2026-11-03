using OpenTK.Mathematics;

namespace G104.Engine.Assets;

// 导入结果不含GL句柄，因而可以在无图形上下文的验证入口中检查。
/// <summary>保存无GPU句柄的导入结果，提供节点求值、蒙皮矩阵和CPU包围盒。</summary>
public sealed class GltfModel
{
    /// <summary>资产根目录下的源模型路径，用于缓存标识和错误定位。</summary>
    public required string SourcePath { get; init; }
    /// <summary>完整逻辑节点及默认局部姿态，索引与导入节点对应。</summary>
    public required ModelNode[] Nodes { get; init; }
    /// <summary>父节点先于子节点的求值顺序，避免读取尚未计算的父矩阵。</summary>
    public required int[] EvaluationOrder { get; init; }
    /// <summary>默认场景中可绘制的三角形批次及其材质。</summary>
    public required ModelPrimitive[] Primitives { get; init; }
    /// <summary>按skin索引保存关节节点和逆绑定矩阵。</summary>
    public required ModelSkin[] Skins { get; init; }
    /// <summary>按唯一名称查找的TRS动画片段，不包含实例播放游标。</summary>
    public required IReadOnlyDictionary<string, AnimationClip> Clips { get; init; }
    /// <summary>单个skin允许的关节上限，与渲染端骨骼调色板容量一致。</summary>
    public const int MaximumJoints = 128;

    /// <summary>复制默认局部姿态，为每个动画实例建立独立的可写数组。</summary>
    public NodePose[] CreateDefaultPose() => Nodes.Select(n => n.DefaultPose).ToArray();

    /// <summary>按父子顺序将局部TRS组合为模型空间矩阵；输出数组由调用者提供。</summary>
    public void EvaluateWorld(NodePose[] local, Matrix4[] world)
    {
        foreach (int i in EvaluationOrder)
        {
            Matrix4 matrix = local[i].Matrix;
            world[i] = Nodes[i].Parent < 0 ? matrix : matrix * world[Nodes[i].Parent];
        }
    }

    /// <summary>将逆绑定与当前关节变换组合到mesh局部空间，供CPU或GPU蒙皮使用。</summary>
    public Matrix4[] CreatePalette(int skinIndex, int meshNode, Matrix4[] world)
    {
        ModelSkin skin = Skins[skinIndex];
        Matrix4 inverseMesh = world[meshNode].Inverted();
        var palette = new Matrix4[skin.Joints.Length];
        // 行向量空间：mesh局部→bind joint→当前模型空间→当前mesh局部。
        // 上传行字节且transpose=false，GLSL看到转置后的列向量矩阵。
        for (int i = 0; i < palette.Length; i++)
            palette[i] = skin.InverseBind[i] * world[skin.Joints[i]] * inverseMesh;
        return palette;
    }

    /// <summary>在给定局部姿态下逐顶点计算模型空间轴对齐包围盒，省略姿态时采用默认值。</summary>
    public (Vector3 Min, Vector3 Max) ComputeBounds(NodePose[]? pose = null)
    {
        var world = new Matrix4[Nodes.Length]; EvaluateWorld(pose ?? CreateDefaultPose(), world);
        Vector3 minimum = new(float.PositiveInfinity), maximum = new(float.NegativeInfinity);
        foreach (var primitive in Primitives)
        {
            Matrix4[]? palette = primitive.Skin < 0 ? null : CreatePalette(primitive.Skin, primitive.Node, world);
            for (int v = 0; v < primitive.Vertices.Length; v += 20)
            {
                var position = new Vector3(primitive.Vertices[v], primitive.Vertices[v + 1], primitive.Vertices[v + 2]);
                if (palette is not null)
                {
                    Vector3 skinned = Vector3.Zero;
                    for (int w = 0; w < 4; w++)
                        if (primitive.Vertices[v + 16 + w] > 0) skinned += Vector3.TransformPosition(position, palette[(int)primitive.Vertices[v + 12 + w]]) * primitive.Vertices[v + 16 + w];
                    position = skinned;
                }
                position = Vector3.TransformPosition(position, world[primitive.Node]);
                minimum = Vector3.ComponentMin(minimum, position); maximum = Vector3.ComponentMax(maximum, position);
            }
        }
        return (minimum, maximum);
    }
}

/// <summary>节点相对父节点的局部TRS姿态。</summary>
/// <param name="Translation">局部平移；模型按米制Y向上导入。</param>
/// <param name="Rotation">局部旋转四元数。</param>
/// <param name="Scale">局部各轴缩放。</param>
public readonly record struct NodePose(Vector3 Translation, Quaternion Rotation, Vector3 Scale)
{
    /// <summary>按行向量约定组合缩放、旋转、平移的局部矩阵。</summary>
    public Matrix4 Matrix => Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Translation);
    /// <summary>分别插值平移、缩放及最短弧旋转，避免直接混合矩阵产生剪切。</summary>
    public static NodePose Blend(NodePose a, NodePose b, float weight) => new(
        Vector3.Lerp(a.Translation, b.Translation, weight), ShortSlerp(a.Rotation, b.Rotation, weight), Vector3.Lerp(a.Scale, b.Scale, weight));

    /// <summary>先统一四元数半球再球面插值并归一化，保证走最短旋转弧。</summary>
    public static Quaternion ShortSlerp(Quaternion a, Quaternion b, float amount)
    {
        if (a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W < 0) b = new(-b.X, -b.Y, -b.Z, -b.W);
        return Quaternion.Normalize(Quaternion.Slerp(a, b, amount));
    }
}

/// <summary>保留逻辑节点名称、父子关系与默认局部姿态。</summary>
/// <param name="Name">导入名称或生成的回退名称。</param>
/// <param name="Parent">父节点索引；-1表示根节点。</param>
/// <param name="DefaultPose">没有动画轨道覆盖时使用的局部TRS。</param>
public sealed record ModelNode(string Name, int Parent, NodePose DefaultPose);
/// <summary>描述一组mesh局部到关节绑定空间的蒙皮关系。</summary>
/// <param name="Joints">关节在模型节点数组中的索引。</param>
/// <param name="InverseBind">与Joints逐项对应的逆绑定矩阵。</param>
public sealed record ModelSkin(int[] Joints, Matrix4[] InverseBind);
// 零填充的UV顶点槽不能代表源mesh具备UV；设计材质覆盖必须消费原始能力信息。
/// <summary>一个可绘制三角形批次，CPU顶点布局与渲染端约定一致。</summary>
/// <param name="Node">引用该mesh的模型节点索引。</param>
/// <param name="Skin">skin索引；-1表示刚性绘制。</param>
/// <param name="Vertices">每顶点20个float：位置3、法线3、UV2、切线4、关节4、权重4。</param>
/// <param name="Indices">三角形顶点索引。</param>
/// <param name="Material">该批次的导入材质。</param>
/// <param name="HasUv0">源mesh是否确有UV0，不由零填充顶点槽推断。</param>
/// <param name="PrimitiveIndex">源mesh中的批次序号，用于覆盖匹配和诊断。</param>
public sealed record ModelPrimitive(int Node, int Skin, float[] Vertices, uint[] Indices, ModelMaterial Material, bool HasUv0, int PrimitiveIndex);
/// <summary>保存V1支持的金属粗糙度材质输入，纹理仍为编码数据。</summary>
/// <param name="BaseColor">线性基础色及alpha因子。</param>
/// <param name="Metallic">金属度因子。</param>
/// <param name="Roughness">粗糙度因子。</param>
/// <param name="DoubleSided">是否关闭背面剔除。</param>
/// <param name="BaseColorImage">可选基础色纹理。</param>
/// <param name="NormalImage">可选切线空间法线纹理。</param>
/// <param name="MetallicRoughnessImage">可选G通道粗糙度、B通道金属度纹理。</param>
/// <param name="NormalScale">法线纹理XY分量的缩放因子。</param>
/// <param name="AlphaCutoff">遮罩裁剪阈值；-1表示不透明材质。</param>
public sealed record ModelMaterial(Vector4 BaseColor, float Metallic, float Roughness, bool DoubleSided,
    ModelTexture? BaseColorImage, ModelTexture? NormalImage, ModelTexture? MetallicRoughnessImage, float NormalScale, float AlphaCutoff);
/// <summary>保存PNG/JPEG编码字节与glTF采样参数，GPU上传由渲染层负责。</summary>
/// <param name="Encoded">图像编码字节。</param>
/// <param name="WrapS">U方向环绕枚举，默认REPEAT。</param>
/// <param name="WrapT">V方向环绕枚举，默认REPEAT。</param>
/// <param name="MinFilter">缩小过滤枚举，默认LINEAR_MIPMAP_LINEAR。</param>
/// <param name="MagFilter">放大过滤枚举，默认LINEAR。</param>
public sealed record ModelTexture(byte[] Encoded, int WrapS = 10497, int WrapT = 10497, int MinFilter = 9987, int MagFilter = 9729);
/// <summary>轨道覆盖的局部TRS分量：Translation为平移，Rotation为四元数旋转，Scale为缩放。</summary>
public enum TrackProperty { Translation, Rotation, Scale }
/// <summary>保存单节点单TRS分量的关键帧采样数据。</summary>
/// <param name="Node">目标模型节点索引。</param>
/// <param name="Property">被覆盖的局部TRS分量。</param>
/// <param name="Times">按时间排列的关键帧，单位秒。</param>
/// <param name="Values">与Times对应的值；旋转为四元数XYZW，向量使用XYZ。</param>
/// <param name="Step">为true时保持前一关键帧，否则使用线性或最短弧插值。</param>
public sealed record AnimationTrack(int Node, TrackProperty Property, float[] Times, Vector4[] Values, bool Step);
/// <summary>具有统一时间范围的命名轨道集合，不包含运行时状态。</summary>
/// <param name="Name">片段唯一名称。</param>
/// <param name="Duration">片段时长，单位秒。</param>
/// <param name="Tracks">该片段覆盖的节点TRS轨道。</param>
public sealed record AnimationClip(string Name, float Duration, AnimationTrack[] Tracks);
