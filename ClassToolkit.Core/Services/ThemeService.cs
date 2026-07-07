using System.Windows;
using System.Windows.Media;

namespace ClassToolkit.Core.Services;

/// <summary>
/// 主题切换服务。维护浅色/深色两套色板，保持相同的明暗层级关系。
/// 调用 Apply() 将色板写入 Window.Resources，所有 DynamicResource 自动跟随。
/// </summary>
public static class ThemeService
{
    private static readonly Dictionary<string, Color> LightPalette = new()
    {
        ["TitleBarBackground"] = Color.FromRgb(0xF0, 0xF0, 0xFF),
        ["TitleBarButtonHover"] = Color.FromRgb(0xE3, 0xE3, 0xE6),
        ["SidebarBackground"] = Color.FromRgb(0xF0, 0xF0, 0xF2),
        ["SidebarHover"] = Color.FromRgb(0xE3, 0xE3, 0xE6),
        ["SidebarSelected"] = Color.FromRgb(0xD6, 0xD6, 0xDB),
        ["SidebarAccent"] = Color.FromRgb(0x4A, 0x7C, 0xF7),
        ["ContentBackground"] = Color.FromRgb(0xFA, 0xFA, 0xFC),
        ["SeparatorColor"] = Color.FromRgb(0xD1, 0xD1, 0xD6),
        ["TextPrimary"] = Color.FromRgb(0x1D, 0x1D, 0x20),
        ["TextSecondary"] = Color.FromRgb(0x6E, 0x6E, 0x78),
        ["ControlBorder"] = Color.FromRgb(0xD1, 0xD1, 0xD6),
        ["ControlBackground"] = Color.FromRgb(0xFF, 0xFF, 0xFF),
    };

    private static readonly Dictionary<string, Color> DarkPalette = new()
    {
        ["TitleBarBackground"] = Color.FromRgb(0x24, 0x24, 0x38),
        ["TitleBarButtonHover"] = Color.FromRgb(0x35, 0x35, 0x40),
        ["SidebarBackground"] = Color.FromRgb(0x1E, 0x1E, 0x22),
        ["SidebarHover"] = Color.FromRgb(0x2D, 0x2D, 0x32),
        ["SidebarSelected"] = Color.FromRgb(0x35, 0x35, 0x3A),
        ["SidebarAccent"] = Color.FromRgb(0x5B, 0x8A, 0xF7),
        ["ContentBackground"] = Color.FromRgb(0x25, 0x25, 0x28),
        ["SeparatorColor"] = Color.FromRgb(0x3F, 0x3F, 0x46),
        ["TextPrimary"] = Color.FromRgb(0xF0, 0xF0, 0xF2),
        ["TextSecondary"] = Color.FromRgb(0xA0, 0xA0, 0xA8),
        ["ControlBorder"] = Color.FromRgb(0x45, 0x45, 0x4A),
        ["ControlBackground"] = Color.FromRgb(0x3A, 0x3A, 0x40),
    };

    /// <summary>
    /// 将主题应用到 Application 层资源。
    /// 所有使用 DynamicResource 的控件自动跟随，无需额外处理。
    /// </summary>
    public static void Apply(string theme)
    {
        var palette = theme switch
        {
            "深色" => DarkPalette,
            "浅色" => LightPalette,
            "跟随系统" => IsSystemDarkMode() ? DarkPalette : LightPalette,
            _ => LightPalette,
        };

        var resources = Application.Current.Resources;
        foreach (var (key, color) in palette)
            resources[key] = new SolidColorBrush(color);
    }

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
