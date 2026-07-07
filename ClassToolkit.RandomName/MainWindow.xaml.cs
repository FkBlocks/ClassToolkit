using ClassToolkit.Core.Controls;
using ClassToolkit.Core.Services;

namespace ClassToolkit.RandomName;

public partial class MainWindow : CustomWindow
{
    public MainWindow()
    {
        LogService.Init("RandomName");
        InitializeComponent();
        
    }
}