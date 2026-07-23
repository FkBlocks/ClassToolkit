using ClassToolkit.Core.Controls;
using ClassToolkit.Core.Services;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace ClassToolkit.RandomName;

public partial class MainWindow : CustomWindow
{
    public MainWindow()
    {
        LogService.Init("RandomName");
        InitializeComponent();
        UpdateDisplay();
        LoadName();
        InitPickMode();
        LogService.Info($"成功启动随机点名，名字数量：{namesCount}");
    }
    /// <summary>
    /// 点名模式
    /// </summary>
    private enum PickMode { Name, StudentId }
    PickMode CurrentPickMode => _pickMode;
    public bool IsPickNameMode => _pickMode == PickMode.Name;

    /// <summary>
    /// 定义
    /// </summary>
    private const int DEFAULT_COUNT = 1;   // 默认计数器
    private const int MIN_COUNT = 1;       // 计数器最小值
    // 名单路径，调用Cores中的方法软编码拼路径
    private string namesPath = ClassToolkit.Core.Utilities.DataPathHelper.GetDataPath("names.txt");
    private readonly List<string> DefaultName = new List<string>
    {
        "张三", "李四", "王五", "赵六",
        "小明", "小红", "小芳", "小虎"
    };    // 默认名单

    private List<string> Names = new List<string>();             // 名单
    private List<string> _selectedItems = new List<string>();    // 抽取的名单
    private int namesCount = 0;    // 名单总数

    private int _count = DEFAULT_COUNT;

    private PickMode _pickMode = PickMode.Name;     // 是否为点名模式
    private SolidColorBrush _accentBrush = null!;   // 背景缓存刷，用于适应主题

