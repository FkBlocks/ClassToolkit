using ClassToolkit.Core.Utilities;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClassToolkit.Core.Services;

/// <summary>
/// 确保 data/ 下所有必须文件和目录存在，缺少则用默认内容创建。
/// 在应用启动时调用一次即可。
/// </summary>
public static class DataFileInitializer
{
    public static void Ensure()
    {
        Directory.CreateDirectory(DataPathHelper.DataDirectory);
        EnsureLogDir();
        EnsureToolsJson();
        EnsureConfigJson();
        EnsureNamesTxt();
    }

    /// <summary>
    /// 确保 log/ 目录存在。
    /// LogService 的静态构造函数也会创建此目录，但它的触发时机取决于
    /// LogService 首次被访问的时间点。这里提前创建以保证 data/log/ 始终就绪。
    /// </summary>
    private static void EnsureLogDir()
    {
        string dir = DataPathHelper.GetDataPath("log");
        Directory.CreateDirectory(dir);
    }

    private static void EnsureToolsJson()
    {
        string path = DataPathHelper.GetDataPath("tools.json");
        if (File.Exists(path)) return;

        var tools = new Dictionary<string, string>
        {
            ["随机点名"] = "Tools\\ClassToolkit.RandomName\\ClassToolkit.RandomName.exe",
            ["倒计时"]   = "Tools\\ClassToolkit.CountDown\\ClassToolkit.CountDown.exe",
            ["音量恢复"] = "Tools\\ClassToolkit.VolumeRecovery\\ClassToolkit.VolumeRecovery.exe",
            ["设置"]     = "Tools\\ClassToolkit.Settings\\ClassToolkit.Settings.exe"
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        File.WriteAllText(path, JsonSerializer.Serialize(tools, options), Encoding.UTF8);
    }

    private static void EnsureConfigJson()
    {
        string path = DataPathHelper.GetDataPath("config/config.json");
        if (File.Exists(path)) return;

        var config = new JsonObject
        {
            ["Language"]        = "简体中文",
            ["AutoStart"]       = true,
            ["CloseBehavior"]   = "直接退出",
            ["Theme"]           = "跟随系统",
            ["BallSize"]        = 60,
            ["SeparatorColor"]  = "#D1D1D6",
            ["MenuFontSize"]    = 14,
            ["ToolsJsonPath"]   = "data/tools.json",
            ["ToolLaunchMode"]  = "由 Windows 决定（推荐）",
            ["ToolsDirectory"]  = "Tools"
        };

        new ConfigService(path).Save(config);
    }

    private static void EnsureNamesTxt()
    {
        string path = DataPathHelper.GetDataPath("names.txt");
        if (File.Exists(path)) return;

        File.WriteAllText(path, string.Join("\n",
            "张三", "李四", "王五", "赵六",
            "小明", "小红", "小芳", "小虎"
        ), Encoding.UTF8);
    }
}
