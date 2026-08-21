using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClassToolkit.Core.Controls;

/// <summary>
/// 通用标题栏控件。拖入任意 CustomWindow 子类的 Window 中即可。
/// </summary>
public partial class TitleBar : UserControl
{
    public TitleBar()
    {
        InitializeComponent();
    }

    /// <summary>标题栏文字</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TitleBar),
            new PropertyMetadata(string.Empty, (d, _) =>
            {
                if (d is TitleBar tb)
                    tb.TitleText.Text = tb.Title;
            }));

    /// <summary>双击标题栏是否最大化/还原。对话框等禁止最大化的窗口可设为 false。</summary>
    public bool DoubleClickMaximize
    {
        get => (bool)GetValue(DoubleClickMaximizeProperty);
        set => SetValue(DoubleClickMaximizeProperty, value);
    }

    public static readonly DependencyProperty DoubleClickMaximizeProperty =
        DependencyProperty.Register(nameof(DoubleClickMaximize), typeof(bool), typeof(TitleBar),
            new PropertyMetadata(true));

    private void OnDrag(object sender, MouseButtonEventArgs e)
    {
        var win = Window.GetWindow(this);
        if (win == null) return;

        if (e.ClickCount == 2)
        {
            // 双击只做最大化切换（可配置关闭），不参与拖拽
            if (DoubleClickMaximize)
                win.WindowState = win.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            return;
        }

        win.DragMove();
    }

    private void OnMinimize(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)!.WindowState = WindowState.Minimized;
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)!.Close();
    }
}
