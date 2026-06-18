namespace ClassToolkit.Core.Utilities;

/// <summary>
/// 解析 data/ 目录的绝对路径。
/// 开发时向上查找解决方案根目录（含 .git / .sln）直接读写源码，
/// 发布/生产时回退到可执行文件旁的 data/ 目录。
/// </summary>
public static class DataPathHelper
{
    /// <summary>data/ 目录的绝对路径（含尾部分隔符）</summary>
    public static string DataDirectory => _dataDir.Value;

    private static readonly Lazy<string> _dataDir = new(() =>
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string? solutionRoot = FindSolutionRoot(baseDir);
        string root = solutionRoot ?? baseDir;
        return System.IO.Path.Combine(root, "data");
    });

    /// <summary>拼接 data/ 下的子路径</summary>
    public static string GetDataPath(string relativePath) =>
        System.IO.Path.Combine(DataDirectory, relativePath);

    /// <summary>向上查找包含 .git 或 *.sln 的目录作为方案根</summary>
    private static string? FindSolutionRoot(string startDir)
    {
        string dir = startDir;
        while (dir != null)
        {
            if (System.IO.Directory.EnumerateFiles(dir, "*.sln").Any() ||
                System.IO.Directory.Exists(System.IO.Path.Combine(dir, ".git")))
                return dir;

            string? parent = System.IO.Path.GetDirectoryName(dir);
            if (parent == null || parent == dir) break;
            dir = parent;
        }
        return null;
    }
}
