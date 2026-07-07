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

    private void OnDrag(object sender, MouseButtonEventArgs e)
    {
        var win = Window.GetWindow(this);
        if (win == null) return;

        if (e.ClickCount == 2)
            win.WindowState = win.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        else
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
