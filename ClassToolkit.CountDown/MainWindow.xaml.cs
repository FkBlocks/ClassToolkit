using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ClassToolkit.CountDown
{
    public partial class MainWindow
    {
        private DispatcherTimer _timer;
        private TimeSpan _remainingTime;
        private TimeSpan _initialTime;
        private bool _isRunning;

        public MainWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;

            // 初始化显示
            UpdateDisplayFromInput();
        }

        private void UpdateDisplayFromInput()
        {
            try
            {
                int hours = int.TryParse(HoursBox.Text, out int h) ? h : 0;
                int minutes = int.TryParse(MinutesBox.Text, out int m) ? m : 0;
                int seconds = int.TryParse(SecondsBox.Text, out int s) ? s : 0;

                _initialTime = new TimeSpan(hours, minutes, seconds);
                _remainingTime = _initialTime;
                TimeDisplay.Text = _initialTime.ToString(@"hh\:mm\:ss");
            }
            catch
            {
                TimeDisplay.Text = "00:00:00";
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_remainingTime.TotalSeconds <= 0)
            {
                _timer.Stop();
                _isRunning = false;
                UpdateButtons(false);
                MessageBox.Show("倒计时结束！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
            TimeDisplay.Text = _remainingTime.ToString(@"hh\:mm\:ss");
        }

        private void UpdateButtons(bool running)
        {
            StartButton.IsEnabled = !running;
            PauseButton.IsEnabled = running;
            ResetButton.IsEnabled = true;
            // 设置输入框在运行时不可编辑（可选）
            HoursBox.IsEnabled = !running;
            MinutesBox.IsEnabled = !running;
            SecondsBox.IsEnabled = !running;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
            {
                // 如果剩余时间为0，则从初始值重新开始
                if (_remainingTime.TotalSeconds <= 0)
                {
                    _remainingTime = _initialTime;
                    TimeDisplay.Text = _remainingTime.ToString(@"hh\:mm\:ss");
                }
                _timer.Start();
                _isRunning = true;
                UpdateButtons(true);
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                _timer.Stop();
                _isRunning = false;
                UpdateButtons(false);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            UpdateDisplayFromInput(); // 重置为输入框的值
            UpdateButtons(false);
        }
    }
}