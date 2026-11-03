namespace G104.Engine.Assets;

/// <summary>将资产访问限制在规范化根目录内，并拒绝目录链接绕过检查。</summary>
public sealed class AssetRoot
{
    /// <summary>带末尾分隔符的绝对根路径，用于区分目录前缀与同名前缀目录。</summary>
    private readonly string root;
    /// <summary>规范化根目录；构造过程不创建目录或加载资产。</summary>
    public AssetRoot(string path) => root = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

    /// <summary>校验本地相对路径及沿途链接，返回根目录内的绝对路径。</summary>
    public string Resolve(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) || relativePath.Contains(':'))
            throw new InvalidDataException($"资产必须是根目录内的相对路径：{relativePath}");
        string path = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"资产路径越出根目录：{relativePath}");
        // 拒绝链接目录，避免规范化字符串通过后又由文件系统转向根目录之外。
        for (string? item = path; item is not null && item.Length >= root.Length; item = Path.GetDirectoryName(item))
            if ((File.Exists(item) || Directory.Exists(item)) && (File.GetAttributes(item) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"V1资产路径不允许符号链接：{relativePath}");
        return path;
    }
}
