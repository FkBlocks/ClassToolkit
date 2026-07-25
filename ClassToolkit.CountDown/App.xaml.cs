using ClassToolkit.Core.Services;
using Microsoft.Win32;
using System.Windows;

namespace ClassToolkit.CountDown;

/// <summary>
/// 倒计时应用入口。启动时应用主题并监听系统主题变化。
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 启动时读取配置应用主题，并注册系统主题变化监听。
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ApplyTheme();
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
    }

    /// <summary>
    /// 退出时取消事件订阅。
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
        base.OnExit(e);
    }

    /// <summary>
    /// 读取配置并应用主题到 Application 层资源。
    /// </summary>
    private static void ApplyTheme()
    {
        var config = new ConfigService().Load();
        string theme = config["Theme"]?.GetValue<string>() ?? "跟随系统";
        ThemeService.Apply(theme);
    }

    /// <summary>
    /// 系统设置（浅色/深色模式）变化时重新应用主题。
    /// </summary>
    private static void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General) return;
        ApplyTheme();
    }
}
