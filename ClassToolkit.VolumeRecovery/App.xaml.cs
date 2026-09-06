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
        ThemeBootstrap.Initialize();
    }
}
