namespace G104.Engine.Scene;

public static class AssetPath
{
    public static string Normalize(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains(':') || relativePath.Contains('\0') || Path.IsPathRooted(relativePath))
            throw new SceneValidationException($"Asset path must be relative: {relativePath}.");
        var normalized = relativePath.Replace('\\', '/');
        var parts = normalized.Split('/');
        if (parts.Any(part => part is "" or "." or ".." || part.IndexOfAny(['<', '>', '"', '|', '?', '*']) >= 0 || part.Any(char.IsControl) || part.EndsWith(' ') || part.EndsWith('.') || IsDeviceName(part)))
            throw new SceneValidationException($"Asset path contains an unsafe segment: {relativePath}.");
        return string.Join('/', parts);
    }

    public static string Resolve(string assetRoot, string relativePath, bool requireExisting = false)
    {
        var normalized = Normalize(relativePath);
        var root = Path.GetFullPath(assetRoot);
        var candidate = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        RequireWithin(root, candidate);
        // 已有符号链接/联接也须留在资源根内，不能仅用文本前缀绕过根边界。
        var current = root;
        foreach (var segment in normalized.Split('/'))
        {
            current = Path.Combine(current, segment);
            FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
            if (info.Exists && info.LinkTarget is not null)
            {
                current = info.ResolveLinkTarget(true)?.FullName ?? throw new SceneValidationException($"Asset link is unresolved: {relativePath}.");
                RequireWithin(root, current);
            }
        }
        if (requireExisting && !File.Exists(candidate)) throw new SceneValidationException($"Asset file is missing: {relativePath}.");
        return candidate;
    }

    private static void RequireWithin(string root, string candidate)
    {
        var relative = Path.GetRelativePath(root, candidate);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new SceneValidationException("Asset path escapes its root.");
    }

    private static bool IsDeviceName(string segment)
    {
        var stem = segment.Split('.')[0].ToUpperInvariant();
        return stem is "CON" or "PRN" or "AUX" or "NUL" ||
            stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] is >= '1' and <= '9';
    }
}
