using ClassToolkit.Core.Services;
using System.Windows;

namespace ClassToolkit.RandomName;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DataFileInitializer.Ensure();

        // 统一主题引导：读配置应用主题 + 监听系统深浅色切换
        ThemeBootstrap.Initialize();
        ThemeBootstrap.ThemeApplied += RefreshAccentAfterThemeChange;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ThemeBootstrap.ThemeApplied -= RefreshAccentAfterThemeChange;
        base.OnExit(e);
    }

    /// <summary>
    /// 主题重新应用（含系统深浅色切换）后刷新分段控件的强调色。
    /// </summary>
    private static void RefreshAccentAfterThemeChange()
    {
        if (Current.MainWindow is MainWindow mw)
            mw.RefreshAccent();
    }
}
