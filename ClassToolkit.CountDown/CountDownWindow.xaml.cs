using ClassToolkit.Core.Controls;
using System;
using System.Windows;
using System.Windows.Threading;

namespace ClassToolkit.CountDown;

/// <summary>
/// 全屏倒计时展示窗口。最大化显示大号倒计时数字，
/// 支持暂停/继续和重新设置。
/// </summary>
public partial class CountDownWindow : CustomWindow
{
    private readonly TimeSpan _initialTime;      // 初始倒计时时长
    private TimeSpan _remainingTime;             // 剩余时间
    private bool _isRunning;                     // 是否正在计时
    private readonly DispatcherTimer _timer;     // 每秒 tick

    /// <summary>
    /// 初始化倒计时窗口并开始计时。
    /// </summary>
    /// <param name="time">倒计时总时长</param>
    public CountDownWindow(TimeSpan time)
    {
        InitializeComponent();
        _initialTime = time;
        _remainingTime = time;
        UpdateDisplay();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
        _timer.Start();
        _isRunning = true;
    }

    /// <summary>
    /// 每秒 tick：递减剩余时间并刷新显示，归零时自动停止。
    /// </summary>
    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_remainingTime.TotalSeconds <= 0)
        {
            _timer.Stop();
            _isRunning = false;
            TimeDisplay.Text = "00:00:00";
            PauseButton.IsEnabled = false;
            return;
        }

        _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
        UpdateDisplay();
    }

    /// <summary>
    /// 刷新大号时间显示文字。
    /// </summary>
    private void UpdateDisplay()
    {
        TimeDisplay.Text = _remainingTime.ToString(@"hh\:mm\:ss");
    }

    /// <summary>
    /// 暂停/继续按钮：切换计时状态并更新按钮文字。
    /// </summary>
    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            _timer.Stop();
            _isRunning = false;
            PauseButton.Content = "继续";
        }
        else
        {
            _timer.Start();
            _isRunning = true;
            PauseButton.Content = "暂停";
        }
    }

    /// <summary>
    /// 重新设置：停止计时并关闭窗口，回到调节窗口。
    /// </summary>
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        Close();
    }

    /// <summary>
    /// 关闭按钮：停止计时并关闭窗口。
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        Close();
    }

    /// <summary>
    /// 窗口关闭时确保计时器停止，防止后台泄漏。
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        base.OnClosed(e);
    }
}
