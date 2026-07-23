using ClassToolkit.Core.Services;
using Microsoft.Win32;
using System.Windows;

namespace ClassToolkit.RandomName;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DataFileInitializer.Ensure();

        // 启动时读配置，在窗口显示前应用主题
        ApplyTheme();

        // 监听系统主题变化（如 Windows 切换深色/浅色模式）
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
        base.OnExit(e);
    }

    /// <summary>
    /// 读取配置并应用主题
    /// </summary>
    private static void ApplyTheme()
    {
        var config = new ConfigService().Load();
        string theme = config["Theme"]?.GetValue<string>() ?? "跟随系统";
        ThemeService.Apply(theme);
    }

    /// <summary>
    /// 系统设置变化时重新应用主题并刷新窗口强调色
    /// </summary>
    private void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General) return;

        ApplyTheme();

        if (Current.MainWindow is MainWindow mw)
            mw.RefreshAccent();
    }
}
