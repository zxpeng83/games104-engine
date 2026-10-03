using OpenTK.Mathematics;

namespace G104.Engine.Assets;

// 导入结果不含GL句柄，因而可以在无图形上下文的验证入口中检查。
public sealed class GltfModel
{
    public required string SourcePath { get; init; }
    public required ModelNode[] Nodes { get; init; }
    public required int[] EvaluationOrder { get; init; }
    public required ModelPrimitive[] Primitives { get; init; }
    public required ModelSkin[] Skins { get; init; }
    public required IReadOnlyDictionary<string, AnimationClip> Clips { get; init; }
    public const int MaximumJoints = 128;

    public NodePose[] CreateDefaultPose() => Nodes.Select(n => n.DefaultPose).ToArray();

    public void EvaluateWorld(NodePose[] local, Matrix4[] world)
    {
        foreach (int i in EvaluationOrder)
        {
            Matrix4 matrix = local[i].Matrix;
            world[i] = Nodes[i].Parent < 0 ? matrix : matrix * world[Nodes[i].Parent];
        }
    }

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

public readonly record struct NodePose(Vector3 Translation, Quaternion Rotation, Vector3 Scale)
{
    public Matrix4 Matrix => Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Translation);
    public static NodePose Blend(NodePose a, NodePose b, float weight) => new(
        Vector3.Lerp(a.Translation, b.Translation, weight), ShortSlerp(a.Rotation, b.Rotation, weight), Vector3.Lerp(a.Scale, b.Scale, weight));

    public static Quaternion ShortSlerp(Quaternion a, Quaternion b, float amount)
    {
        if (a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W < 0) b = new(-b.X, -b.Y, -b.Z, -b.W);
        return Quaternion.Normalize(Quaternion.Slerp(a, b, amount));
    }
}

public sealed record ModelNode(string Name, int Parent, NodePose DefaultPose);
public sealed record ModelSkin(int[] Joints, Matrix4[] InverseBind);
// 零填充的UV顶点槽不能代表源mesh具备UV；设计材质覆盖必须消费原始能力信息。
public sealed record ModelPrimitive(int Node, int Skin, float[] Vertices, uint[] Indices, ModelMaterial Material, bool HasUv0, int PrimitiveIndex);
public sealed record ModelMaterial(Vector4 BaseColor, float Metallic, float Roughness, bool DoubleSided,
    ModelTexture? BaseColorImage, ModelTexture? NormalImage, ModelTexture? MetallicRoughnessImage, float NormalScale, float AlphaCutoff);
public sealed record ModelTexture(byte[] Encoded, int WrapS = 10497, int WrapT = 10497, int MinFilter = 9987, int MagFilter = 9729);
public enum TrackProperty { Translation, Rotation, Scale }
public sealed record AnimationTrack(int Node, TrackProperty Property, float[] Times, Vector4[] Values, bool Step);
public sealed record AnimationClip(string Name, float Duration, AnimationTrack[] Tracks);
