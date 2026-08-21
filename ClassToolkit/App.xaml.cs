using ClassToolkit.Core.Services;
using ClassToolkit.Core.Controls;
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
            Dialog.Show("已有一个工具箱正在运行", title:"正在运行");
            Shutdown();
            return;
        }

        base.OnStartup(e);
        DataFileInitializer.Ensure();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_appMutex != null)
        {
            _appMutex.ReleaseMutex();
            _appMutex.Dispose();
            _appMutex = null;
        }

        base.OnExit(e);
    }
}