using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using G104.Engine.Assets;

namespace G104.Engine.Animation;

/// <summary>将内置动画角色映射到模型片段名，不定义任意状态图。</summary>
/// <param name="Idle">待机片段名。</param>
/// <param name="Walk">行走片段名。</param>
/// <param name="Run">跑步片段名。</param>
/// <param name="JumpStart">起跳片段名。</param>
/// <param name="JumpLoop">腾空循环片段名。</param>
/// <param name="JumpLand">落地片段名。</param>
public sealed record AnimationClipMap(string Idle, string Walk, string Run, string JumpStart, string JumpLoop, string JumpLand)
{
    /// <summary>以JSON角色名枚举映射，供统一校验和错误定位。</summary>
    public IEnumerable<(string Role, string Clip)> Entries()
    {
        yield return ("idle", Idle); yield return ("walk", Walk); yield return ("run", Run);
        yield return ("jumpStart", JumpStart); yield return ("jumpLoop", JumpLoop); yield return ("jumpLand", JumpLand);
    }
}

// 共享定义不含游标/事件/当前状态；校验后不可修改，所有实例只读同一配置。
/// <summary>校验后只读的角色动画共享定义，不保存实例游标、姿态或事件。</summary>
public sealed class AnimationSettings
{
    /// <summary>当前严格JSON配置支持的schema版本。</summary>
    public const int CurrentSchemaVersion = 1;
    /// <summary>实际渲染入口默认读取的资产相对配置路径。</summary>
    public const string DefaultAssetPath = "config/character-animation.json";
    /// <summary>报告当前内存定义使用的schema版本。</summary>
    public int SchemaVersion => CurrentSchemaVersion;
    /// <summary>四个内置状态及移动混合使用的片段名映射。</summary>
    public AnimationClipMap ClipMap { get; }
    /// <summary>行走参考速度，单位米/秒，用于混合权重和步态速率。</summary>
    public float WalkReferenceSpeed { get; }
    /// <summary>跑步参考速度，单位米/秒，必须大于行走参考速度。</summary>
    public float RunReferenceSpeed { get; }
    /// <summary>速度指数平滑的响应系数，单位秒的倒数。</summary>
    public float SpeedResponse { get; }
    /// <summary>状态切换时从旧姿态混入新姿态的时长，单位秒。</summary>
    public float TransitionSeconds { get; }
    /// <summary>起跳状态停留及片段重定时的时长，单位秒。</summary>
    public float JumpStartSeconds { get; }
    /// <summary>落地状态停留及片段重定时的时长，单位秒。</summary>
    public float JumpLandSeconds { get; }
    /// <summary>离地时区分主动起跳的严格大于阈值，单位米/秒。</summary>
    public float MinJumpVelocity { get; }
    /// <summary>排序且防御复制的步态事件相位，范围[0,1)，空集合禁用脚步。</summary>
    public ReadOnlyCollection<float> FootstepMarkers { get; }
    /// <summary>CPU直接构造实例时可用的默认定义；坏资产配置不会自动回退到此值。</summary>
    public static AnimationSettings Default { get; } = new(new("Idle_Loop", "Walk_Loop", "Jog_Fwd_Loop", "Jump_Start", "Jump_Loop", "Jump_Land"), 2.2f, 4.8f, 12, .12f, .18f, .22f, .1f, [.15f, .65f]);

    /// <summary>校验映射、有限数值和事件相位，再建立可由多实例共享的只读定义。</summary>
    public AnimationSettings(AnimationClipMap clipMap, float walkReferenceSpeed, float runReferenceSpeed, float speedResponse,
        float transitionSeconds, float jumpStartSeconds, float jumpLandSeconds, float minJumpVelocity, IEnumerable<float> footstepMarkers)
    {
        ArgumentNullException.ThrowIfNull(clipMap); ArgumentNullException.ThrowIfNull(footstepMarkers);
        foreach (var (role, clip) in clipMap.Entries())
            if (string.IsNullOrWhiteSpace(clip)) throw new InvalidDataException($"动画配置clipMap.{role}必须为非空clip名");
        Positive(walkReferenceSpeed, "walkReferenceSpeed"); Positive(runReferenceSpeed, "runReferenceSpeed");
        if (runReferenceSpeed <= walkReferenceSpeed) throw new InvalidDataException("动画配置runReferenceSpeed必须大于walkReferenceSpeed");
        Positive(speedResponse, "speedResponse"); Positive(transitionSeconds, "transitionSeconds");
        Positive(jumpStartSeconds, "jumpStartSeconds"); Positive(jumpLandSeconds, "jumpLandSeconds");
        if (!float.IsFinite(minJumpVelocity) || minJumpVelocity < 0) throw new InvalidDataException("动画配置minJumpVelocity必须为有限非负m/s");
        float[] markers = footstepMarkers.Take(33).ToArray();
        if (markers.Length > 32 || markers.Any(m => !float.IsFinite(m) || m < 0 || m >= 1) || markers.Distinct().Count() != markers.Length)
            throw new InvalidDataException("动画配置footstepMarkers须为0至32个唯一有限相位，范围[0,1)");
        Array.Sort(markers);
        ClipMap = clipMap; WalkReferenceSpeed = walkReferenceSpeed; RunReferenceSpeed = runReferenceSpeed; SpeedResponse = speedResponse;
        TransitionSeconds = transitionSeconds; JumpStartSeconds = jumpStartSeconds; JumpLandSeconds = jumpLandSeconds; MinJumpVelocity = minJumpVelocity;
        FootstepMarkers = Array.AsReadOnly(markers); // 防御复制，调用者原数组不能改变共享定义。
    }

