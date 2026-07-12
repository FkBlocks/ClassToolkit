using ClassToolkit.Core.Services;
using System.Windows;

namespace ClassToolkit.RandomName;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DataFileInitializer.Ensure();

        // 启动时读配置，在窗口显示前应用主题
        var config = new ConfigService().Load();
        string theme = config["Theme"]?.GetValue<string>() ?? "跟随系统";
        ThemeService.Apply(theme);
    }
}
