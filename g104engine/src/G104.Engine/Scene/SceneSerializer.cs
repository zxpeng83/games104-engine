using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace G104.Engine.Scene;

/// <summary>执行严格JSON读写、设计深复制与资产引用校验，保存采用同目录替换。</summary>
public static class SceneSerializer
{
    /// <summary>区分字段大小写、拒绝未知字段并使用文本枚举，固定场景JSON合同。</summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 128,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    /// <summary>只读加载并校验大小、结构、设计与资产引用；失败不改原文件。</summary>
    public static SceneDocument Load(string path, string assetRoot)
    {
        var info = new FileInfo(path);
        if (info.Length > 16 * 1024 * 1024) throw new SceneValidationException("Scene JSON exceeds the 16 MiB limit.");
        var json = File.ReadAllText(path, Encoding.UTF8);
        RejectDuplicateProperties(json);
        var document = JsonSerializer.Deserialize<SceneDocument>(json, Options) ?? throw new SceneValidationException("Scene document cannot be null.");
        SceneValidator.Validate(document);
        ValidateAssets(document, assetRoot);
        return document;
    }

    /// <summary>校验设计后写临时文件并替换目标；此重载不检查资产是否存在。</summary>
    public static void Save(SceneDocument document, string path)
    {
        SceneValidator.Validate(document);
        var json = Serialize(document);
        var destination = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");
        try
        {
            // 先将新内容写入同目录随机文件并刷盘；目标只在完整写入后替换。
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            // 临时文件位于同一目录；Replace失败时旧文件与内存文档保持有效。
            if (File.Exists(destination)) File.Replace(temporary, destination, null);
            else File.Move(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    /// <summary>先校验设计及资产文件，再使用原子替换保存流程。</summary>
    public static void Save(SceneDocument document, string path, string assetRoot)
    {
        SceneValidator.Validate(document);
        ValidateAssets(document, assetRoot);
        Save(document, path);
    }

    /// <summary>通过同一JSON合同深复制设计数据，不复制运行时资源或句柄。</summary>
    public static SceneDocument Clone(SceneDocument document) => JsonSerializer.Deserialize<SceneDocument>(Serialize(document), Options)!;
    /// <summary>按统一选项输出设计文本，供保存、快照比较和历史脏状态使用。</summary>
    public static string Serialize(SceneDocument document) => JsonSerializer.Serialize(document, Options);

    /// <summary>检查场景对象和模板默认对象引用的模型与贴图均存在于资产根内。</summary>
    public static void ValidateAssets(SceneDocument document, string assetRoot)
    {
        foreach (var item in document.Objects.Concat(document.Templates.Select(template => template.Defaults)))
        {
            if (item.ModelPath is string model) AssetPath.Resolve(assetRoot, model, requireExisting: true);
            if (item.Material.BaseColorTexture is string color) AssetPath.Resolve(assetRoot, color, requireExisting: true);
            if (item.Material.NormalTexture is string normal) AssetPath.Resolve(assetRoot, normal, requireExisting: true);
        }
    }

    /// <summary>验证必要根字段和显式对象ID，并递归拒绝重复JSON字段。</summary>
    private static void RejectDuplicateProperties(string json)
    {
        using var parsed = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 128 });
        if (parsed.RootElement.ValueKind != JsonValueKind.Object || !parsed.RootElement.TryGetProperty("schemaVersion", out _) ||
            !parsed.RootElement.TryGetProperty("objects", out var objects) || objects.ValueKind != JsonValueKind.Array)
            throw new SceneValidationException("Scene JSON requires schemaVersion and an objects array.");
        foreach (var item in objects.EnumerateArray())
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("id", out _))
                throw new SceneValidationException("Every scene object requires an explicit stable id.");
        // 逐对象建立字段名集合，并继续检查嵌套对象和数组。
        void Visit(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject())
                {
                    if (!names.Add(property.Name)) throw new SceneValidationException($"Duplicate JSON field: {property.Name}.");
                    Visit(property.Value);
                }
            }
            else if (value.ValueKind == JsonValueKind.Array)
                foreach (var element in value.EnumerateArray()) Visit(element);
        }
        Visit(parsed.RootElement);
    }
}
