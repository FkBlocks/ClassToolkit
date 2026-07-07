using ClassToolkit.Core.Services;
using System.Windows;

namespace ClassToolkit.VolumeRecovery;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigService().Load();
        string theme = config["Theme"]?.GetValue<string>() ?? "跟随系统";
        ThemeService.Apply(theme);
    }
}
