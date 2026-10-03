namespace G104.Engine.Assets;

public sealed class AssetRoot
{
    private readonly string root;
    public AssetRoot(string path) => root = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

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
