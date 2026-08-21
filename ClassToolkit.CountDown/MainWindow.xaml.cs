using ClassToolkit.Core.Controls;
using ClassToolkit.CountDown.Controls;
using System.Windows;
using System.Windows.Input;

namespace ClassToolkit.CountDown;

/// <summary>
/// 倒计时调节窗口。启动时显示，提供时/分/秒三轮盘和开始/重置按钮。
/// 点击"开始倒计时"后隐藏自身并打开全屏倒计时展示窗口。
/// </summary>
public partial class MainWindow : CustomWindow
{
    /// <summary>当前正在被拖拽的轮盘（无拖拽时为 null）。</summary>
    private NumberWheel? _activeWheel;

    /// <summary>三个轮盘，供按指针 X 坐标路由滑动使用。</summary>
    private readonly NumberWheel[] _wheels;

    /// <summary>
    /// 初始化调节窗口。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        _wheels = new[] { HourWheel, MinuteWheel, SecondWheel };
    }

    /// <summary>
    /// 重置按钮：将时/分/秒三个轮盘全部归零。
    /// </summary>
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        HourWheel.Value = 0;
        MinuteWheel.Value = 0;
        SecondWheel.Value = 0;
    }

    /// <summary>
    /// 开始倒计时：校验时间 > 0 后隐藏调节窗口，
    /// 以模态方式打开全屏倒计时展示窗口，关闭后恢复调节窗口。
    /// </summary>
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var time = new System.TimeSpan(
            HourWheel.Value,
            MinuteWheel.Value,
            SecondWheel.Value);

        if (time.TotalSeconds <= 0)
        {
            Dialog.Show("请设置至少 1 秒的倒计时。", title:"提示");
            return;
        }

        Hide();
        var countDownWindow = new CountDownWindow(time);
        countDownWindow.Owner = this;
        countDownWindow.ShowDialog();
        Show();
    }

    /// <summary>
    /// 在轮盘区域任意位置按下左键：按指针 X 坐标选中最近的轮盘并开始拖拽。
    /// 整个轮盘组（含分隔符、标签、内边距）都是滑动热区。
    /// </summary>
    private void WheelArea_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var wheel = HitWheel(e.GetPosition(WheelArea));
        _activeWheel = wheel;
        WheelArea.CaptureMouse();
        wheel.BeginDrag(e.GetPosition(wheel).Y);
    }

    /// <summary>拖拽过程中把指针位置转发给当前轮盘。</summary>
    private void WheelArea_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        var wheel = _activeWheel;
        if (wheel == null) return;

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            EndActiveDrag();
            return;
        }

        wheel.DragTo(e.GetPosition(wheel).Y);
    }

    /// <summary>松开左键：结束拖拽并吸附。</summary>
    private void WheelArea_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndActiveDrag();
    }

    /// <summary>鼠标捕获丢失（如窗口失焦）时安全结束拖拽。</summary>
    private void WheelArea_LostMouseCapture(object sender, MouseEventArgs e)
    {
        EndActiveDrag();
    }

    /// <summary>
    /// 轮盘区域内的滚轮事件：若指针不在某个轮盘上（如分隔符、标签上），
    /// 按 X 坐标转发给最近的轮盘，让整个区域滚轮都好用。
    /// </summary>
    private void WheelArea_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;

        var wheel = HitWheel(e.GetPosition(WheelArea));
        wheel.ScrollBy(e.Delta > 0 ? 1 : -1);
        e.Handled = true;
    }

    /// <summary>结束当前拖拽：吸附值并释放鼠标捕获。</summary>
    private void EndActiveDrag()
    {
        var wheel = _activeWheel;
        _activeWheel = null;

        wheel?.EndDrag();
        if (WheelArea.IsMouseCaptured)
            WheelArea.ReleaseMouseCapture();
    }

    /// <summary>按指针 X 坐标找到最近的轮盘（分隔符/标签上的滑动也能落到相邻轮盘）。</summary>
    private NumberWheel HitWheel(Point posInArea)
    {
        NumberWheel best = _wheels[0];
        double bestDist = double.MaxValue;

        foreach (var wheel in _wheels)
        {
            double centerX = wheel.TranslatePoint(
                new Point(wheel.ActualWidth / 2.0, 0), WheelArea).X;
            double dist = Math.Abs(posInArea.X - centerX);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = wheel;
            }
        }

        return best;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {

    }
}
