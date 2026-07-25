using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ClassToolkit.CountDown.Controls;

/// <summary>
/// 轮盘式数字选择器。垂直排列数字，中心高亮为当前值，
/// 支持鼠标滚轮和拖拽切换，带弹簧物理惯性动画。
/// 用于倒计时的时/分/秒调节。
/// </summary>
public class NumberWheel : FrameworkElement
{
    // ── 依赖属性 ────────────────────────────

    /// <summary>当前值</summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(NumberWheel),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender,
                (d, _) => ((NumberWheel)d).OnValueChanged()));

    /// <summary>最小值</summary>
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(NumberWheel),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>最大值</summary>
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(NumberWheel),
            new FrameworkPropertyMetadata(59, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>格式化字符串（如 "D2" 显示为 "05"）</summary>
    public static readonly DependencyProperty FormatProperty =
        DependencyProperty.Register(nameof(Format), typeof(string), typeof(NumberWheel),
            new FrameworkPropertyMetadata("D2", FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>当前值</summary>
    public int Value { get => (int)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>最小值</summary>
    public int Minimum { get => (int)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    /// <summary>最大值</summary>
    public int Maximum { get => (int)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>数字格式化字符串</summary>
    public string Format { get => (string)GetValue(FormatProperty); set => SetValue(FormatProperty, value); }

    // ── 动画状态 ────────────────────────────

    private double _displayOffset;       // 当前视觉偏移量（相对于 _lastValue）
    private double _targetOffset;        // 目标偏移量（始终为 0 = 居中吸附）
    private double _velocity;            // 惯性速度（单位/秒）
    private int _lastValue;              // 上次吸附的整数值
    private bool _isDragging;            // 是否正在拖拽中
    private double _dragStartY;          // 拖拽起始 Y 坐标
    private double _dragStartOffset;     // 拖拽起始偏移量
    private DateTime _lastFrameTime = DateTime.Now;
    private readonly DispatcherTimer _animTimer;

    // ── 外观常量 ────────────────────────────

    private const double ITEM_HEIGHT = 42;       // 每个数字槽的高度
    private const int VISIBLE_SLOTS = 5;         // 可见槽位数（奇数，中间为选中）
    private const int HALF_SLOTS = VISIBLE_SLOTS / 2;
    private const double CENTER_FONT = 30;       // 中心选中数字字号
    private const double ADJACENT_FONT = 14;     // 紧邻数字字号
    private const double EDGE_FONT = 11;         // 边缘数字字号
    private const double WHEEL_WIDTH = 100;

    // ── 物理常量 ────────────────────────────

    private const double SPRING = 220;           // 弹簧刚度（越大吸附越快）
    private const double DAMPING = 26;           // 阻尼系数（越大停得越快）

    /// <summary>
    /// 初始化轮盘控件，设置尺寸和输入事件。
    /// </summary>
    public NumberWheel()
    {
        Width = WHEEL_WIDTH;
        Height = ITEM_HEIGHT * VISIBLE_SLOTS;
        MinHeight = ITEM_HEIGHT * 3;
        ClipToBounds = true;
        Cursor = Cursors.Hand;
        Focusable = true;

        _lastValue = Value;
        _displayOffset = 0;
        _targetOffset = 0;

        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _animTimer.Tick += OnAnimationTick;

        MouseWheel += OnMouseWheel;
        MouseLeftButtonDown += OnMouseDown;
        MouseLeftButtonUp += OnMouseUp;
        MouseMove += OnMouseMove;
        LostMouseCapture += (_, _) => { _isDragging = false; ReleaseMouseCapture(); };

        Loaded += (_, _) => InvalidateVisual();
    }

    /// <summary>
    /// Value 依赖属性变更回调。将偏移量重基准到新值并触发归中动画。
    /// </summary>
    private void OnValueChanged()
    {
        if (_lastValue != Value)
        {
            _displayOffset += _lastValue - Value;
            _targetOffset = 0;
            _velocity = 0;
            _lastValue = Value;
            if (!_animTimer.IsEnabled)
                _animTimer.Start();
        }
    }

    /// <summary>
    /// 鼠标滚轮：上滚数值增加，下滚数值减小，带方向轻踢。
    /// </summary>
    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        int delta = e.Delta > 0 ? 1 : -1;
        int newVal = Clamp(Value + delta);
        if (newVal != Value)
        {
            _displayOffset += Value - newVal;
            _targetOffset = 0;
            _velocity = delta * 8;
            Value = newVal;
            _lastValue = Value;
            if (!_animTimer.IsEnabled)
                _animTimer.Start();
        }
        e.Handled = true;
    }

    /// <summary>鼠标按下：开始拖拽，暂停动画。</summary>
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragStartY = e.GetPosition(this).Y;
        _dragStartOffset = _displayOffset;
        _velocity = 0;
        CaptureMouse();
        _animTimer.Stop();
    }

    /// <summary>
    /// 鼠标移动：实时更新偏移量。上拖数值增加，下拖数值减小。
    /// </summary>
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        double dy = e.GetPosition(this).Y - _dragStartY;
        _displayOffset = _dragStartOffset - dy / ITEM_HEIGHT;
        _velocity = -dy / ITEM_HEIGHT;
        InvalidateVisual();
    }

    /// <summary>
    /// 鼠标松开：吸附到最近整数，保留部分惯性后启动弹簧动画。
    /// </summary>
    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        ReleaseMouseCapture();

        double totalOffset = _displayOffset;
        int steps = (int)Math.Round(totalOffset);
        int newVal = Clamp(_lastValue + steps);

        _displayOffset = totalOffset - steps;   // 残差，相对于新 _lastValue
        _targetOffset = 0;                      // 目标始终归中
        _velocity *= 0.4;                       // 保留部分惯性
        Value = newVal;
        _lastValue = Value;

        _animTimer.Start();
    }

    /// <summary>
    /// 动画帧（约 60fps）。弹簧 + 阻尼驱动物理模拟，静止时自动停止。
    /// </summary>
    private void OnAnimationTick(object? sender, EventArgs e)
    {
        double dt = (DateTime.Now - _lastFrameTime).TotalSeconds;
        _lastFrameTime = DateTime.Now;
        dt = Math.Min(dt, 0.04);
        if (_isDragging) return;

        // F = k * (target - x) - c * v
        double springForce = (_targetOffset - _displayOffset) * SPRING;
        double dampForce = -_velocity * DAMPING;
        _velocity += (springForce + dampForce) * dt;
        _displayOffset += _velocity * dt;

        // 静止检测 → 吸附停止
        bool still = Math.Abs(_displayOffset - _targetOffset) < 0.0003
                  && Math.Abs(_velocity) < 0.08;
        if (still)
        {
            _displayOffset = _targetOffset;
            _velocity = 0;
            _animTimer.Stop();
        }

        InvalidateVisual();
    }

    /// <summary>
    /// 自绘渲染。以 _lastValue + _displayOffset 为中心，
    /// 上下各延伸 HALF_SLOTS+1 个数字，按距离缩放字号和透明度。
    /// </summary>
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double centerY = ActualHeight / 2.0;
        var typeface = new Typeface("Microsoft YaHei");
        var accentBrush = GetAccentBrush();
        var textBrush = GetTextBrush();
        var dimBrush = GetDimBrush();

        double totalDisplay = _lastValue + _displayOffset;
        int centerVal = (int)Math.Round(totalDisplay);
        int startVal = centerVal - HALF_SLOTS - 1;
        int endVal = centerVal + HALF_SLOTS + 1;

        for (int val = startVal; val <= endVal; val++)
        {
            double y = centerY + (val - totalDisplay) * ITEM_HEIGHT;
            if (y < -ITEM_HEIGHT || y > ActualHeight + ITEM_HEIGHT)
                continue;

            bool inRange = val >= Minimum && val <= Maximum;
            double dist = Math.Abs(val - totalDisplay);
            double opacity, fontSize;

            if (dist < 0.5)
            {
                double t = dist / 0.5;
                fontSize = CENTER_FONT - (CENTER_FONT - ADJACENT_FONT) * t * t;
                opacity = 1.0 - 0.4 * t;
            }
            else if (dist < 2.5)
            {
                double t = (dist - 0.5) / 2.0;
                fontSize = ADJACENT_FONT - (ADJACENT_FONT - EDGE_FONT) * t;
                opacity = 0.6 - 0.35 * t;
            }
            else
            {
                fontSize = EDGE_FONT;
                opacity = 0.25;
            }

            if (!inRange) opacity *= 0.3;

            string text = inRange ? val.ToString(Format) : "—";
            Brush fg = dist < 0.6 ? accentBrush
                     : dist < 1.5 ? textBrush
                     : dimBrush;

            var ft = new FormattedText(text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface, fontSize, fg,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            double x = (ActualWidth - ft.Width) / 2.0;
            dc.PushOpacity(opacity);
            dc.DrawText(ft, new Point(x, y - ft.Height / 2.0));
            dc.Pop();
        }

        // 中心选中区域指示线
        var linePen = new Pen(
            new SolidColorBrush(Color.FromArgb(0x28, 0x00, 0x00, 0x00)), 1);
        double lineY1 = centerY - ITEM_HEIGHT / 2.0;
        double lineY2 = centerY + ITEM_HEIGHT / 2.0;
        dc.DrawLine(linePen, new Point(6, lineY1), new Point(ActualWidth - 6, lineY1));
        dc.DrawLine(linePen, new Point(6, lineY2), new Point(ActualWidth - 6, lineY2));
    }

    /// <summary>
    /// 从主题资源获取强调色，失败回退硬编码值。
    /// </summary>
    private Brush GetAccentBrush()
    {
        try { return (Brush)Application.Current.FindResource("SidebarAccent"); }
        catch { return new SolidColorBrush(Color.FromRgb(0x4A, 0x7C, 0xF7)); }
    }

    /// <summary>
    /// 从主题资源获取主文字色，失败回退黑色。
    /// </summary>
    private Brush GetTextBrush()
    {
        try { return (Brush)Application.Current.FindResource("TextPrimary"); }
        catch { return Brushes.Black; }
    }

    /// <summary>
    /// 从主题资源获取次要文字色，失败回退灰色。
    /// </summary>
    private Brush GetDimBrush()
    {
        try { return (Brush)Application.Current.FindResource("TextSecondary"); }
        catch { return new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)); }
    }

    /// <summary>将值限制在 [Minimum, Maximum] 区间。</summary>
    private int Clamp(int v) => Math.Max(Minimum, Math.Min(Maximum, v));

    /// <summary>键盘 ↑↓ 切换值。</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Up)
        {
            var v = Clamp(Value + 1);
            if (v != Value) Value = v;
        }
        else if (e.Key == Key.Down)
        {
            var v = Clamp(Value - 1);
            if (v != Value) Value = v;
        }
    }
}
