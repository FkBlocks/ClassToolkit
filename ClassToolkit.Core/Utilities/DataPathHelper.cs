namespace ClassToolkit.Core.Utilities;

/// <summary>
/// 解析 data/ 目录的绝对路径。
/// 开发时向上查找解决方案根目录（含 .git / .sln）直接读写源码，
/// 发布/生产时向上查找已存在的 data/ 目录实现共享，
/// 若都不存在则回退到可执行文件旁的 data/ 目录。
/// </summary>
public static class DataPathHelper
{
    /// <summary>data/ 目录的绝对路径</summary>
    public static string DataDirectory => _dataDir.Value;

    private static readonly Lazy<string> _dataDir = new(() =>
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // 1) 开发环境：向上查找解决方案根目录
        string? solutionRoot = FindSolutionRoot(baseDir);
        if (solutionRoot != null)
            return System.IO.Path.Combine(solutionRoot, "data");

        // 2) 生产环境：向上查找已存在的 data/ 目录
        //    主程序在根目录，子工具在 Tools/ClassToolkit.XXX/ 下，
        //    向上找到主程序创建的 data/ 后复用，避免重复创建。
        string? existingData = FindExistingDataDir(baseDir);
        if (existingData != null)
            return existingData;

        // 3) 兜底：在可执行文件旁创建 data/
        return System.IO.Path.Combine(baseDir, "data");
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

    /// <summary>
    /// 向上查找已存在的 data/ 目录。
    /// 用于生产环境下 Tools/ClassToolkit.XXX 子目录中的工具
    /// 共享主程序创建的 data/ 目录。
    /// </summary>
    private static string? FindExistingDataDir(string startDir)
    {
        string dir = startDir;
        while (dir != null)
        {
            string candidate = System.IO.Path.Combine(dir, "data");
            if (System.IO.Directory.Exists(candidate))
                return candidate;

            string? parent = System.IO.Path.GetDirectoryName(dir);
            if (parent == null || parent == dir) break;
            dir = parent;
        }
        return null;
    }
}
