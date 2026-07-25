using ClassToolkit.Core.Controls;
using System.Windows;

namespace ClassToolkit.CountDown;

/// <summary>
/// 倒计时调节窗口。启动时显示，提供时/分/秒三轮盘和开始/重置按钮。
/// 点击"开始倒计时"后隐藏自身并打开全屏倒计时展示窗口。
/// </summary>
public partial class MainWindow : CustomWindow
{
    /// <summary>
    /// 初始化调节窗口。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
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
            MessageBox.Show("请设置至少 1 秒的倒计时。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Hide();
        var countDownWindow = new CountDownWindow(time);
        countDownWindow.Owner = this;
        countDownWindow.ShowDialog();
        Show();
    }
}
