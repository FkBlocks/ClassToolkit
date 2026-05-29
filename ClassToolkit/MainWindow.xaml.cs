using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Threading;


namespace ClassToolkit;

/// <summary>
/// 悬浮球主窗口
/// </summary>
public partial class MainWindow : Window
{
    // 导入user32.dll，用于超级置顶
    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;
    
    private bool isDragging = false;
    private Point startPoint;
    private readonly double dragThreshold = 5;
    
    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += (s, e) => MakeSuperTopmost();
    }

    // 设置计时器，保证永远超级置顶，定时刷新一次
    private DispatcherTimer timer = new DispatcherTimer();
    // 设置超级置顶
    private void MakeSuperTopmost()
    {
        // 置顶
        var hwnd = new WindowInteropHelper(this).Handle;
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        
        // 定时刷新，保持置顶
        timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromMilliseconds(500);
        timer.Tick += (s, e) => SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,  SWP_NOMOVE | SWP_NOSIZE);
        timer.Start();
    }
    
    private void Quit(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
    
    // 绘制圆形窗口
    private void UpdateClipToCircle()
    {
        // 圆形半径
        double radius = Math.Min(ActualWidth, ActualHeight) / 2;
        if (radius <= 0) return;
        
        Point center = new Point(ActualWidth / 2, ActualHeight / 2);
        EllipseGeometry circle = new EllipseGeometry(center, radius, radius);
        Clip = circle;
    }
    
    // 显示
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateClipToCircle();
        // 获取屏幕宽高
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;
        
        // 计算偏移
        this.Left = screenWidth - this.ActualWidth - 10;    // 距右边80px
        this.Top = screenHeight - this.ActualHeight - 330;  // 距底部400px
        
        // 防止越出边界
        if (this.Left < 0) this.Left = 0;
        if (this.Top < 0) this.Top = 0;
    }

    private void OnDragFloatBall(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !isDragging)
        {
            Point currentPoint = e.GetPosition(this);
            Vector delta = currentPoint - startPoint;
            if (Math.Abs(delta.X) > dragThreshold || Math.Abs(delta.Y) > dragThreshold)
            {
                isDragging = true;
                this.DragMove();
            }
        }
    }

    private void ClickBall(object sender, MouseButtonEventArgs e)
    {
        startPoint = e.GetPosition(this);
        isDragging = false;
    }


    private void LeftClickUp(object sender, MouseButtonEventArgs e)
    {
        if (!isDragging)
        {
            ShowMenu();
        }
        isDragging = false;
    }

    private void ShowMenu()
    {
        MessageBox.Show("Show Menu!");
    }
}