    /// <summary>
    /// 减法按钮按下，计数器减一，达到最小值，不操作
    /// </summary>
    private void MinusButtonClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_count > MIN_COUNT) _count--;
        UpdateDisplay();
    }

    /// <summary>
    /// 加法按钮按下，计数器加一，如果达到名单总数，不操作
    /// </summary>
    private void AddButtonClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_count < namesCount) _count++;
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
    /// 点名按钮 Click 入口
    /// </summary>
    private void Call_Click(object sender, RoutedEventArgs e)
    {
        if (Names == null || Names.Count == 0)
        {
            MessageBox.Show("名单为空，无法点名。请检查 names.txt 文件。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DoPick(_count);
        ShowNames();
    }

    /// <summary>
    /// 执行抽取逻辑
    /// </summary>
    private void DoPick(int times)
    {
        if (IsPickNameMode) PickNamesMode(times);
        else PickIdsMode(times);
    }

    /// <summary>
    /// 点名字
    /// </summary>
    /// <param name="times">次数</param>
    private void PickNamesMode(int times)
    {
        // 安全兜底
        if (Names == null || Names.Count == 0) return;

        // 生成索引列表
        var indices = Enumerable.Range(0, Names.Count).ToList();

        // 打乱
        for (int i = 0; i < times; i++)
        {
            int j = Random.Shared.Next(i, indices.Count);
            (indices[i], indices[j]) = (indices[j], indices[i]);    // Fisher-Yates 部分洗牌
        }

        // 取出
        _selectedItems = indices.Take(times).Select(idx => Names[idx]).ToList();
    }

    /// <summary>
    /// 点学号
    /// </summary>
    /// <param name="times">次数</param>
    private void PickIdsMode(int times)
    {
        if (Names == null || Names.Count == 0) return;
        if (times > Names.Count) times = Names.Count;

        var indices = Enumerable.Range(0, Names.Count).ToList();
        for (int i = 0; i < times; i++)
        {
            int j = Random.Shared.Next(i, indices.Count);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // 学号 = 索引 + 1（转字符串）
        _selectedItems = indices.Take(times).Select(idx => (idx + 1).ToString()).ToList();
    }
    /// <summary>
    /// 在新窗口中展示抽取结果
    /// </summary>
    private void ShowNames()
    {
        if (_selectedItems == null || _selectedItems.Count == 0) return;

        string title = IsPickNameMode ? "点名结果" : "点学号结果";
        var resultWindow = new ResultWindow(_selectedItems, title, _accentBrush);
        resultWindow.ShowDialog();
    }

    /// <summary>
    /// 读取名单，并统计总数
    /// </summary>
    private void LoadName()
    {
        try
        {
            Names = File.ReadAllLines(namesPath, Encoding.UTF8).ToList();
            namesCount = Names.Count;
            if (Names.SequenceEqual(DefaultName))
            {
                LogService.Info("使用默认名单");
                MessageBox.Show("你当前使用的是默认名单！\n" +
                    "这不会影响程序的使用，但请前往设置填写或导入本班名单\n" +
                    "你也可以在 程序目录/data/names.txt 文件中手动填写，确保每一行一个名字",
                    "默认名单", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
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

    /// <summary>
    /// 初始化点名模式切换控件，从标题栏颜色软编码提取饱和强调色
    /// </summary>
    private void InitPickMode()
    {
        _accentBrush = CreateAccentBrush();
        _pickMode = PickMode.Name;
        ApplyPickModeStyle();
    }

    /// <summary>
    /// 从 TitleBarBackground 资源色提取色相，拉高饱和度与亮度作为选中强调色
    /// </summary>
    private SolidColorBrush CreateAccentBrush()
    {
        var titleBarBrush = (SolidColorBrush)Application.Current.FindResource("TitleBarBackground");
        Color titleBarColor = titleBarBrush.Color;
        Color saturated = BoostSaturation(titleBarColor);
        return new SolidColorBrush(saturated);
    }

    /// <summary>
    /// RGB → HSL → 饱和度和亮度调节 → RGB，返回高饱和的强调色
    /// </summary>
    private static Color BoostSaturation(Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double l = (max + min) / 2.0;

        double h = 0, s = 0;
        double d = max - min;

        if (d > 0.0001)
        {
            s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

            if (Math.Abs(max - r) < 0.0001)
                h = ((g - b) / d + (g < b ? 6 : 0)) / 6.0;
            else if (Math.Abs(max - g) < 0.0001)
                h = ((b - r) / d + 2) / 6.0;
            else
                h = ((r - g) / d + 4) / 6.0;
        }

        // 拉高饱和度到 0.78，得到鲜明但不刺眼的强调色
        s = 0.78;

        // 保留来源明度关系：浅色标题栏 → 较亮的强调色，深色标题栏 → 较深的强调色
        // 同时保证白字对比度（L ≤ 0.52 即可通过 WCAG AA）
        if (l > 0.5)
            l = 0.50;  // 浅色主题：明快的中等蓝色
        else
            l = 0.38;  // 深色主题：浓郁的深蓝/靛色

        return HslToRgb(h, s, l);
    }

    private static Color HslToRgb(double h, double s, double l)
    {
        double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        double p = 2 * l - q;

        return Color.FromRgb(
            (byte)Math.Round(HueToRgb(p, q, h + 1.0 / 3.0) * 255),
            (byte)Math.Round(HueToRgb(p, q, h) * 255),
            (byte)Math.Round(HueToRgb(p, q, h - 1.0 / 3.0) * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
        return p;
    }

    /// <summary>
    /// 模式切换按钮点击
    /// </summary>
    private void OnPickModeChanged(object sender, RoutedEventArgs e)
    {
        if (sender == BtnPickName)
            _pickMode = PickMode.Name;
        else
            _pickMode = PickMode.StudentId;

        ApplyPickModeStyle();
    }

    /// <summary>
    /// 根据当前选中模式刷新分段控件的背景色与前景色
    /// </summary>
    private void ApplyPickModeStyle()
    {
        var normalFg = (Brush)Application.Current.FindResource("TextPrimary");

        if (_pickMode == PickMode.Name)
        {
            BtnPickNameBorder.Background = _accentBrush;
            BtnPickName.Foreground = Brushes.White;
            BtnPickIdBorder.Background = Brushes.Transparent;
            BtnPickId.Foreground = normalFg;
        }
        else
        {
            BtnPickIdBorder.Background = _accentBrush;
            BtnPickId.Foreground = Brushes.White;
            BtnPickNameBorder.Background = Brushes.Transparent;
            BtnPickName.Foreground = normalFg;
        }
    }

    /// <summary>
    /// 系统主题变化时重新计算强调色并刷新分段控件
    /// </summary>
    public void RefreshAccent()
    {
        _accentBrush = CreateAccentBrush();
        ApplyPickModeStyle();
    }
}