using ClassToolkit.Core.Services;
using ClassToolkit.Settings.Services;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ClassToolkit.Settings;

public partial class MainWindow : Window
{
    private readonly ConfigService _config = new();
    private JsonObject _settings = null!;

    private bool _initializing = true;

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
        ApplySeparatorColor(GetStr(_settings, "SeparatorColor", "#D1D1D6"));
        TxtMenuFontSize.Text = GetInt(_settings, "MenuFontSize", 14).ToString();

        // ── 工具 ──
        TxtToolsJsonPath.Text = GetStr(_settings, "ToolsJsonPath", "data/tools.json");
        SetComboBoxByContent(CmbToolLaunchMode, GetStr(_settings, "ToolLaunchMode", "由 Windows 决定（推荐）"));
        TxtToolsDirectory.Text = GetStr(_settings, "ToolsDirectory", "Tools");

        // ── 主题应用（最后执行，覆盖所有颜色）──
        ThemeService.Apply(GetStr(_settings, "Theme", "跟随系统"), Resources);
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
        _settings["SeparatorColor"] = GetCurrentSeparatorColorHex();
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
        string theme = GetComboBoxContent(CmbTheme);
        ThemeService.Apply(theme, Resources);

        // 同步更新分隔线（它不受 DynamicResource 直接绑定）
        ApplySeparatorColor(GetCurrentSeparatorColorHex());
    }

    private void OnBallSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        TxtBallSizeValue.Text = $"当前: {(int)e.NewValue} px";
    }

    private void SeparatorColor_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string colorHex)
            ApplySeparatorColor(colorHex);
    }

    private void ApplySeparatorColor(string colorHex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        brush.Freeze();
        SepLine.Background = brush;
    }

    private string GetCurrentSeparatorColorHex()
    {
        if (SepLine.Background is SolidColorBrush scb)
            return $"#{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}";
        return "#D1D1D6";
    }

    // ═══════════════ 标题栏 ═══════════════

    private void TitleBar_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        else
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

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
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        TxtSavedHint.Visibility = Visibility.Visible;
        BtnApply.IsEnabled = false;
        await Task.Delay(2000);
        TxtSavedHint.Visibility = Visibility.Collapsed;
        BtnApply.IsEnabled = true;
    }
}
