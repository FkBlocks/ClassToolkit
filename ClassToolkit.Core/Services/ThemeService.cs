using System.Windows;
using System.Windows.Media;

namespace ClassToolkit.Core.Services;

/// <summary>
/// 主题切换服务。维护浅色/深色两套色板，保持相同的明暗层级关系。
/// 调用 Apply() 将色板写入 Application.Resources，所有 DynamicResource 自动跟随。
/// 所有颜色键统一由 <see cref="Keys"/> 定义，XAML 与 C# 共用，禁止散落魔法字符串。
/// </summary>
public static class ThemeService
{
    /// <summary>
    /// 全局颜色键。XAML 中用作 {DynamicResource xxx} 的键名，
    /// C# 代码通过 ThemeService.Keys.xxx 引用，避免魔法字符串。
    /// </summary>
    public static class Keys
    {
        // ── 基础结构色 ──
        public const string TitleBarBackground   = "TitleBarBackground";
        public const string TitleBarButtonHover  = "TitleBarButtonHover";
        public const string SidebarBackground    = "SidebarBackground";
        public const string SidebarHover         = "SidebarHover";
        public const string SidebarSelected      = "SidebarSelected";
        public const string SidebarAccent        = "SidebarAccent";
        public const string ContentBackground    = "ContentBackground";
        public const string SeparatorColor       = "SeparatorColor";
        public const string TextPrimary          = "TextPrimary";
        public const string TextSecondary        = "TextSecondary";
        public const string ControlBorder        = "ControlBorder";
        public const string ControlBackground    = "ControlBackground";

        // ── 语义色 ──
        public const string AccentForeground     = "AccentForeground";   // 强调色底上的文字
        public const string AccentBorder         = "AccentBorder";       // 强调按钮描边
        public const string SuccessColor         = "SuccessColor";       // 成功/正向提示
        public const string ErrorColor           = "ErrorColor";         // 错误/危险提示

        // ── 悬浮球/悬浮菜单（跟随主题）──
        public const string FloatBallBackground  = "FloatBallBackground";
        public const string FloatBallStroke      = "FloatBallStroke";
        public const string MenuBackground       = "MenuBackground";
        public const string MenuForeground       = "MenuForeground";
        public const string MenuHover            = "MenuHover";
    }

    private static readonly Dictionary<string, Color> LightPalette = new()
    {
        [Keys.TitleBarBackground] = Color.FromRgb(0xF0, 0xF0, 0xFF),
        [Keys.TitleBarButtonHover] = Color.FromRgb(0xE3, 0xE3, 0xE6),
        [Keys.SidebarBackground] = Color.FromRgb(0xF0, 0xF0, 0xF2),
        [Keys.SidebarHover] = Color.FromRgb(0xE3, 0xE3, 0xE6),
        [Keys.SidebarSelected] = Color.FromRgb(0xD6, 0xD6, 0xDB),
        [Keys.SidebarAccent] = Color.FromRgb(0x4A, 0x7C, 0xF7),
        [Keys.ContentBackground] = Color.FromRgb(0xFA, 0xFA, 0xFC),
        [Keys.SeparatorColor] = Color.FromRgb(0xD1, 0xD1, 0xD6),
        [Keys.TextPrimary] = Color.FromRgb(0x1D, 0x1D, 0x20),
        [Keys.TextSecondary] = Color.FromRgb(0x6E, 0x6E, 0x78),
        [Keys.ControlBorder] = Color.FromRgb(0xD1, 0xD1, 0xD6),
        [Keys.ControlBackground] = Color.FromRgb(0xFF, 0xFF, 0xFF),

        [Keys.AccentForeground] = Color.FromRgb(0xFF, 0xFF, 0xFF),
        [Keys.AccentBorder] = Color.FromRgb(0x3B, 0x6C, 0xD4),
        [Keys.SuccessColor] = Color.FromRgb(0x38, 0xA1, 0x69),
        [Keys.ErrorColor] = Color.FromRgb(0xE5, 0x3E, 0x3E),

        [Keys.FloatBallBackground] = Color.FromArgb(0xEF, 0xF0, 0xF0, 0xF7),
        [Keys.FloatBallStroke] = Color.FromRgb(0x4A, 0x7C, 0xF7),
        [Keys.MenuBackground] = Color.FromRgb(0xF5, 0xF5, 0xF7),
        [Keys.MenuForeground] = Color.FromRgb(0x1D, 0x1D, 0x20),
        [Keys.MenuHover] = Color.FromRgb(0xE3, 0xE3, 0xE6),
    };

