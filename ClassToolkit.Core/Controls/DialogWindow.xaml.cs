using System;
using System.Windows;
using System.Windows.Shell;

namespace ClassToolkit.Core.Controls
{
    /// <summary>
    /// 对话框窗口。窗口尺寸由代码按内容手动计算（不用 SizeToContent，
    /// 它与 WindowChrome 组合存在已知 bug，会在右下角留下黑边）：
    /// 消息超宽自动换行（TextBlock.MaxWidth），超高出现滚动条（ScrollViewer.MaxHeight 封顶）。
    /// 内容通过属性在 ShowDialog 之前传入，由 Dialog 门面统一创建。
    /// </summary>
    public partial class DialogWindow : CustomWindow
    {
        public DialogWindow()
        {
            InitializeComponent();

            // 对话框不可调整大小，不需要 WindowChrome 的隐形拖拽缩边（4px），
            // 避免残留边框区域与窗口尺寸计算冲突（黑边）。
            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(0),
                CaptionHeight = 0,
                UseAeroCaptionButtons = false,
            });
        }

        /// <summary>消息内容（支持 \n 换行）</summary>
        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(DialogWindow),
                new PropertyMetadata(string.Empty, OnMessageChanged));

        private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DialogWindow w || w.MessageText == null) return;

            w.MessageText.Text = (string)e.NewValue;
            if (w.IsLoaded) w.ApplyAutoSize(); // 显示后修改内容时同步窗口尺寸
        }

        /// <summary>是否显示“取消”按钮（需在 ShowDialog 之前设置）</summary>
        public bool ShowCancel { get; set; }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            BtnCancel.Visibility = ShowCancel ? Visibility.Visible : Visibility.Collapsed;
            TitleBar.Title = Title;
            ApplyAutoSize();
        }

        /// <summary>
        /// 手动测量内容并设置窗口尺寸与位置，等效 SizeToContent=WidthAndHeight + CenterOwner，
        /// 但绕开两者的已知 bug（右下黑边、Owner 不可用时不居中）。
        /// </summary>
        private void ApplyAutoSize()
        {
            // 无限空间下测量：TextBlock 自身 MaxWidth、ScrollViewer 自身 MaxHeight 仍然生效
            Root.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            var size = Root.DesiredSize;

            // 内容超高出现滚动条时，滚动条约占 17px 宽度，提前补上避免文字被挤得再换行
            if (MessageText.DesiredSize.Height > MessageScroll.MaxHeight)
                size.Width += SystemParameters.VerticalScrollBarWidth;

            Width = Math.Clamp(size.Width, MinWidth, MaxWidth);
            Height = size.Height;

            CenterWindow();
        }

        /// <summary>
        /// 手动居中：优先以已显示的 Owner 为中心（多显示器跟随主窗口），
        /// 否则以主屏工作区为中心（如主窗口尚未显示时）。
        /// </summary>
        private void CenterWindow()
        {
            if (Owner is { IsVisible: true } owner)
            {
                double ownerWidth = owner.ActualWidth > 0 ? owner.ActualWidth : owner.Width;
                double ownerHeight = owner.ActualHeight > 0 ? owner.ActualHeight : owner.Height;
                Left = owner.Left + (ownerWidth - Width) / 2;
                Top = owner.Top + (ownerHeight - Height) / 2;
            }
            else
            {
                var workArea = SystemParameters.WorkArea;
                Left = workArea.Left + (workArea.Width - Width) / 2;
                Top = workArea.Top + (workArea.Height - Height) / 2;
            }
        }

        private void OnOk(object sender, RoutedEventArgs e) => DialogResult = true;

        private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
