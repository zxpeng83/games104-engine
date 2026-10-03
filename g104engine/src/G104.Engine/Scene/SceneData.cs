using OpenTK.Mathematics;

namespace G104.Engine.Scene;

// DTO只保存可编辑设计数据；GPU、物理和音频句柄不进入JSON。
public readonly record struct Float3(float X, float Y, float Z)
{
    public static Float3 Zero => new(0, 0, 0);
    public static Float3 One => new(1, 1, 1);
    public Vector3 ToVector() => new(X, Y, Z);
    public static Float3 From(Vector3 value) => new(value.X, value.Y, value.Z);
}

public readonly record struct RotationData(float X, float Y, float Z, float W)
{
    public static RotationData Identity => new(0, 0, 0, 1);
    public Quaternion ToQuaternion() => new(X, Y, Z, W);
    public static RotationData From(Quaternion value) => new(value.X, value.Y, value.Z, value.W);
}

public sealed class TransformData
{
    public Float3 Position { get; set; } = Float3.Zero;
    public RotationData Rotation { get; set; } = RotationData.Identity;
    public Float3 Scale { get; set; } = Float3.One;
}

public enum ObjectKind { Group, StaticMesh, Player, Npc, Button, Door, Goal, PointLight }
public enum PrimitiveKind { Cube, Plane, Sphere, Model }
public enum ColliderKind { None, Box, Capsule }
public enum RenderPipelineMode { Forward, Deferred }

public sealed class MaterialData
{
    public Float3 BaseColor { get; set; } = new(0.65f, 0.65f, 0.65f);
    public float Metallic { get; set; }
    public float Roughness { get; set; } = 0.6f;
    public string? BaseColorTexture { get; set; }
    public string? NormalTexture { get; set; }
}

public sealed class ColliderData
{
    public ColliderKind Kind { get; set; } = ColliderKind.Box;
    public Float3 Size { get; set; } = Float3.One;
    public Float3 Center { get; set; } = Float3.Zero;
    public float Friction { get; set; } = 0.6f;
}

public sealed class SceneObjectData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Object";
    public Guid? ParentId { get; set; }
    public ObjectKind Kind { get; set; } = ObjectKind.StaticMesh;
    public PrimitiveKind Primitive { get; set; } = PrimitiveKind.Cube;
    public TransformData Transform { get; set; } = new();
    public MaterialData Material { get; set; } = new();
    public ColliderData? Collider { get; set; }
    public string? ModelPath { get; set; }
    public string? TemplateId { get; set; }
    public List<string> TemplateOverrides { get; set; } = [];
    public Guid? TargetId { get; set; }
    public bool Visible { get; set; } = true;
    public Dictionary<string, float> Parameters { get; set; } = new(StringComparer.Ordinal);
}

public sealed class RenderSettings
{
    public RenderPipelineMode Pipeline { get; set; } = RenderPipelineMode.Forward;
    public Float3 SunDirection { get; set; } = new(-0.5f, -1, -0.4f);
    public Float3 SunColor { get; set; } = new(1, 0.94f, 0.82f);
    public float SunIntensity { get; set; } = 3;
    public float Exposure { get; set; } = 1;
    public bool Shadows { get; set; } = true;
    public bool Fxaa { get; set; } = true;
}

public sealed class ObjectTemplateData
{
    public string Id { get; set; } = "cube";
    public SceneObjectData Defaults { get; set; } = new();
}

public sealed class SceneDocument
{
    public const int CurrentVersion = 1;
    public int SchemaVersion { get; set; } = CurrentVersion;
    public string Name { get; set; } = "Training Ground V1";
    public RenderSettings Rendering { get; set; } = new();
    public List<SceneObjectData> Objects { get; set; } = [];
    public List<ObjectTemplateData> Templates { get; set; } = [];
}
