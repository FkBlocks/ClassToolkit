using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Threading;
using ClassToolkit.Core.Services;


namespace ClassToolkit;

/// <summary>
/// 悬浮球主窗口 —— 一个始终置顶的圆形浮动按钮，可拖拽移动、点击弹出菜单。
/// </summary>
public partial class MainWindow
{
    
    /// <summary>设置窗口位置和层级（用于置顶）</summary>
    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    /// <summary>
    /// 获取鼠标光标的屏幕坐标（物理像素）。
    /// GetCursorPos 直接从系统获取，彻底绕过 WPF 坐标栈。
    /// </summary>
    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    /// <summary>Win32 POINT 结构体，int 类型（物理像素）</summary>
    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X; public int Y; }
    
    // ReSharper disable InconsistentNaming
    /// <summary>HWND_TOPMOST: 置顶窗口（在所有非置顶窗口上方），传入 SetWindowPos 的 hWndInsertAfter 参数</summary>
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    /// <summary>SWP_NO_MOVE: 保持当前位置，忽略 X/Y 参数</summary>
    private const uint SWP_NO_MOVE = 0x0002;
    /// <summary>SWP_NO_SIZE: 保持当前大小，忽略 cx/cy 参数</summary>
    private const uint SWP_NO_SIZE = 0x0001;
    /// <summary>SWP_SHOW_WINDOW: 显示窗口</summary>
    private const uint SWP_SHOW_WINDOW = 0x0040;
    
    
    /// <summary>是否正在拖拽中（鼠标按下且移动超过阈值后置为 true）</summary>
    private bool _isDragging;
    /// <summary>鼠标按下时，光标在悬浮球窗口内的相对位置，用于判断是否移动超过了拖拽阈值</summary>
    private Point _startPoint;
    /// <summary>拖拽开始时，鼠标光标的屏幕绝对坐标（WPF 设备无关像素，通过 GetCursorPos + DPI 转换得到）</summary>
    private Point _dragStartMouseScreenPos;
    /// <summary>拖拽开始时，悬浮球窗口的屏幕位置 (Left, Top)</summary>
    private Point _dragStartWindowPos;
    /// <summary>鼠标需要移动的最小像素数，超过此值才判定为拖拽（防止误触）</summary>
    private readonly double _dragThreshold = 5;
    /// <summary>右键/WPF Popup 菜单，显示在悬浮球旁边</summary>
    private Popup _menuPopup = null!;
    /// <summary>菜单是否正在显示</summary>
    private bool _isMenuOpen;

    
    public MainWindow()
    {
        LogService.Init("ClassToolkit");  // 设置日志名称
        InitializeComponent();      // 加载 XAML 布局
        InitializeMenuPopup();      // 用代码构建 Popup 菜单
        this.Loaded += (_, _) => MakeSuperTopmost();  // 窗口加载完成后立即置顶
        LogService.Info("启动成功");
    }

    /// <summary>
    /// 用纯代码构建弹出菜单（Popup 控件）。
    /// 不用 XAML 是因为 Popup 需要独立于窗口的定位逻辑，
    /// 用代码更灵活地控制 Placement 和 Offset。
    /// </summary>
    /// <summary>每个菜单按钮的固定高度（含 Margin），用于动态计算 Popup 尺寸</summary>
    private const double MenuItemHeight = 40;

    private void InitializeMenuPopup()
    {
        _menuPopup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.Absolute,
            StaysOpen = false,
        };

        BuildMenuFromJson();
    }

    /// <summary>
    /// 读取 data/tools.json 并构建菜单内容。仅启动时调用一次。
    /// </summary>
    private void BuildMenuFromJson()
        // TODO 加日志
    {
        var tools = LoadTools();

        var stackPanel = new StackPanel { Margin = new Thickness(10) };

        foreach (var tool in tools)
            stackPanel.Children.Add(CreateMenuItem(tool.Name, tool.Path));

        // "退出"固定在底部，toolPath 为 null
        stackPanel.Children.Add(CreateMenuItem("退出", null));

        var menuPanel = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(22, 22, 22)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8),
            Child = stackPanel
        };

        _menuPopup.Child = menuPanel;

        // 动态计算 Popup 尺寸：工具数 + 1（退出按钮）
        int itemCount = tools.Count + 1;
        _menuPopup.Width = 130;
        _menuPopup.Height = itemCount * MenuItemHeight + 36; // 36 = Border(8×2) + StackPanel(10×2)
    }

    /// <summary>
    /// 从 data/tools.json 加载工具列表。
    /// </summary>
    private List<(string Name, string Path)> LoadTools()
    {
        string jsonPath = ClassToolkit.Core.Utilities.DataPathHelper.GetDataPath("tools.json");

        if (!System.IO.File.Exists(jsonPath))
            return new List<(string, string)>();

        try
        {
            string json = System.IO.File.ReadAllText(jsonPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return dict?.Select(kv => (kv.Key, kv.Value)).ToList()
                   ?? new List<(string, string)>();
        }
        catch
        {
            return new List<(string, string)>();
        }
    }

    /// <summary>
    /// 创建一个菜单按钮，统一样式。
    /// </summary>
    /// <param name="text">按钮文字</param>
    /// <returns>配置好的 Button 控件</returns>
    private Button CreateMenuItem(string text, string? toolPath)
    {
        Button btn = new Button
        {
            Content = text,
            Height = 30,
            Margin = new Thickness(0, 5, 0, 5),
            Background = Brushes.Transparent,
            Foreground = Brushes.White,
            BorderBrush = Brushes.Transparent,
            Cursor = Cursors.Hand,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 14,
            FontFamily = new FontFamily("Microsoft YaHei")
        };

        if (toolPath != null)
            btn.Click += (_, _) => OnToolClick(text, toolPath);
        else
            btn.Click += (_, _) => AskWhenExit();

        return btn;
    }
    
    /// <summary>定时刷新置顶的计时器，每 60s 触发一次</summary>
    private DispatcherTimer timer = new DispatcherTimer();

    /// <summary>
    /// 将悬浮球设为系统级"超级置顶"。
    ///
    /// HWND_TOPMOST 级别的窗口会始终渲染在所有非置顶窗口上方，
    /// 包括其他应用的窗口。定时刷新是为了防止其他程序抢夺置顶层导致悬浮球被遮挡。
    /// </summary>
    private void MakeSuperTopmost()
    {
        // 获取 WPF 窗口底层的 Win32 句柄 (HWND)
        var hwnd = new WindowInteropHelper(this).Handle;

        // 立即置顶一次
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
            SWP_NO_MOVE | SWP_NO_SIZE | SWP_SHOW_WINDOW);

        // 每 60000ms(60s) 重新置顶，防止被其他置顶窗口覆盖
        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        timer.Tick += (_, _) => SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
            SWP_NO_MOVE | SWP_NO_SIZE);
        timer.Start();
    }
    
    /// <summary>
    /// 将方形窗口裁剪成圆形。
    /// WPF 的 Clip 属性可以接受任意 Geometry 来定义窗口的可视区域，
    /// 这里用一个圆形 EllipseGeometry 来裁剪，实现"圆形悬浮球"效果。
    /// </summary>
    private void UpdateClipToCircle()
    {
        double radius = Math.Min(ActualWidth, ActualHeight) / 2;
        if (radius <= 0) return;

        Point center = new Point(ActualWidth / 2, ActualHeight / 2);
        EllipseGeometry circle = new EllipseGeometry(center, radius, radius);
        Clip = circle;
    }
    
    /// <summary>
    /// 窗口首次加载时：读取配置 -> 裁剪圆形 -> 定位到屏幕右下角 -> 边界保护。
    /// WindowStartupLocation="Manual" 表示由代码手动指定位置。
    /// </summary>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 加载所需配置
        ConfigService configService = new ConfigService();
        var config = configService.Load(new Dictionary<string, object?> { ["BallSize"] = 60 });

        UpdateClipToCircle();

        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;

        // 默认定位：右边距 10px，下边距 330px（避开任务栏区域）
        this.Left = screenWidth - this.ActualWidth - 10;
        this.Top = screenHeight - this.ActualHeight - 330;

        // 防止越出边界（小屏幕或分辨率变更时）
        if (this.Left < 0) this.Left = 0;
        if (this.Top < 0) this.Top = 0;
    }

    // ============================================================
    //  拖拽交互（三个事件的协作）
    //
    //  整体流程：
    //  1. MouseLeftButtonDown  -> ClickBall()        记录起点，开始鼠标捕获
    //  2. MouseMove            -> OnDragFloatBall()  超过阈值后，实时移动窗口（边界钳制）
    //  3. MouseLeftButtonUp    -> LeftClickUp()      释放捕获，判断是"点击"还是"拖拽结束"
    //
    //  关键设计：手动拖拽替代 DragMove()
    //  - WPF 内置的 DragMove() 无法限制窗口不超出屏幕
    //  - 手动计算：拖拽起点 (窗口位置 + 鼠标屏幕位置) -> MouseMove 时计算偏移量 -> 钳制 -> 写回 Left/Top
    //  - 坐标获取：用 Win32 GetCursorPos 而不是 PointToScreen
    //    因为透明窗口 (AllowsTransparency=True) 的底层是分层窗口，PointToScreen 会漂移
    //  - 鼠标捕获：CaptureMouse() 确保鼠标移出窗口边界时仍能收到 MouseMove 事件
    // ============================================================

    /// <summary>
    /// 鼠标按下事件 —— 记录拖拽的起点信息，开始鼠标捕获。
    ///
    /// 为什么需要三个坐标：
    /// - _startPoint: 鼠标在窗口内的相对位置 -> 判断是否超过拖拽阈值（区分"点击"和"拖拽"）
    /// - _dragStartMouseScreenPos: 鼠标的屏幕绝对坐标 -> 后续 MouseMove 时计算鼠标移动的总偏移量
    /// - _dragStartWindowPos: 窗口当前的屏幕坐标 -> 偏移量 + 窗口起点 = 新窗口位置
    /// </summary>
    private void ClickBall(object sender, MouseButtonEventArgs e)
    {
        // 鼠标在悬浮球内的相对位置
        _startPoint = e.GetPosition(this);

        // 用 Win32 GetCursorPos 获取鼠标屏幕坐标（不受透明窗口坐标漂移影响）
        _dragStartMouseScreenPos = GetCursorScreenPos();

        // 窗口当前屏幕坐标
        _dragStartWindowPos = new Point(this.Left, this.Top);

        // 重置拖拽状态
        _isDragging = false;

        // 捕获鼠标：即使鼠标移出窗口区域，MouseMove 和 MouseUp 事件仍会发送到这个窗口
        CaptureMouse();
    }

    /// <summary>
    /// 鼠标移动事件 —— 判断是否进入拖拽状态，并在拖拽中实时更新窗口位置。
    ///
    /// 两个阶段：
    /// 1. 未超过阈值：检查鼠标移动距离是否超过 _dragThreshold（5px），区分拖拽和误触
    /// 2. 已进入拖拽：根据鼠标屏幕坐标的偏移量计算窗口新位置，并钳制在屏幕边界内
    ///
    /// 坐标计算公式：
    ///   新位置 = 拖拽时窗口位置 + (当前鼠标屏幕位置 - 拖拽时鼠标屏幕位置)
    ///   即窗口跟随鼠标位移相同的偏移量
    /// </summary>
    private void OnDragFloatBall(object sender, MouseEventArgs e)
    {
        // 如果鼠标左键没有按下（松开了），忽略
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        // --- 阶段 1: 检查是否达到拖拽阈值 ---
        if (!_isDragging)
        {
            Point currentPoint = e.GetPosition(this);
            Vector delta = currentPoint - _startPoint;
            if (Math.Abs(delta.X) > _dragThreshold || Math.Abs(delta.Y) > _dragThreshold)
            {
                _isDragging = true;  // 超过阈值，进入拖拽模式
            }
            else
            {
                return;  // 还没到阈值，不处理
            }
        }

        // --- 阶段 2: 拖拽中，实时计算新位置 ---

        // 获取鼠标当前的屏幕坐标（Win32 API，不受透明窗口干扰）
        Point currentScreenPos = GetCursorScreenPos();

        // 计算窗口新位置：窗口起点 + 鼠标偏移量
        double newLeft = _dragStartWindowPos.X + (currentScreenPos.X - _dragStartMouseScreenPos.X);
        double newTop = _dragStartWindowPos.Y + (currentScreenPos.Y - _dragStartMouseScreenPos.Y);

        // 屏幕边界（WPF 设备无关像素）
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;

        // 四条边界钳制：窗口永远不会超出屏幕
        // 左边界：窗口不能小于 0
        if (newLeft < 0) newLeft = 0;
        // 上边界：窗口不能小于 0
        if (newTop < 0) newTop = 0;
        // 右边界：窗口右边不能超出屏幕右边缘
        if (newLeft + this.ActualWidth > screenWidth)
            newLeft = screenWidth - this.ActualWidth;
        // 下边界：窗口下边不能超出屏幕下边缘
        if (newTop + this.ActualHeight > screenHeight)
            newTop = screenHeight - this.ActualHeight;

        // 应用新位置
        this.Left = newLeft;
        this.Top = newTop;
    }

    /// <summary>
    /// 鼠标松开事件 —— 释放捕获，判断是"点击"还是"拖拽结束"。
    ///
    /// - 如果是点击（_isDragging == false）：弹出功能菜单
    /// - 如果是拖拽结束（_isDragging == true）：什么都不做（窗口已在拖拽过程中跟随到位）
    /// </summary>
    private void LeftClickUp(object sender, MouseButtonEventArgs e)
    {
        // 释放鼠标捕获，恢复正常事件路由
        ReleaseMouseCapture();

        if (!_isDragging)
        {
            // 没拖拽 = 一次完整的点击 -> 显示菜单
            ShowMenu();
        }
        // 拖拽结束不需要额外处理，窗口位置已在 MouseMove 中实时更新

        _isDragging = false;
    }

    /// <summary>
    /// 获取鼠标光标的屏幕坐标，返回 WPF 设备无关像素 (DIP)。
    ///
    /// 调用链：
    ///   Win32 GetCursorPos (物理像素) -> TransformFromDevice (DIP 转换) -> 返回 Point
    /// </summary>
    /// <returns>鼠标在屏幕上的位置（WPF 设备无关像素坐标）</returns>
    private Point GetCursorScreenPos()
    {
        // 从 Win32 获取物理像素坐标
        GetCursorPos(out POINT pt);

        // 将物理像素转换为 WPF 的设备无关像素 (DIP)
        // 例如在 150% 缩放屏幕上，物理像素 150 -> DIP 100
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            Matrix transform = source.CompositionTarget.TransformFromDevice;
            return transform.Transform(new Point(pt.X, pt.Y));
        }

        // 降级处理：如果 PresentationSource 不可用（极少见），直接返回物理像素
        return new Point(pt.X, pt.Y);
    }
    
    /// <summary>
    /// 显示/切换弹出菜单。
    ///
    /// 定位策略：菜单显示在悬浮球左侧，垂直居中对齐，留 5px 间距。
    /// 如果左侧空间不足（靠近屏幕左边缘），则贴在屏幕左侧 5px 处。
    /// 如果菜单超出屏幕上下边界，也进行钳制。
    /// </summary>
    private void ShowMenu()
    {
        // 已打开则关闭（点击切换行为）
        if (_isMenuOpen)
        {
            CloseMenu();
            return;
        }

        double ballLeft = this.Left;
        double ballTop = this.Top;

        // 菜单默认放在悬浮球左侧，垂直居中
        double menuLeft = ballLeft - _menuPopup.Width - 5;
        double menuTop = ballTop + (this.ActualHeight - _menuPopup.Height) / 2;

        // 边界保护：不超出屏幕
        if (menuLeft < 0) menuLeft = 5;
        if (menuTop < 0) menuTop = 5;
        if (menuTop + _menuPopup.Height > SystemParameters.PrimaryScreenHeight)
            menuTop = SystemParameters.PrimaryScreenHeight - _menuPopup.Height - 5;

        // Popup 用 HorizontalOffset/VerticalOffset 设置屏幕绝对坐标
        _menuPopup.HorizontalOffset = menuLeft;
        _menuPopup.VerticalOffset = menuTop;

        _menuPopup.IsOpen = true;
        _isMenuOpen = true;
    }

    /// <summary>关闭弹出菜单</summary>
    private void CloseMenu()
    {
        _menuPopup.IsOpen = false;
        _isMenuOpen = false;
    }

    /// <summary>
    /// 工具菜单项点击处理 —— 关闭菜单后启动 tools.json 中指定的程序。
    /// 使用 Process.Start 打开，由 Windows 决定关联程序。
    /// </summary>
    private void OnToolClick(string toolName, string toolPath)
    {
        CloseMenu();

        string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, toolPath);

        if (!System.IO.File.Exists(fullPath))
        {
            LogService.Warn($"启动程序未找到: {fullPath}");
            MessageBox.Show($"未找到工具: {toolName}, 请检查路径是否正确");
            return;
        }

        try
        {
            Process.Start(fullPath);
        }
        catch (Exception ex)
        {
            LogService.Error($"程序启动失败: 位于'{fullPath}'启动时发生 {ex.Message} 错误");
            MessageBox.Show($"启动失败: {ex.Message}");
        }
    }

    private void AskWhenExit()
    {
        bool? confirmed = ShowConfirmDialog("确认退出？", "是否退出");
        if (confirmed == true)
        {
            timer.Stop();
            LogService.Info("程序退出");
            Application.Current.Shutdown();
        }
    }

    /// <summary>
    /// 显示一个定位在悬浮球旁边的确认对话框。
    ///
    /// 为什么不用 MessageBox.Show(this, ...)：
    ///   本窗口启用 AllowsTransparency=True（分层窗口），WPF 的 MessageBox
    ///   内部依赖 CenterOwner 定位，对分层窗口的坐标计算会失败，退回到屏幕中央。
    ///   因此手动创建一个 Window 并参照 ShowMenu() 的定位策略，用悬浮球的屏幕坐标
    ///   计算对话框位置，绕过 WPF 的自动居中机制。
    /// </summary>
    /// <param name="message">提示文字</param>
    /// <param name="title">对话框标题</param>
    /// <returns>true=确认, false=取消, null=关闭（理论上不会出现）</returns>
    private bool? ShowConfirmDialog(string message, string title)
    {
        // 构建确认对话框窗口，样式与 MessageBox 一致
        var dialog = new Window
        {
            Title = title,
            Width = 280,
            SizeToContent = SizeToContent.Height,  // 高度根据内容自适应
            WindowStyle = WindowStyle.ToolWindow,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,  // 关键：手动定位
            Topmost = true,
            Owner = this,                                         // 保持模态关系
        };

        // ── 对话框内容 ──
        var panel = new StackPanel { Margin = new Thickness(20, 15, 20, 15) };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            Margin = new Thickness(0, 0, 0, 15),
            FontSize = 14,
            FontFamily = new FontFamily("Microsoft YaHei"),
            TextWrapping = TextWrapping.Wrap
        });

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        bool? dialogResult = null;

        var yesBtn = new Button
        {
            Content = "是",
            Width = 70,
            Height = 30,
            Margin = new Thickness(0, 0, 10, 0)
        };
        yesBtn.Click += (_, _) => { dialogResult = true; dialog.Close(); };

        var noBtn = new Button
        {
            Content = "否",
            Width = 70,
            Height = 30
        };
        noBtn.Click += (_, _) => { dialogResult = false; dialog.Close(); };

        btnPanel.Children.Add(yesBtn);
        btnPanel.Children.Add(noBtn);
        panel.Children.Add(btnPanel);
        dialog.Content = panel;

        // ── 定位：参照 ShowMenu() 的策略 —— 对话框显示在悬浮球右侧，垂直居中 ──
        dialog.Loaded += (_, _) =>
        {
            // 默认：悬浮球右侧，留 5px 间距，垂直居中
            double dialogLeft = this.Left + this.ActualWidth + 5;
            double dialogTop = this.Top + (this.ActualHeight - dialog.ActualHeight) / 2;

            // 边界保护（与 ShowMenu 一致）：不超出屏幕四边
            if (dialogLeft + dialog.Width > SystemParameters.PrimaryScreenWidth)
                dialogLeft = this.Left - dialog.Width - 5;  // 右侧不够 -> 改放左侧
            if (dialogLeft < 0) dialogLeft = 5;
            if (dialogTop < 0) dialogTop = 5;
            if (dialogTop + dialog.ActualHeight > SystemParameters.PrimaryScreenHeight)
                dialogTop = SystemParameters.PrimaryScreenHeight - dialog.ActualHeight - 5;

            dialog.Left = dialogLeft;
            dialog.Top = dialogTop;
        };

        // 模态显示，阻塞直到用户点击按钮
        dialog.ShowDialog();
        return dialogResult;
    }

}