using ClassToolkit.Core.Controls;
using ClassToolkit.Core.Services;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ClassToolkit.Settings;

public partial class MainWindow : CustomWindow
{
    private readonly ConfigService _config = new();
    private JsonObject _settings = null!;

    private bool _initializing = true;

    /// <summary>用户自定义主题色（hex 或 null=跟随主题默认）。</summary>
    private string? _accentOverride;

    public MainWindow()
    {
        InitializeComponent();

        CategoryList.SelectionChanged += OnCategoryChanged;
        SldBallSize.ValueChanged += OnBallSizeChanged;
        CmbTheme.SelectionChanged += OnThemeChanged;

        LoadSettings();
        _initializing = false;
    }

    // ═══════════════ 字典辅助：带默认值的类型读取 ═══════════════

    private static string GetStr(JsonObject obj, string key, string fallback = "") =>
        obj[key]?.GetValue<string>() ?? fallback;

    private static bool GetBool(JsonObject obj, string key, bool fallback = false) =>
        obj[key]?.GetValue<bool>() ?? fallback;

    private static int GetInt(JsonObject obj, string key, int fallback = 0) =>
        obj[key]?.GetValue<int>() ?? fallback;

    // ═══════════════ 配置读写 ═══════════════

    /// <summary>
    /// 加载 config.json → JsonObject 字典 → 填入 UI 控件。
    /// 字典中不存在的 key 走 fallback 默认值。
    /// </summary>
    private void LoadSettings()
    {
        _settings = _config.Load();

        // ── 通用 ──
        SetComboBoxByContent(CmbLanguage, GetStr(_settings, "Language", "简体中文"));
        ChkAutoStart.IsChecked = GetBool(_settings, "AutoStart");
        SetComboBoxByContent(CmbCloseBehavior, GetStr(_settings, "CloseBehavior", "直接退出"));

        // ── 外观 ──
        SetComboBoxByContent(CmbTheme, GetStr(_settings, "Theme", "跟随系统"));
        SldBallSize.Value = GetInt(_settings, "BallSize", 60);
        TxtBallSizeValue.Text = $"当前: {(int)SldBallSize.Value} px";

        // 主题色：config 无值 → 跟随主题；有值 → 作为全局强调色生效
        string accent = GetStr(_settings, "AccentColor", "");
        _accentOverride = string.IsNullOrEmpty(accent) ? null : accent;

        TxtMenuFontSize.Text = GetInt(_settings, "MenuFontSize", 14).ToString();

        // ── 工具 ──
        TxtToolsJsonPath.Text = GetStr(_settings, "ToolsJsonPath", "data/tools.json");
        SetComboBoxByContent(CmbToolLaunchMode, GetStr(_settings, "ToolLaunchMode", "由 Windows 决定（推荐）"));
        TxtToolsDirectory.Text = GetStr(_settings, "ToolsDirectory", "Tools");

        // ── 主题应用（最后执行，覆盖所有颜色；含用户自定义主题色）──
        ApplyThemeWithOverrides();
    }

    /// <summary>
    /// 把 UI 控件当前值写入 JsonObject 字典 → 持久化到 config.json。
    /// 添加新配置：这里加一行 config["新key"] = 控件值。
    /// </summary>
    private void SaveSettings()
    {
        // ── 通用 ──
        _settings["Language"] = GetComboBoxContent(CmbLanguage);
        _settings["AutoStart"] = ChkAutoStart.IsChecked ?? false;
        _settings["CloseBehavior"] = GetComboBoxContent(CmbCloseBehavior);

        // ── 外观 ──
        _settings["Theme"] = GetComboBoxContent(CmbTheme);
        _settings["BallSize"] = (int)SldBallSize.Value;
        _settings["AccentColor"] = _accentOverride;   // null → 移除键 = 跟随主题
        _settings["MenuFontSize"] = int.TryParse(TxtMenuFontSize.Text, out var fs) ? fs : 14;

        // ── 工具 ──
        _settings["ToolsJsonPath"] = TxtToolsJsonPath.Text;
        _settings["ToolLaunchMode"] = GetComboBoxContent(CmbToolLaunchMode);
        _settings["ToolsDirectory"] = TxtToolsDirectory.Text;

        _config.Save(_settings);
    }

    // ═══════════════ 分类切换 ═══════════════

    private void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is not ListBoxItem item || item.Tag is not string tag)
            return;

        PageGeneral.Visibility = Visibility.Collapsed;
        PageAppearance.Visibility = Visibility.Collapsed;
        PageTools.Visibility = Visibility.Collapsed;
        PageAbout.Visibility = Visibility.Collapsed;

        switch (tag)
        {
            case "general": PageGeneral.Visibility = Visibility.Visible; break;
            case "appearance": PageAppearance.Visibility = Visibility.Visible; break;
            case "tools": PageTools.Visibility = Visibility.Visible; break;
            case "about": PageAbout.Visibility = Visibility.Visible; break;
        }
    }

    // ═══════════════ 外观页交互 ═══════════════

    /// <summary>切换主题下拉框 → 即时预览，不持久化（保存仍由"应用"按钮负责）</summary>
    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing) return;  // 构造函数阶段跳过，避免窗口未初始化时崩
        ApplyThemeWithOverrides();
    }

    private void OnBallSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        TxtBallSizeValue.Text = $"当前: {(int)e.NewValue} px";
    }

    private void AccentColor_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string tag)
            return;

        // Tag 为空串 = "跟随主题"（清除自定义主题色）
        _accentOverride = string.IsNullOrEmpty(tag) ? null : tag;
        ApplyThemeWithOverrides();
    }

    /// <summary>
    /// 应用当前主题，并把用户自定义主题色作为全局强调色一并写入。
    /// 按钮/选中项/标题栏等所有窗口的 DynamicResource 自动跟随。
    /// </summary>
    private void ApplyThemeWithOverrides()
    {
        var overrides = new Dictionary<string, Color>();
        if (!string.IsNullOrEmpty(_accentOverride) &&
            ColorConverter.ConvertFromString(_accentOverride) is Color c)
            overrides = ThemeService.BuildAccentOverrides(GetComboBoxContent(CmbTheme), c);

        ThemeService.Apply(GetComboBoxContent(CmbTheme), overrides);
    }

    // ═══════════════ ComboBox 辅助 ═══════════════

    private static void SetComboBoxByContent(ComboBox cmb, string content)
    {
        foreach (ComboBoxItem item in cmb.Items)
        {
            if (item.Content?.ToString() == content)
            {
                cmb.SelectedItem = item;
                return;
            }
        }
        cmb.SelectedIndex = 0;
    }

    private static string GetComboBoxContent(ComboBox cmb) =>
        cmb.SelectedItem is ComboBoxItem item ? item.Content?.ToString() ?? "" : "";

    // ═══════════════ 应用按钮 ═══════════════

    private async void ApplySettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveSettings();
            LoadSettings();
        }
        catch (Exception ex)
        {
            Dialog.Show($"保存失败: {ex.Message}", title:"错误");
            return;
        }

        TxtSavedHint.Visibility = Visibility.Visible;
        BtnApply.IsEnabled = false;
        await Task.Delay(2000);
        TxtSavedHint.Visibility = Visibility.Collapsed;
        BtnApply.IsEnabled = true;
    }
}
