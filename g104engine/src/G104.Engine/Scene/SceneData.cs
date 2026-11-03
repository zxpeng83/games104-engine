using OpenTK.Mathematics;

namespace G104.Engine.Scene;

// DTO只保存可编辑设计数据；GPU、物理和音频句柄不进入JSON。
/// <summary>可序列化的三分量数据；具体空间和单位由使用字段定义。</summary>
/// <param name="X">第一分量，对应向量X或颜色R。</param>
/// <param name="Y">第二分量，对应向量Y或颜色G。</param>
/// <param name="Z">第三分量，对应向量Z或颜色B。</param>
public readonly record struct Float3(float X, float Y, float Z)
{
    /// <summary>三个分量均为0，作为默认位移或偏移。</summary>
    public static Float3 Zero => new(0, 0, 0);
    /// <summary>三个分量均为1，作为默认单位缩放。</summary>
    public static Float3 One => new(1, 1, 1);
    /// <summary>按分量转换为OpenTK向量，不改变坐标约定。</summary>
    public Vector3 ToVector() => new(X, Y, Z);
    /// <summary>复制OpenTK向量分量，得到可保存的三分量数据。</summary>
    public static Float3 From(Vector3 value) => new(value.X, value.Y, value.Z);
}

/// <summary>可序列化四元数；使用前由变换校验归一化，不存欧拉角。</summary>
/// <param name="X">四元数虚部X分量。</param>
/// <param name="Y">四元数虚部Y分量。</param>
/// <param name="Z">四元数虚部Z分量。</param>
/// <param name="W">四元数实部分量；单位旋转时为1。</param>
public readonly record struct RotationData(float X, float Y, float Z, float W)
{
    /// <summary>表示不改变方向的单位旋转。</summary>
    public static RotationData Identity => new(0, 0, 0, 1);
    /// <summary>按X/Y/Z/W分量转换为OpenTK四元数，不在此处校验。</summary>
    public Quaternion ToQuaternion() => new(X, Y, Z, W);
    /// <summary>复制OpenTK四元数分量，保留为可保存的旋转数据。</summary>
    public static RotationData From(Quaternion value) => new(value.X, value.Y, value.Z, value.W);
}

/// <summary>场景对象相对父级的局部TRS；根对象的局部空间即世界空间。</summary>
public sealed class TransformData
{
    /// <summary>局部平移，单位米；角色作为根对象时表示脚底位置。</summary>
    public Float3 Position { get; set; } = Float3.Zero;
    /// <summary>局部四元数旋转，使用时要求有限且非零。</summary>
    public RotationData Rotation { get; set; } = RotationData.Identity;
    /// <summary>局部正缩放比例；作为父级时只允许统一缩放。</summary>
    public Float3 Scale { get; set; } = Float3.One;
}

/// <summary>
/// 对象职责：Group只组织层级；StaticMesh为静态外观；Player/Npc为可移动角色。
/// Button为交互机关；Door为被控门；Goal为目标触发区；PointLight为点光源。
/// </summary>
public enum ObjectKind { Group, StaticMesh, Player, Npc, Button, Door, Goal, PointLight }
/// <summary>几何来源：Cube立方体、Plane平面、Sphere球体、Model外部glTF模型。</summary>
public enum PrimitiveKind { Cube, Plane, Sphere, Model }
/// <summary>碰撞形状：None不创建碰撞体，Box盒形，Capsule胶囊形。</summary>
public enum ColliderKind { None, Box, Capsule }
/// <summary>渲染路径：Forward直接光照绘制，Deferred先写G-buffer再统一光照。</summary>
public enum RenderPipelineMode { Forward, Deferred }

/// <summary>设计材质；普通几何直接使用参数，模型按导入材质与允许的覆盖绘制。</summary>
public sealed class MaterialData
{
    /// <summary>线性基础色；普通几何直接使用，模型作为导入基础色的实例tint。</summary>
    public Float3 BaseColor { get; set; } = new(0.65f, 0.65f, 0.65f);
    /// <summary>普通几何的金属度，范围0到1；模型使用导入因素。</summary>
    public float Metallic { get; set; }
    /// <summary>普通几何的粗糙度，范围0到1；模型使用导入因素。</summary>
    public float Roughness { get; set; } = 0.6f;
    /// <summary>资产根内的基础色贴图相对路径；null表示没有设计覆盖。</summary>
    public string? BaseColorTexture { get; set; }
    /// <summary>资产根内的切线空间法线贴图相对路径；按线性数据采样。</summary>
    public string? NormalTexture { get; set; }
}

/// <summary>局部碰撞形状设计；运行时由物理世界创建并拥有原生资源。</summary>
public sealed class ColliderData
{
    /// <summary>物理形状类别；None表示不建立碰撞体。</summary>
    public ColliderKind Kind { get; set; } = ColliderKind.Box;
    /// <summary>局部尺寸，单位米；盒形用全边长，胶囊用X直径与Y总高。</summary>
    public Float3 Size { get; set; } = Float3.One;
    /// <summary>碰撞中心相对对象原点的局部偏移，单位米。</summary>
    public Float3 Center { get; set; } = Float3.Zero;
    /// <summary>创建刚体时使用的非负摩擦系数。</summary>
    public float Friction { get; set; } = 0.6f;
}

