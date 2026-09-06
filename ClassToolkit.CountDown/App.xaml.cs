using ClassToolkit.Core.Services;
using System.Windows;

namespace ClassToolkit.CountDown;

/// <summary>
/// 倒计时应用入口。启动时通过 ThemeBootstrap 应用主题并监听系统主题变化。
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeBootstrap.Initialize();
    }
}
