using ClassToolkit.Core.Services;
using System.Windows;

namespace ClassToolkit;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DataFileInitializer.Ensure();
    }
}