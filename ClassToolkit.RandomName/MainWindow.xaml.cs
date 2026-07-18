using ClassToolkit.Core.Controls;
using ClassToolkit.Core.Services;
using System.IO;
using System.Text;
using System.Windows;

namespace ClassToolkit.RandomName;

public partial class MainWindow : CustomWindow
{
    public MainWindow()
    {
        LogService.Init("RandomName");
        InitializeComponent();
        UpdateDisplay();
        LoadName();
        LogService.Info($"成功启动随机点名，名字数量：{namesCount}");
    }

    /// <summary>
    /// 常量定义
    /// </summary>
    private const int DEFAULT_COUNT = 1;
    private const int MIN_COUNT = 1;
    private string namesPath = ClassToolkit.Core.Utilities.DataPathHelper.GetDataPath("names.txt");
    private readonly List<string> DefaultName = new List<string>
    {
        "张三", "李四", "王五", "赵六",
        "小明", "小红", "小芳", "小虎"
    };

    public List<string>? names;
    public int namesCount = 0;

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

    // TODO 点名逻辑
    /// <summary>
    /// 点名逻辑
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Call(object sender, System.Windows.RoutedEventArgs e)
    {

    }

    private void LoadName()
    {

        try
        {
            names = File.ReadAllLines(namesPath, Encoding.UTF8).ToList();
            if (names.SequenceEqual(DefaultName))
            {
                LogService.Info("使用默认名单");
                MessageBox.Show("你当前使用的是默认名单！\n" +
                    "这不会影响程序的使用，但请前往设置填写或导入本班名单\n" +
                    "你也可以在 程序目录/data/names.txt 文件中手动填写，确保每一行一个名字",
                    "默认名单", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            namesCount = names.Count;

        }
        catch (Exception ex)
        {
            LogService.Error($"读取名单错误：{ex}");
            MessageBox.Show($"读取名单发生致命错误：{ex}。\n 详情见 程序目录/data/log/running.log 日志文件", "错误", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown(1);
            return;
        }
    }
}