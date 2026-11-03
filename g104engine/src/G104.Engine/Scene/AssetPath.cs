namespace G104.Engine.Scene;

/// <summary>统一资产相对路径与根目录约束，拒绝穿越和不安全文件名。</summary>
public static class AssetPath
{
    /// <summary>校验相对路径各段，并统一为正斜线形式；不访问资产内容。</summary>
    public static string Normalize(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains(':') || relativePath.Contains('\0') || Path.IsPathRooted(relativePath))
            throw new SceneValidationException($"Asset path must be relative: {relativePath}.");
        // 先拒绝绝对路径、盘符和空段，再允许交给文件系统拼接。
        var normalized = relativePath.Replace('\\', '/');
        var parts = normalized.Split('/');
        if (parts.Any(part => part is "" or "." or ".." || part.IndexOfAny(['<', '>', '"', '|', '?', '*']) >= 0 || part.Any(char.IsControl) || part.EndsWith(' ') || part.EndsWith('.') || IsDeviceName(part)))
            throw new SceneValidationException($"Asset path contains an unsafe segment: {relativePath}.");
        return string.Join('/', parts);
    }

    /// <summary>解析资产根内路径并检查已有链接；可选要求文件已存在。</summary>
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

    /// <summary>用相对路径关系拒绝越出资产根的候选位置。</summary>
    private static void RequireWithin(string root, string candidate)
    {
        var relative = Path.GetRelativePath(root, candidate);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new SceneValidationException("Asset path escapes its root.");
    }

    /// <summary>识别Windows保留设备名，包含带扩展名的同名路径段。</summary>
    private static bool IsDeviceName(string segment)
    {
        var stem = segment.Split('.')[0].ToUpperInvariant();
        return stem is "CON" or "PRN" or "AUX" or "NUL" ||
            stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] is >= '1' and <= '9';
    }
}
