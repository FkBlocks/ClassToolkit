using ClassToolkit.Core.Services;
using System.Windows;

namespace ClassToolkit.Core.Controls;

/// <summary>
/// 全局对话框门面。按需创建 DialogWindow，自动以主窗口为 Owner 模态显示。
/// </summary>
public static class Dialog
{
    /// <summary>信息提示（仅“确定”按钮）。</summary>
    public static void Show(string message, string title = "提示") =>
        ShowCore(message, title, showCancel: false);

    /// <summary>确认询问，返回用户是否点击“确定”（点 ✕ 关闭视为取消）。</summary>
    public static bool Confirm(string message, string title = "确认") =>
        ShowCore(message, title, showCancel: true) == true;

    private static bool? ShowCore(string message, string title, bool showCancel)
    {
        // 兜底：宿主 App 若尚未合并主题资源字典（如防多开对话框在主窗口
        // 创建前弹出），DynamicResource 会全部解析失败导致黑屏，
        // 这里确保色板键一定存在。
        if (Application.Current?.TryFindResource("ContentBackground") == null)
            ThemeService.Apply("浅色");

        var dlg = new DialogWindow
        {
            Message = message,
            Title = title,
            ShowCancel = showCancel,
        };

        // Owner 只能指向“已经显示过”的窗口。
        // 在主窗口构造函数期间调用（此时窗口尚未显示）时跳过 Owner，对话框居中屏幕显示。
        var owner = Application.Current?.MainWindow;
        if (owner is { IsVisible: true })
            dlg.Owner = owner;

        return dlg.ShowDialog();
    }
}
