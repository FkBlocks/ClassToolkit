using ClassToolkit.Core.Services;
using System.Windows;

namespace ClassToolkit;

public partial class App : Application
{
    private Mutex? _appMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _appMutex = new Mutex(true, @"Local\ClassToolkit.SingleInstance", out bool createdNew);
        
        if (!createdNew)
        {
            _appMutex.Dispose();
            _appMutex = null;
            MessageBox.Show("已有一个工具箱正在运行", "正在运行",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown();
            return;
        }

        base.OnStartup(e);
        DataFileInitializer.Ensure();
    }
}