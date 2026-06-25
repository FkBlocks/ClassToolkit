using System.Windows;
using System.Windows.Media;

namespace ClassToolkit.Settings.Services;

/// <summary>
/// 主题切换服务。维护浅色/深色两套色板，设计上保持相同的明暗层级关系。
/// 所有 UI 必须用 DynamicResource 引用颜色，调用 Apply() 即可全局换色。
/// </summary>
public static class ThemeService
{
    /// <summary>浅色色板 —— 保留原始设计的灰度层级</summary>
    private static readonly Dictionary<string, Color> LightPalette = new()
    {
        ["TitleBarBackground"] = Color.FromRgb(0xF0, 0xF0, 0xFF),   // 标题栏（淡蓝紫调，与侧边栏灰区分）
        ["SidebarBackground"] = Color.FromRgb(0xF0, 0xF0, 0xF2),   // 最浅灰
        ["SidebarHover"]      = Color.FromRgb(0xE3, 0xE3, 0xE6),   // hover 稍深
        ["SidebarSelected"]   = Color.FromRgb(0xD6, 0xD6, 0xDB),   // 选中更深
        ["SidebarAccent"]     = Color.FromRgb(0x4A, 0x7C, 0xF7),   // 蓝色强调
        ["ContentBackground"] = Color.FromRgb(0xFA, 0xFA, 0xFC),   // 内容白
        ["SeparatorColor"]    = Color.FromRgb(0xD1, 0xD1, 0xD6),   // 分界线
        ["TextPrimary"]       = Color.FromRgb(0x1D, 0x1D, 0x20),   // 主文字黑
        ["TextSecondary"]     = Color.FromRgb(0x6E, 0x6E, 0x78),   // 次要文字灰
        ["ControlBorder"]     = Color.FromRgb(0xD1, 0xD1, 0xD6),   // 输入框边框
        ["ControlBackground"] = Color.FromRgb(0xFF, 0xFF, 0xFF),   // 输入框白
    };

    /// <summary>深色色板 —— 默认层级完全对应，同样的相对明暗关系</summary>
    private static readonly Dictionary<string, Color> DarkPalette = new()
    {
        ["TitleBarBackground"] = Color.FromRgb(0x24, 0x24, 0x38),   // 标题栏（深底淡蓝紫调，与侧边栏区分）
        ["SidebarBackground"] = Color.FromRgb(0x1E, 0x1E, 0x22),   // 最深深灰
        ["SidebarHover"]      = Color.FromRgb(0x2D, 0x2D, 0x32),   // hover 稍亮
        ["SidebarSelected"]   = Color.FromRgb(0x35, 0x35, 0x3A),   // 选中更亮
        ["SidebarAccent"]     = Color.FromRgb(0x5B, 0x8A, 0xF7),   // 蓝色稍亮（暗底上更显眼）
        ["ContentBackground"] = Color.FromRgb(0x25, 0x25, 0x28),   // 内容深灰
        ["SeparatorColor"]    = Color.FromRgb(0x3F, 0x3F, 0x46),   // 分界线（暗底上更亮）
        ["TextPrimary"]       = Color.FromRgb(0xF0, 0xF0, 0xF2),   // 主文字白
        ["TextSecondary"]     = Color.FromRgb(0xA0, 0xA0, 0xA8),   // 次要文字浅灰
        ["ControlBorder"]     = Color.FromRgb(0x45, 0x45, 0x4A),   // 输入框边框
        ["ControlBackground"] = Color.FromRgb(0x3A, 0x3A, 0x40),   // 输入框深灰（比内容背景明显亮，下拉框能看清）
    };

    /// <summary>
    /// 应用主题。传入 "深色"、"浅色" 或 "跟随系统"。
    /// "跟随系统" 会读取 Windows 设置。
    /// </summary>
    public static void Apply(string theme, ResourceDictionary resources)
    {
        var palette = ResolvePalette(theme);

        foreach (var (key, color) in palette)
        {
            // XAML 定义的 SolidColorBrush 会被 WPF 自动冻结（frozen），不能原地改 Color，
            // 必须整体替换为新刷子。DynamicResource 引用了 key 的控件自动跟随新值。
            resources[key] = new SolidColorBrush(color);
        }
    }

    private static Dictionary<string, Color> ResolvePalette(string theme) => theme switch
    {
        "深色" => DarkPalette,
        "浅色" => LightPalette,
        "跟随系统" => IsSystemDarkMode() ? DarkPalette : LightPalette,
        _ => LightPalette
    };

    private static bool IsSystemDarkMode()
    {
        try
        {
            const string key = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            object? value = Microsoft.Win32.Registry.GetValue(key, "AppsUseLightTheme", 1);
            return value is int v && v == 0;
        }
        catch
        {
            return false;
        }
    }
}