/// <summary>可编辑对象设计，使用稳定ID连接层级、模板与交互目标。</summary>
public sealed class SceneObjectData
{
    /// <summary>对象稳定身份；保存、历史恢复和运行映射均按此ID关联。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>用于编辑器和诊断的对象名称，不承担唯一身份。</summary>
    public string Name { get; set; } = "Object";
    /// <summary>父对象ID；null为根，角色必须保持根对象。</summary>
    public Guid? ParentId { get; set; }
    /// <summary>对象的玩法或组织职责，决定创建哪些运行消费者。</summary>
    public ObjectKind Kind { get; set; } = ObjectKind.StaticMesh;
    /// <summary>绘制所用基本几何或导入模型类型。</summary>
    public PrimitiveKind Primitive { get; set; } = PrimitiveKind.Cube;
    /// <summary>相对父级的设计TRS；Play中的修改仅发生在独立运行副本。</summary>
    public TransformData Transform { get; set; } = new();
    /// <summary>设计材质参数与贴图覆盖，不保存GPU句柄。</summary>
    public MaterialData Material { get; set; } = new();
    /// <summary>可选碰撞设计；null表示没有显式碰撞形状。</summary>
    public ColliderData? Collider { get; set; }
    /// <summary>导入模型在资产根内的相对路径；Model几何必须提供。</summary>
    public string? ModelPath { get; set; }
    /// <summary>实例来源模板ID；null表示独立对象。</summary>
    public string? TemplateId { get; set; }
    /// <summary>显式编辑过的模板字段名，只允许约定白名单。</summary>
    public List<string> TemplateOverrides { get; set; } = [];
    /// <summary>交互对象的显式目标引用，例如按钮控制的门。</summary>
    public Guid? TargetId { get; set; }
    /// <summary>是否生成渲染对象；不等同于关闭物理或玩法。</summary>
    public bool Visible { get; set; } = true;
    /// <summary>设计声明的数值参数；编辑只改已有键，具体单位由消费者定义。</summary>
    public Dictionary<string, float> Parameters { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>可保存的场景渲染配置，不包含后端资源。</summary>
public sealed class RenderSettings
{
    /// <summary>场景选用的Forward或Deferred渲染路径。</summary>
    public RenderPipelineMode Pipeline { get; set; } = RenderPipelineMode.Forward;
    /// <summary>世界空间方向光方向，使用时归一化。</summary>
    public Float3 SunDirection { get; set; } = new(-0.5f, -1, -0.4f);
    /// <summary>方向光非负线性RGB，与强度相乘形成辐射输入。</summary>
    public Float3 SunColor { get; set; } = new(1, 0.94f, 0.82f);
    /// <summary>方向光非负强度系数，作用于SunColor。</summary>
    public float SunIntensity { get; set; } = 3;
    /// <summary>色调映射前的正曝光倍率。</summary>
    public float Exposure { get; set; } = 1;
    /// <summary>是否启用当前方向光的阴影采样。</summary>
    public bool Shadows { get; set; } = true;
    /// <summary>是否对最终LDR画面执行FXAA抗锯齿。</summary>
    public bool Fxaa { get; set; } = true;
}

/// <summary>单层对象模板；实例通过深复制默认设计获得独立数据。</summary>
public sealed class ObjectTemplateData
{
    /// <summary>模板目录内唯一的文本键，供对象TemplateId引用。</summary>
    public string Id { get; set; } = "cube";
    /// <summary>实例化时复制的默认对象；不允许嵌套模板或外部对象引用。</summary>
    public SceneObjectData Defaults { get; set; } = new();
}

/// <summary>场景JSON根数据，统一保存版本、渲染、对象与模板设计。</summary>
public sealed class SceneDocument
{
    /// <summary>当前实现接受的场景数据版本。</summary>
    public const int CurrentVersion = 1;
    /// <summary>文件声明的数据版本；加载时必须匹配CurrentVersion。</summary>
    public int SchemaVersion { get; set; } = CurrentVersion;
    /// <summary>场景显示名称；不承担对象或模板的引用身份。</summary>
    public string Name { get; set; } = "Training Ground V1";
    /// <summary>该场景使用的光照、管线与后处理设计。</summary>
    public RenderSettings Rendering { get; set; } = new();
    /// <summary>场景对象集合；顺序不要求父先子后，构图时重排求值。</summary>
    public List<SceneObjectData> Objects { get; set; } = [];
    /// <summary>场景内可用的单层模板定义，不作为运行实例。</summary>
    public List<ObjectTemplateData> Templates { get; set; } = [];
}
