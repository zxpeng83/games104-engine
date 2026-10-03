using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace G104.Engine.Scene;

public static class SceneSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 128,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

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

    public static void Save(SceneDocument document, string path, string assetRoot)
    {
        SceneValidator.Validate(document);
        ValidateAssets(document, assetRoot);
        Save(document, path);
    }

    public static SceneDocument Clone(SceneDocument document) => JsonSerializer.Deserialize<SceneDocument>(Serialize(document), Options)!;
    public static string Serialize(SceneDocument document) => JsonSerializer.Serialize(document, Options);

    public static void ValidateAssets(SceneDocument document, string assetRoot)
    {
        foreach (var item in document.Objects.Concat(document.Templates.Select(template => template.Defaults)))
        {
            if (item.ModelPath is string model) AssetPath.Resolve(assetRoot, model, requireExisting: true);
            if (item.Material.BaseColorTexture is string color) AssetPath.Resolve(assetRoot, color, requireExisting: true);
            if (item.Material.NormalTexture is string normal) AssetPath.Resolve(assetRoot, normal, requireExisting: true);
        }
    }

    private static void RejectDuplicateProperties(string json)
    {
        using var parsed = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 128 });
        if (parsed.RootElement.ValueKind != JsonValueKind.Object || !parsed.RootElement.TryGetProperty("schemaVersion", out _) ||
            !parsed.RootElement.TryGetProperty("objects", out var objects) || objects.ValueKind != JsonValueKind.Array)
            throw new SceneValidationException("Scene JSON requires schemaVersion and an objects array.");
        foreach (var item in objects.EnumerateArray())
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("id", out _))
                throw new SceneValidationException("Every scene object requires an explicit stable id.");
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