    private static readonly Dictionary<string, Color> DarkPalette = new()
    {
        [Keys.TitleBarBackground] = Color.FromRgb(0x24, 0x24, 0x38),
        [Keys.TitleBarButtonHover] = Color.FromRgb(0x35, 0x35, 0x40),
        [Keys.SidebarBackground] = Color.FromRgb(0x1E, 0x1E, 0x22),
        [Keys.SidebarHover] = Color.FromRgb(0x2D, 0x2D, 0x32),
        [Keys.SidebarSelected] = Color.FromRgb(0x35, 0x35, 0x3A),
        [Keys.SidebarAccent] = Color.FromRgb(0x5B, 0x8A, 0xF7),
        [Keys.ContentBackground] = Color.FromRgb(0x25, 0x25, 0x28),
        [Keys.SeparatorColor] = Color.FromRgb(0x3F, 0x3F, 0x46),
        [Keys.TextPrimary] = Color.FromRgb(0xF0, 0xF0, 0xF2),
        [Keys.TextSecondary] = Color.FromRgb(0xA0, 0xA0, 0xA8),
        [Keys.ControlBorder] = Color.FromRgb(0x45, 0x45, 0x4A),
        [Keys.ControlBackground] = Color.FromRgb(0x3A, 0x3A, 0x40),

        [Keys.AccentForeground] = Color.FromRgb(0xFF, 0xFF, 0xFF),
        [Keys.AccentBorder] = Color.FromRgb(0x5B, 0x8A, 0xF7),
        [Keys.SuccessColor] = Color.FromRgb(0x48, 0xBB, 0x78),
        [Keys.ErrorColor] = Color.FromRgb(0xFC, 0x81, 0x81),

        [Keys.FloatBallBackground] = Color.FromArgb(0xEF, 0x12, 0x12, 0x12),
        [Keys.FloatBallStroke] = Color.FromRgb(0xFF, 0xFF, 0xFF),
        [Keys.MenuBackground] = Color.FromRgb(0x16, 0x16, 0x16),
        [Keys.MenuForeground] = Color.FromRgb(0xF0, 0xF0, 0xF2),
        [Keys.MenuHover] = Color.FromRgb(0x2D, 0x2D, 0x32),
    };

    /// <summary>
    /// 将主题应用到 Application 层资源。
    /// 所有使用 DynamicResource 的控件自动跟随，无需额外处理。
    /// </summary>
    /// <param name="theme">主题名称："深色" / "浅色" / "跟随系统"。</param>
    /// <param name="overrides">用户自定义覆盖色（如自定义分隔线颜色），优先级高于色板。</param>
    public static void Apply(string theme, IReadOnlyDictionary<string, Color>? overrides = null)
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

        if (overrides != null)
            foreach (var (key, color) in overrides)
                resources[key] = new SolidColorBrush(color);
    }

    /// <summary>
    /// 代码后置取色入口：优先应用级资源，找不到时回退当前色板。
    /// 供代码构建的控件使用，替代各文件里 FindResource + try/catch + 硬编码兜底 的重复代码。
    /// </summary>
    public static Brush GetBrush(string key)
    {
        if (Application.Current?.TryFindResource(key) is Brush brush)
            return brush;
        var palette = IsSystemDarkMode() ? DarkPalette : LightPalette;
        return new SolidColorBrush(palette.TryGetValue(key, out var c) ? c : Colors.Transparent);
    }

    /// <summary>
    /// 由用户自定义"主题色"生成全局覆盖色板。
    /// 同一主题色会被应用到强调色（按钮/选中项/高亮）、按钮描边、标题栏底色，
    /// 使主题色在所有窗口全局生效；深/浅主题下自动推导合适的明暗层次。
    /// </summary>
    public static Dictionary<string, Color> BuildAccentOverrides(string theme, Color accent)
    {
        bool dark = theme switch
        {
            "深色" => true,
            "浅色" => false,
            _ => IsSystemDarkMode(),
        };

        // 按钮描边：浅色主题加深一档保证轮廓，深色主题提亮一档保证可见
        var border = dark ? Blend(accent, Colors.White, 0.78) : Blend(accent, Colors.Black, 0.78);
        // 标题栏底色：浅色主题混白形成柔和淡彩，深色主题混深海军蓝形成浓郁配色
        var title = dark
            ? Blend(accent, Color.FromRgb(0x1A, 0x1A, 0x22), 0.30)
            : Blend(accent, Colors.White, 0.12);

        return new Dictionary<string, Color>
        {
            [Keys.SidebarAccent] = accent,
            [Keys.AccentBorder] = border,
            [Keys.TitleBarBackground] = title,
        };
    }

    /// <summary>按比例混合两个颜色：t 越大越偏向 c1。</summary>
    private static Color Blend(Color c1, Color c2, double t)
    {
        byte L(byte a, byte b) => (byte)Math.Round(a * t + b * (1 - t));
        return Color.FromRgb(L(c1.R, c2.R), L(c1.G, c2.G), L(c1.B, c2.B));
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
