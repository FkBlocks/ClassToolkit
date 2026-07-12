using ClassToolkit.Core.Controls;
using ClassToolkit.Core.Services;

namespace ClassToolkit.RandomName;

public partial class MainWindow : CustomWindow
{
    public MainWindow()
    {
        LogService.Init("RandomName");
        InitializeComponent();
        UpdateDisplay();
    }

    /// <summary>
    /// 常量定义
    /// </summary>
    private const int DEFAULT_COUNT = 1;
    private const int MIN_COUNT = 1;
    private string namesPath = ClassToolkit.Core.Utilities.DataPathHelper.GetDataPath("names.txt");

    private int _count = DEFAULT_COUNT;

    /// <summary>
    /// 减法按钮按下，计数器减一
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MinusButtonClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_count > MIN_COUNT) _count--;
        UpdateDisplay();
    }

    /// <summary>
    /// 加法按钮按下，计数器加一
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AddButtonClick(object sender, System.Windows.RoutedEventArgs e)
    {
        _count++;
        UpdateDisplay();
    }

    /// <summary>
    /// 刷新显示
    /// </summary>
    private void UpdateDisplay()
    {
        ChoiceTimes.Text = _count.ToString();
    }

    private void Call(object sender, System.Windows.RoutedEventArgs e)
    {

    }
}