    /// <summary>从受限资产路径加载严格JSON，并在失败时补充配置文件位置。</summary>
    public static AnimationSettings Load(AssetRoot assets, string relativePath = DefaultAssetPath)
    {
        string path = assets.Resolve(relativePath);
        try { return Parse(File.ReadAllText(path)); }
        catch (Exception error) when (error is JsonException or InvalidDataException or IOException or ArgumentException)
        { throw new InvalidDataException($"动画配置加载失败：{relativePath}；{error.Message}", error); }
    }

    /// <summary>拒绝重复、未知、缺失字段及不支持版本，再执行数值和映射校验。</summary>
    public static AnimationSettings Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        RejectDuplicateFields(document.RootElement, "$");
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        var data = JsonSerializer.Deserialize<SettingsData>(json, options) ?? throw new InvalidDataException("动画配置必须为对象");
        if (data.SchemaVersion != CurrentSchemaVersion) throw new InvalidDataException($"动画配置schemaVersion不支持：{data.SchemaVersion}");
        var clips = data.ClipMap ?? throw new InvalidDataException("动画配置clipMap不能为null");
        return new(new(clips.Idle!, clips.Walk!, clips.Run!, clips.JumpStart!, clips.JumpLoop!, clips.JumpLand!), data.WalkReferenceSpeed, data.RunReferenceSpeed,
            data.SpeedResponse, data.TransitionSeconds, data.JumpStartSeconds, data.JumpLandSeconds, data.MinJumpVelocity,
            data.FootstepMarkers ?? throw new InvalidDataException("动画配置footstepMarkers不能为null"));
    }

    /// <summary>要求角色模型具有全部映射片段、正时长及可用节点TRS轨道。</summary>
    public void ValidateCharacterModel(GltfModel model)
    {
        foreach (var (role, name) in ClipMap.Entries())
        {
            if (!model.Clips.TryGetValue(name, out var clip)) throw new InvalidDataException($"角色模型{model.SourcePath}缺少配置clipMap.{role}：{name}");
            if (!float.IsFinite(clip.Duration) || clip.Duration <= 0 || clip.Tracks.Length == 0 ||
                clip.Tracks.Any(t => t.Node < 0 || t.Node >= model.Nodes.Length || t.Times.Length == 0 || t.Values.Length != t.Times.Length))
                throw new InvalidDataException($"角色模型{model.SourcePath}的clipMap.{role}必须有正时长和TRS轨道：{name}");
        }
    }

    /// <summary>拒绝非有限数或非正数，并报告对应配置字段。</summary>
    private static void Positive(float value, string field)
    { if (!float.IsFinite(value) || value <= 0) throw new InvalidDataException($"动画配置{field}必须为有限正数"); }
    /// <summary>递归检查对象和数组中的重复字段，避免反序列化时被后值覆盖。</summary>
    private static void RejectDuplicateFields(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException($"动画配置重复字段：{path}.{property.Name}");
                RejectDuplicateFields(property.Value, path + "." + property.Name);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RejectDuplicateFields(item, path + "[]");
    }

    /// <summary>严格JSON反序列化载体；保留必填和可空信息供后续显式校验。</summary>
    private sealed class SettingsData
    {
        /// <summary>供JSON反序列化创建配置载体，不执行运行时定义校验。</summary>
        public SettingsData() { }
        /// <summary>配置声明的schema版本，必须与当前版本一致。</summary>
        public required int SchemaVersion { get; init; }
        /// <summary>原始片段映射；null由解析入口拒绝。</summary>
        public required ClipMapData? ClipMap { get; init; }
        /// <summary>原始行走参考速度，单位米/秒。</summary>
        public required float WalkReferenceSpeed { get; init; }
        /// <summary>原始跑步参考速度，单位米/秒。</summary>
        public required float RunReferenceSpeed { get; init; }
        /// <summary>原始速度响应系数，单位秒的倒数。</summary>
        public required float SpeedResponse { get; init; }
        /// <summary>原始姿态过渡时长，单位秒。</summary>
        public required float TransitionSeconds { get; init; }
        /// <summary>原始起跳状态时长，单位秒。</summary>
        public required float JumpStartSeconds { get; init; }
        /// <summary>原始落地状态时长，单位秒。</summary>
        public required float JumpLandSeconds { get; init; }
        /// <summary>原始主动起跳速度阈值，单位米/秒。</summary>
        public required float MinJumpVelocity { get; init; }
        /// <summary>原始步态相位数组，校验后复制到只读集合。</summary>
        public required float[]? FootstepMarkers { get; init; }
    }
    /// <summary>保存JSON中的原始片段名，使缺失和null在边界被明确拒绝。</summary>
    private sealed class ClipMapData
    {
        /// <summary>供JSON反序列化创建片段映射载体。</summary>
        public ClipMapData() { }
        /// <summary>原始待机片段名，空值由定义构造过程拒绝。</summary>
        public required string? Idle { get; init; }
        /// <summary>原始行走片段名，空值由定义构造过程拒绝。</summary>
        public required string? Walk { get; init; }
        /// <summary>原始跑步片段名，空值由定义构造过程拒绝。</summary>
        public required string? Run { get; init; }
        /// <summary>原始起跳片段名，空值由定义构造过程拒绝。</summary>
        public required string? JumpStart { get; init; }
        /// <summary>原始腾空循环片段名，空值由定义构造过程拒绝。</summary>
        public required string? JumpLoop { get; init; }
        /// <summary>原始落地片段名，空值由定义构造过程拒绝。</summary>
        public required string? JumpLand { get; init; }
    }
}
