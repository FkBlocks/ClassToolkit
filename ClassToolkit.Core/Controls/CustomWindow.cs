using System.Windows;
using System.Windows.Input;

namespace ClassToolkit.Core.Controls;

/// <summary>
/// 带自定义标题栏的窗口基类。
/// 内置 WindowChrome 设置和标题栏按钮事件（拖拽/最小化/最大化/关闭）。
/// 子类 XAML 中在标题栏 Border 上绑定 MouseLeftButtonDown="TitleBar_Drag" 即可获得拖拽能力。
/// </summary>
public class CustomWindow : Window
{
    public CustomWindow()
    {
        // 设置自定义标题栏：无玻璃边框、可拖拽调整大小、隐藏默认标题按钮
        System.Windows.Shell.WindowChrome.SetWindowChrome(this, new System.Windows.Shell.WindowChrome
        {
            GlassFrameThickness = new Thickness(0),
            ResizeBorderThickness = new Thickness(4),
            CaptionHeight = 0,
            UseAeroCaptionButtons = false,
        });
    }

    /// <summary>标题栏拖拽 — XAML 绑定 MouseLeftButtonDown="TitleBar_Drag"</summary>
    protected void TitleBar_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            ToggleMaximize();
        else
            DragMove();
    }

    /// <summary>最小化 — XAML 绑定 Click="Minimize_Click"</summary>
    protected void Minimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    /// <summary>最大化/还原切换 — XAML 绑定 Click="Maximize_Click"</summary>
    protected void Maximize_Click(object sender, RoutedEventArgs e) =>
        ToggleMaximize();

    /// <summary>关闭 — XAML 绑定 Click="Close_Click"</summary>
    protected void Close_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>最大化 ↔ 普通 切换</summary>
    private void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
}
