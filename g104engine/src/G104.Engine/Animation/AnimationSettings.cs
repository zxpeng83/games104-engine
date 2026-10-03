using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using G104.Engine.Assets;

namespace G104.Engine.Animation;

public sealed record AnimationClipMap(string Idle, string Walk, string Run, string JumpStart, string JumpLoop, string JumpLand)
{
    public IEnumerable<(string Role, string Clip)> Entries()
    {
        yield return ("idle", Idle); yield return ("walk", Walk); yield return ("run", Run);
        yield return ("jumpStart", JumpStart); yield return ("jumpLoop", JumpLoop); yield return ("jumpLand", JumpLand);
    }
}

// 共享定义不含游标/事件/当前状态；校验后不可修改，所有实例只读同一配置。
public sealed class AnimationSettings
{
    public const int CurrentSchemaVersion = 1;
    public const string DefaultAssetPath = "config/character-animation.json";
    public int SchemaVersion => CurrentSchemaVersion;
    public AnimationClipMap ClipMap { get; }
    public float WalkReferenceSpeed { get; }
    public float RunReferenceSpeed { get; }
    public float SpeedResponse { get; }
    public float TransitionSeconds { get; }
    public float JumpStartSeconds { get; }
    public float JumpLandSeconds { get; }
    public float MinJumpVelocity { get; }
    public ReadOnlyCollection<float> FootstepMarkers { get; }
    public static AnimationSettings Default { get; } = new(new("Idle_Loop", "Walk_Loop", "Jog_Fwd_Loop", "Jump_Start", "Jump_Loop", "Jump_Land"), 2.2f, 4.8f, 12, .12f, .18f, .22f, .1f, [.15f, .65f]);

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

    public static AnimationSettings Load(AssetRoot assets, string relativePath = DefaultAssetPath)
    {
        string path = assets.Resolve(relativePath);
        try { return Parse(File.ReadAllText(path)); }
        catch (Exception error) when (error is JsonException or InvalidDataException or IOException or ArgumentException)
        { throw new InvalidDataException($"动画配置加载失败：{relativePath}；{error.Message}", error); }
    }

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

    private static void Positive(float value, string field)
    { if (!float.IsFinite(value) || value <= 0) throw new InvalidDataException($"动画配置{field}必须为有限正数"); }
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

    private sealed class SettingsData
    {
        public SettingsData() { }
        public required int SchemaVersion { get; init; }
        public required ClipMapData? ClipMap { get; init; }
        public required float WalkReferenceSpeed { get; init; }
        public required float RunReferenceSpeed { get; init; }
        public required float SpeedResponse { get; init; }
        public required float TransitionSeconds { get; init; }
        public required float JumpStartSeconds { get; init; }
        public required float JumpLandSeconds { get; init; }
        public required float MinJumpVelocity { get; init; }
        public required float[]? FootstepMarkers { get; init; }
    }
    private sealed class ClipMapData
    {
        public ClipMapData() { }
        public required string? Idle { get; init; }
        public required string? Walk { get; init; }
        public required string? Run { get; init; }
        public required string? JumpStart { get; init; }
        public required string? JumpLoop { get; init; }
        public required string? JumpLand { get; init; }
    }
}
