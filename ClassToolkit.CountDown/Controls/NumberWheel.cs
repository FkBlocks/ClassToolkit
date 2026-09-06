using ClassToolkit.Core.Services;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ClassToolkit.CountDown.Controls;

/// <summary>
/// 循环式轮盘数字选择器。垂直排列数字，中心高亮为当前值。
/// 数值首尾相接（59 往上滚绕回 00，00 往下滚绕到 59），可无限滚动。
/// 手感类似真实转盘：拖拽跟手，松手后按瞬时速度惯性滚动（甩得越猛滚得越远），
/// 指数摩擦让速度渐近耗尽、缓慢自然停下，再以 easeOutCubic 缓动落位。
/// 拖拽由宿主容器通过 BeginDrag / DragTo / EndDrag 驱动（MainWindow 中整个
/// 轮盘组区域都是滑动热区），鼠标滚轮与键盘 ↑↓ 也可调节。
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

    /// <summary>
    /// 数字横向排版方向（3D 透视用）：1 = 靠右，0 = 居中，-1 = 靠左。
    /// 左侧轮盘设 1、中间设 0、右侧设 -1，配合透视缩放/灰度营造真实轮盘效果。
    /// </summary>
    public static readonly DependencyProperty TextBiasProperty =
        DependencyProperty.Register(nameof(TextBias), typeof(double), typeof(NumberWheel),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>数字横向排版方向（-1 靠左，0 居中，1 靠右）</summary>
    public double TextBias { get => (double)GetValue(TextBiasProperty); set => SetValue(TextBiasProperty, value); }

    // ── 动画状态 ────────────────────────────

    private double _displayOffset;       // 当前视觉偏移量（相对于 _lastValue，单位：格）
    private double _velocity;            // 当前滚动速度（格/秒，上滚为正）
    private int _lastValue;              // 视觉中心对应的整数值
    private bool _isDragging;            // 是否正在拖拽中
    private bool _isSnapping;            // 是否处于落位缓动阶段
    private double _snapStartOffset;     // 落位缓动开始时的残差
    private double _snapStartSeconds;    // 落位缓动开始时间（秒）
    private double _snapDuration;        // 落位缓动时长（秒）
    private double _dragStartY;          // 拖拽起始 Y 坐标
    private double _dragStartOffset;     // 拖拽起始偏移量
    private double _dragLastY;           // 上一次测速的指针 Y 坐标
    private double _dragLastSeconds;     // 上一次测速时间（秒）
    private double _dragVelocity;        // 拖拽平滑后的瞬时速度（格/秒）
    private readonly Stopwatch _clock = Stopwatch.StartNew();  // 高精度计时
    private double _lastFrameSeconds;    // 上一动画帧时间（秒）
    private bool _isAnimating;           // 是否已挂载渲染帧回调
    private readonly EventHandler _frameHandler;

    // ── 外观常量 ────────────────────────────

    private const double ITEM_HEIGHT = 38;       // 每个数字槽的高度（越小越紧凑）
    private const int VISIBLE_SLOTS = 5;         // 可见槽位数（奇数，中间为选中）
    private const int HALF_SLOTS = VISIBLE_SLOTS / 2;
    private const double CENTER_FONT = 30;       // 中心选中数字字号
    private const double ADJACENT_FONT = 14;     // 紧邻数字字号
    private const double EDGE_FONT = 11;         // 边缘数字字号
    private const double WHEEL_WIDTH = 90;

    // ── 3D 透视外观常量 ─────────────────────

    private const double BASE_OFFSET = 10;             // 偏置轮盘的基础横向偏移（px）
    private const double ARC_SHIFT = 30;               // 弧面横向偏移幅度（边缘最大，px）
    private const double PERSPECTIVE_ANGLE_MAX = 1.25; // 边缘最大视角（弧度，约 72°）
    private const double PERSPECTIVE_FALLOFF = 0.8;    // 字号衰减（越小越柔和）
    private const byte PERSPECTIVE_GRAY = 0x9A;         // 远距离数字的目标灰度值

    // ── 物理常量（真实转盘式惯性手感）──────────

    private const double FLING_KICK = 1.0;       // 松手惯性初速度倍率（1 = 完全跟手）
    private const double FLING_FRICTION = 3.0;   // 指数摩擦系数（1/秒，越小滚得越久、停得越绵长）
    private const double FLING_DECAY = 4.0;      // 指针静止时速度衰减（1/秒，防止精确拖动后滑走）
    private const double FLING_MIN_SPEED = 1.0;  // 松手速度低于该值（格/秒）时不做惯性滚动
    private const double SNAP_SPEED = 0.05;      // 速度低于该值（格/秒）时转入落位缓动

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
        _lastFrameSeconds = _clock.Elapsed.TotalSeconds;

        // 使用 CompositionTarget.Rendering 驱动动画：与显示器刷新同步，
        // 60Hz 屏幕 60fps、高刷屏自动 120/144fps，比 16ms 定时器丝滑得多。
        _frameHandler = OnAnimationTick;

        MouseWheel += OnMouseWheel;

        // 主题/主题色变化时刷新轮盘强调色（Unloaded 时反订阅）
        ThemeBootstrap.ThemeApplied += OnThemeApplied;

        Loaded += (_, _) => InvalidateVisual();
        Unloaded += (_, _) =>
        {
            StopAnimation();
            ThemeBootstrap.ThemeApplied -= OnThemeApplied;
        };
    }

    /// <summary>主题重新应用后强制重绘，让中心数字的强调色实时跟随。</summary>
    private void OnThemeApplied() => InvalidateVisual();

    /// <summary>挂载渲染帧回调开始动画（幂等）。</summary>
    private void StartAnimation()
    {
        if (_isAnimating) return;
        _isAnimating = true;
        _lastFrameSeconds = _clock.Elapsed.TotalSeconds;
        CompositionTarget.Rendering += _frameHandler;
    }

    /// <summary>卸载渲染帧回调停止动画（幂等）。</summary>
    private void StopAnimation()
    {
        if (!_isAnimating) return;
        _isAnimating = false;
        CompositionTarget.Rendering -= _frameHandler;
    }

    /// <summary>
    /// Value 依赖属性变更回调（外部赋值路径：重置按钮、键盘、ScrollBy 等）。
    /// 循环轮盘：以当前视觉中心为起点，沿最短环绕路径滚向新值。
    /// </summary>
    private void OnValueChanged()
    {
        int center = (int)Math.Round(_lastValue + _displayOffset);
        int delta = WrapDelta(center, Value);
        if (delta == 0) return;   // 视觉中心已对齐新值（内部重基准触发的回调走这里，不打断动画）

        int newBase = center + delta;             // 不环绕的内部基准
        _displayOffset += _lastValue - newBase;   // 保持视觉位置连续
        _velocity = 0;
        _isSnapping = false;    // 取消进行中的落位缓动，稍后以新基准重新落位
        _lastValue = newBase;
        StartAnimation();
    }

    /// <summary>
    /// 鼠标滚轮：上滚数值增加，下滚数值减小，带方向轻踢。
    /// </summary>
    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        ScrollBy(e.Delta > 0 ? 1 : -1);
        e.Handled = true;
    }

    /// <summary>
    /// 滚动一格（宿主可在更大的手势区域内转发滚轮事件）。
    /// delta &gt; 0 时数值增加，delta &lt; 0 时数值减小。
    /// </summary>
    public void ScrollBy(int delta)
    {
        int newVal = Wrap(Value + delta);
        if (newVal == Value) return;   // Minimum == Maximum 的退化情形

        // 外部改值路径：OnValueChanged 沿最短环绕路径重基准、清零速度并启动动画，
        // 之后补上方向初速度，让轮盘带一点惯性滚入新值（滚一格只滚一小段）。
        Value = newVal;
        _velocity = delta * 3.5;
    }

    /// <summary>
    /// 开始一次拖拽（由宿主容器调用）。pointerY 为指针在控件坐标系中的 Y 坐标。
    /// 拖拽期间暂停动画，偏移量跟随指针，并开始采样指针速度。
    /// </summary>
    public void BeginDrag(double pointerY)
    {
        _isDragging = true;
        _isSnapping = false;
        _dragStartY = pointerY;
        _dragStartOffset = _displayOffset;
        _dragLastY = pointerY;
        _dragLastSeconds = _clock.Elapsed.TotalSeconds;
        _dragVelocity = 0;
        _velocity = 0;
        StopAnimation();
    }

    /// <summary>
    /// 拖拽中更新偏移量（由宿主容器调用）。上拖数值增加，下拖数值减小。
    /// </summary>
    public void DragTo(double pointerY)
    {
        if (!_isDragging) return;

        double now = _clock.Elapsed.TotalSeconds;

        // 指针静止时速度按指数衰减：停下来片刻，甩动速度就会被"洗掉"，
        // 从而区分"精确拖动到位后松手"（不会滑走）和"甩动后立刻松手"（保留惯性）。
        double idle = now - _dragLastSeconds;
        if (idle > 0.01)
            _dragVelocity *= Math.Exp(-FLING_DECAY * idle);

        // 采样指针瞬时速度（格/秒，上滑为正）：只在指针明显移动时采样并做指数平滑
        if (Math.Abs(pointerY - _dragLastY) > 0.5)
        {
            double sampleDt = now - _dragLastSeconds;
            if (sampleDt > 0.001)
            {
                double instant = -(pointerY - _dragLastY) / ITEM_HEIGHT / sampleDt;
                _dragVelocity = _dragVelocity * 0.5 + instant * 0.5;
            }
            _dragLastY = pointerY;
            _dragLastSeconds = now;
        }

        double dy = pointerY - _dragStartY;
        _displayOffset = _dragStartOffset - dy / ITEM_HEIGHT;
        InvalidateVisual();
    }

    /// <summary>
    /// 结束拖拽（由宿主容器调用）。重基准到最近整数后，
    /// 以松手时的瞬时速度进入惯性滚动。
    /// </summary>
    public void EndDrag()
    {
        if (!_isDragging) return;
        _isDragging = false;
        _isSnapping = false;

        // 手动重基准到最近的整数（视觉位置完全连续）：内部基准不环绕、残差保持在
        // ±0.5 格内，Value 属性则映射为环绕后的值，保证落位缓动只滚一小段。
        double total = _lastValue + _displayOffset;
        int center = (int)Math.Round(total);
        _displayOffset = total - center;
        _lastValue = center;
        Value = Wrap(center);   // 回调里中心与新值环绕差为 0 → 直接跳过，速度不受影响

        // 松手前的最后一段静止时间也做衰减 + 低速死区：
        // 拖到位后停一会儿再松手，轮盘就稳稳停在目标，不会继续滑走。
        double idle = _clock.Elapsed.TotalSeconds - _dragLastSeconds;
        if (idle > 0.02)
            _dragVelocity *= Math.Exp(-FLING_DECAY * idle);
        if (Math.Abs(_dragVelocity) < FLING_MIN_SPEED)
            _dragVelocity = 0;

        // 以松手时的瞬时速度进入惯性滚动
        _velocity = _dragVelocity * FLING_KICK;
        StartAnimation();
    }

    /// <summary>
    /// 动画帧（约 60fps）。三个阶段：
    /// 惯性滚动（指数摩擦，速度渐近耗尽，像真实转盘一样缓慢停下；
    /// 循环轮盘没有边界，可无限滚动）→ 落位缓动（easeOutCubic 滑向最近整数）。
    /// 静止后自动停表。
    /// </summary>
    private void OnAnimationTick(object? sender, EventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        double dt = Math.Min(now - _lastFrameSeconds, 0.04);
        _lastFrameSeconds = now;
        if (_isDragging) return;

        if (_isSnapping)
        {
            // ── 落位阶段：easeOutCubic 缓动，平滑滑向最近的整数 ──
            double t = Math.Min((now - _snapStartSeconds) / _snapDuration, 1.0);
            double eased = 1.0 - Math.Pow(1.0 - t, 3.0);
            _displayOffset = _snapStartOffset * (1.0 - eased);

            if (t >= 1.0)
            {
                _displayOffset = 0;
                _velocity = 0;
                _isSnapping = false;
                StopAnimation();
            }
        }
        else if (Math.Abs(_velocity) < SNAP_SPEED)
        {
            // 没有惯性速度 → 进入落位缓动；已经居中则直接停表
            if (Math.Abs(_displayOffset) < 0.0005)
            {
                _displayOffset = 0;
                _velocity = 0;
                StopAnimation();
            }
            else
            {
                _isSnapping = true;
                _snapStartOffset = _displayOffset;
                _snapStartSeconds = now;
                _snapDuration = Math.Min(0.2 + Math.Abs(_displayOffset) * 0.35, 1.4);
            }
        }
        else
        {
            // ── 惯性滚动阶段：指数摩擦，速度渐近耗尽，像真实转盘一样缓慢停下 ──
            double step = _velocity * dt;
            _displayOffset += step;
            _velocity *= Math.Exp(-FLING_FRICTION * dt);
        }

        // 视觉中心越过整数时实时更新 Value（手动重基准，不打断滚动动画）。
        // 落位缓动期间不重基准：缓动距离最大可达整段滚动距离，
        // 中间穿插重基准会被缓动公式覆盖造成跳变，落位完成时 Value 自然对齐。
        if (!_isSnapping)
        {
            int center = (int)Math.Round(_lastValue + _displayOffset);
            if (center != _lastValue)
            {
                _displayOffset += _lastValue - center;
                _lastValue = center;
                Value = Wrap(center);   // 回调里环绕差为 0 → 直接跳过
            }
        }

        InvalidateVisual();
    }

    /// <summary>
    /// 自绘渲染。以 _lastValue + _displayOffset 为中心，上下各延伸 HALF_SLOTS+1 个数字。
    /// 3D 透视：数字按与中心的距离做字号缩放、灰度渐变、透明度渐隐，
    /// 并按 TextBias 整体偏移 + 逐数字弧面位移（只移动、不变形）。
    /// </summary>
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double centerY = ActualHeight / 2.0;
        var typeface = new Typeface("Microsoft YaHei");
        var accentBrush = GetAccentBrush();
        var textBrush = GetTextBrush();

        double totalDisplay = _lastValue + _displayOffset;
        int centerVal = (int)Math.Round(totalDisplay);
        int startVal = centerVal - HALF_SLOTS - 1;
        int endVal = centerVal + HALF_SLOTS + 1;

        for (int val = startVal; val <= endVal; val++)
        {
            double y = centerY + (val - totalDisplay) * ITEM_HEIGHT;
            if (y < -ITEM_HEIGHT || y > ActualHeight + ITEM_HEIGHT)
                continue;

            double dist = Math.Abs(val - totalDisplay);

            // ── 3D 透视参数：距离中心越远，等效视角越大 ──
            double t = Math.Min(dist / (HALF_SLOTS + 1.0), 1.0);
            double angle = t * PERSPECTIVE_ANGLE_MAX;
            double cosA = Math.Cos(angle);

            // 高度方向字号衰减（柔和，避免显得很小）
            double scale = Math.Pow(1.0 - t, PERSPECTIVE_FALLOFF);
            double fontSize = Math.Max(CENTER_FONT * scale, EDGE_FONT * 0.7);

            // 透明度渐隐
            double opacity = 1.0 - 0.65 * t * t;

            // 灰度阶：中心用强调色，向外从主文字色渐变成灰色
            Brush fg = dist < 0.5 ? accentBrush : BlendToGray(textBrush, t);

            string text = Wrap(val).ToString(Format);
            var ft = new FormattedText(text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface, fontSize, fg,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // 个性化位置：居中为基准，TextBias 让轮盘整体向左/右偏，
            // 离中心越远的数字横向偏移越大（弧面透视）——只移动、不拉伸字形
            double x = (ActualWidth - ft.Width) / 2.0
                     + TextBias * (BASE_OFFSET + ARC_SHIFT * (1.0 - cosA));

            dc.PushOpacity(opacity);
            dc.DrawText(ft, new Point(x, y - ft.Height / 2.0));
            dc.Pop();
        }
    }

    /// <summary>
    /// 从主题资源获取强调色（GetBrush 内部自带色板回退）。
    /// </summary>
    private Brush GetAccentBrush() =>
        ThemeService.GetBrush(ThemeService.Keys.SidebarAccent);

    /// <summary>
    /// 从主题资源获取主文字色。
    /// </summary>
    private Brush GetTextBrush() =>
        ThemeService.GetBrush(ThemeService.Keys.TextPrimary);

    /// <summary>
    /// 从主题资源获取次要文字色。
    /// </summary>
    private Brush GetDimBrush() =>
        ThemeService.GetBrush(ThemeService.Keys.TextSecondary);

    /// <summary>
    /// 将文字色按 t(0~1) 渐变成灰色，用于远距离数字的灰度阶。
    /// </summary>
    private Brush BlendToGray(Brush source, double t)
    {
        var c = ((SolidColorBrush)source).Color;
        return new SolidColorBrush(Color.FromRgb(
            (byte)(c.R + (PERSPECTIVE_GRAY - c.R) * t),
            (byte)(c.G + (PERSPECTIVE_GRAY - c.G) * t),
            (byte)(c.B + (PERSPECTIVE_GRAY - c.B) * t)));
    }

    /// <summary>循环取值：把任意整数环绕到 [Minimum, Maximum] 区间。</summary>
    private int Wrap(int v)
    {
        int count = Maximum - Minimum + 1;
        int r = (v - Minimum) % count;
        if (r < 0) r += count;
        return Minimum + r;
    }

    /// <summary>从 from 到 to 的最短环绕距离（带符号，格数）。</summary>
    private int WrapDelta(int from, int to)
    {
        int count = Maximum - Minimum + 1;
        int d = (to - from) % count;
        if (d > count / 2) d -= count;
        else if (d < -count / 2) d += count;
        return d;
    }

    /// <summary>键盘 ↑↓ 切换值（循环）。</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Up)
            Value = Wrap(Value + 1);
        else if (e.Key == Key.Down)
            Value = Wrap(Value - 1);
    }
}
