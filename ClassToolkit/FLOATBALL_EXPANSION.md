# 悬浮球扩展方案

> 目标：悬浮球支持**任意形状切换**、**颜色/主题自适应**、**内嵌文本/emoji/图片/交互控件**、**智能音量控制（自动检测外部媒体播放）**。

---

## 目录

- [一、总体架构改造](#一总体架构改造)
- [二、形状系统](#二形状系统)
- [三、颜色 / 外观系统](#三颜色外观系统)
- [四、内嵌内容展示（文本/emoji/图片）](#四内嵌内容展示文本emoji图片)
- [五、音量控制集成（智能检测外部媒体）](#五音量控制集成智能检测外部媒体)
- [六、修改 / 新增位置清单](#六修改--新增位置清单)
- [七、实现顺序建议](#七实现顺序建议)
- [八、关键注意事项](#八关键注意事项)

---

## 一、总体架构改造

### 现状

```
Window (60×60, AllowsTransparency, Topmost)
  └─ Grid
       ├─ Ellipse (固定圆形, 硬编码 #EF121212 填充 + White 描边)
       └─ Border Menu (折叠, TranslateTransform 偏移)
```

### 改造后

```
Window (Width/Height 可变, AllowsTransparency, Topmost)
  └─ Grid
       ├─ Path (形状由 BallShape 枚举驱动, 颜色绑定 DynamicResource)
       ├─ ContentControl (内嵌文本/emoji/图片/交互控件, 按需显示)
       └─ Border Menu (折叠菜单, 保持不变)
```

**核心思路**：把硬编码的 `Ellipse` + `EllipseGeometry` Clip 拆成三个可独立控制的概念：

- **Clip 几何**（窗口外形）→ `BallShape` 枚举 + 工厂方法，支持 Circle / Pill / RoundedSquare / VerticalPill
- **填充/描边**（视觉皮肤）→ `DynamicResource` 绑定 + `BallAppearance` 配置
- **内容层**（嵌入信息）→ `ContentControl`，可以是纯文本、emoji+文字卡片、或交互控件（如 Slider）

### 形状与应用场景对照

| 形状 | 尺寸 | 应用场景 |
|---|---|---|
| `Circle` | 60×60 | 默认待机状态 |
| `Pill` | 160-260×60 | 通知卡片（图标+标题+详情） |
| `RoundedSquare` | 80×80 | 快捷面板 |
| `VerticalPill` | 80×360 | 音量控制、滚动信息 |

---

## 二、形状系统

### 2.1 枚举定义

```csharp
// 位置：ClassToolkit/MainWindow.xaml.cs，MainWindow 类内部

/// <summary>
/// 悬浮球显示形态。可通过 SetBallShape() 随时切换并带动画。
/// </summary>
public enum BallShape
{
    /// <summary>正圆形（默认，60×60）</summary>
    Circle,

    /// <summary>水平药丸（高度固定 60，宽度 160-260 自适应内容）</summary>
    Pill,

    /// <summary>圆角正方形</summary>
    RoundedSquare,

    /// <summary>竖长胶囊（宽度固定 80，高度可变，用于音量控制等）</summary>
    VerticalPill,
}
```

### 2.2 几何工厂

```csharp
// 位置：ClassToolkit/MainWindow.xaml.cs，MainWindow 类内部

/// <summary>
/// 根据形状枚举和窗口尺寸生成对应的 Clip 几何。
/// WPF 的 Clip 接受任意 Geometry，窗口会被裁剪成对应形状。
/// </summary>
private static Geometry CreateClipGeometry(BallShape shape, double w, double h)
{
    switch (shape)
    {
        case BallShape.Circle:
        {
            double r = Math.Min(w, h) / 2;
            return new EllipseGeometry(new Point(w / 2, h / 2), r, r);
        }

        case BallShape.Pill:
        {
            // 药丸 = 矩形 + 半圆两端（RectangleGeometry 的 RadiusX/Y = 半高）
            double rx = h / 2;
            double ry = h / 2;
            return new RectangleGeometry(new Rect(0, 0, w, h), rx, ry);
        }

        case BallShape.RoundedSquare:
        {
            double r = Math.Min(w, h) * 0.2;
            return new RectangleGeometry(new Rect(0, 0, w, h), r, r);
        }

        case BallShape.VerticalPill:
        {
            // 竖胶囊：RadiusX/Y = 半宽 → 上下两端半圆
            double rx = w / 2;
            double ry = w / 2;
            return new RectangleGeometry(new Rect(0, 0, w, h), rx, ry);
        }

        default:
            goto case BallShape.Circle;
    }
}
```

### 2.3 形状切换 + 动画

```csharp
// 位置：ClassToolkit/MainWindow.xaml.cs，MainWindow 类内部

/// <summary>
/// 平滑切换悬浮球形状。同时动画窗口宽高和 Clip 几何。
/// 调用示例：SetBallShape(BallShape.VerticalPill, 80, 360);
/// </summary>
/// <param name="shape">目标形状</param>
/// <param name="targetWidth">目标窗口宽度（WPF 单位）</param>
/// <param name="targetHeight">目标窗口高度</param>
private void SetBallShape(BallShape shape, double targetWidth, double targetHeight)
{
    _currentShape = shape;

    var duration = TimeSpan.FromMilliseconds(250);
    var easing = new SineEase { EasingMode = EasingMode.EaseInOut };

    var widthAnim = new DoubleAnimation(targetWidth, duration) { EasingFunction = easing };
    var heightAnim = new DoubleAnimation(targetHeight, duration) { EasingFunction = easing };

    // 动画期间每帧刷新 Clip，保证形状跟随宽高平滑过渡
    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
    timer.Tick += (_, _) =>
    {
        if (ActualWidth > 0 && ActualHeight > 0)
            Clip = CreateClipGeometry(shape, ActualWidth, ActualHeight);
    };
    timer.Start();

    widthAnim.Completed += (_, _) =>
    {
        timer.Stop();
        Clip = CreateClipGeometry(shape, ActualWidth, ActualHeight);
    };

    BeginAnimation(WidthProperty, widthAnim);
    BeginAnimation(HeightProperty, heightAnim);
}
```

---

## 三、颜色 / 外观系统

### 3.1 外观配置模型

```csharp
// 位置：ClassToolkit/MainWindow.xaml.cs，MainWindow 类内部

/// <summary>
/// 悬浮球外观配置。所有字段可运行时修改并即时刷新。
/// </summary>
public sealed class BallAppearance
{
    /// <summary>当前形状</summary>
    public BallShape Shape { get; set; } = BallShape.Circle;

    /// <summary>填充画刷（支持 DynamicResource 或代码指定）</summary>
    public Brush FillBrush { get; set; } =
        new SolidColorBrush(Color.FromArgb(0xEF, 0x12, 0x12, 0x12));

    /// <summary>描边画刷</summary>
    public Brush StrokeBrush { get; set; } = Brushes.White;

    /// <summary>描边粗细</summary>
    public double StrokeThickness { get; set; } = 1;

    /// <summary>圆形/圆角方形边长</summary>
    public double CircleSize { get; set; } = 60;

    /// <summary>水平药丸展开宽度</summary>
    public double PillWidth { get; set; } = 220;

    /// <summary>药丸高度</summary>
    public double PillHeight { get; set; } = 60;

    /// <summary>竖胶囊宽度</summary>
    public double VerticalPillWidth { get; set; } = 80;

    /// <summary>竖胶囊高度</summary>
    public double VerticalPillHeight { get; set; } = 360;
}
```

### 3.2 XAML 改动

**现状**（`ClassToolkit/MainWindow.xaml` 第 20-22 行）：

```xml
<Ellipse Fill="#EF121212"
         Stroke="White"
         StrokeThickness="1"/>
```

**改造**：`Ellipse` → `Path`，颜色用 `DynamicResource` 实现主题自适应：

```xml
<!-- 位置：ClassToolkit/MainWindow.xaml，替换原有 Ellipse -->

<Path x:Name="BallShapePath"
      Fill="{DynamicResource ControlBackground}"
      Stroke="{DynamicResource ControlBorder}"
      StrokeThickness="1"/>

<!-- 内嵌内容层（默认折叠，有内容时展开） -->
<ContentControl x:Name="BallContent"
                HorizontalAlignment="Center"
                VerticalAlignment="Center"
                Visibility="Collapsed">
    <TextBlock x:Name="BallText"
               Foreground="{DynamicResource TextPrimary}"
               FontFamily="Microsoft YaHei"
               FontSize="22"
               FontWeight="Bold"
               HorizontalAlignment="Center"
               VerticalAlignment="Center"/>
</ContentControl>
```

### 3.3 颜色来源选择

| 方式 | 适用场景 |
|---|---|
| `DynamicResource` | 跟随主题自动切换浅色/深色 |
| 硬编码 `SolidColorBrush` | 固定品牌色 |
| 代码运行时修改 | 用户通过设置界面自定义颜色 |

---

## 四、内嵌内容展示（文本/emoji/图片）

### 4.1 内容层级设计

```
BallContent (ContentControl)
  ├─ Collapsed              → 纯球模式（无文字）
  ├─ TextBlock only         → 球内短文本（1-3 字，Circle 形态）
  ├─ StackPanel (Horizontal) → 药丸通知卡片（Pill 形态）
  │    ├─ TextBlock emoji   → 图标
  │    ├─ TextBlock         → 标题
  │    └─ TextBlock         → 详情
  └─ StackPanel (Vertical)  → 竖胶囊交互控件（VerticalPill 形态）
       ├─ TextBlock emoji   → 图标/标识
       ├─ Slider            → 交互控件（如音量）
       └─ TextBlock         → 数值显示
```

### 4.2 短文本 API

```csharp
/// <summary>
/// 在悬浮球内显示短文本（如倒计时数字、未读计数）。
/// 适用于 Circle 形态，2-3 个字符最佳。
/// </summary>
public void ShowBallText(string text)
{
    BallContent.Content = BallText;
    BallContent.Visibility = Visibility.Visible;
    BallText.Text = text;
    BallText.FontSize = text.Length <= 2 ? 24 : 18;
}

/// <summary>隐藏球内文字，回到纯球模式。</summary>
public void HideBallText()
{
    BallContent.Visibility = Visibility.Collapsed;
    BallText.Text = "";
}
```

### 4.3 通知卡片 API（水平药丸）

```csharp
/// <summary>
/// 展开水平药丸并显示通知卡片。
/// 自动切换到 Pill 形状，内部排列 emoji 图标 + 标题 + 详情。
/// </summary>
public void ShowPillInfo(string icon, string title, string detail = "")
{
    var panel = new StackPanel
    {
        Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(12, 0, 16, 0)
    };

    if (!string.IsNullOrEmpty(icon))
    {
        panel.Children.Add(new TextBlock
        {
            Text = icon, FontSize = 22,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
    }

    var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
    textStack.Children.Add(new TextBlock
    {
        Text = title,
        FontFamily = new FontFamily("Microsoft YaHei"),
        FontSize = 13, FontWeight = FontWeights.Bold,
        Foreground = (Brush)App.Current.FindResource("TextPrimary"),
        TextTrimming = TextTrimming.CharacterEllipsis
    });

    if (!string.IsNullOrEmpty(detail))
    {
        textStack.Children.Add(new TextBlock
        {
            Text = detail,
            FontFamily = new FontFamily("Microsoft YaHei"),
            FontSize = 11,
            Foreground = (Brush)App.Current.FindResource("TextSecondary"),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
    }

    panel.Children.Add(textStack);
    BallContent.Content = panel;
    BallContent.Visibility = Visibility.Visible;

    double pillWidth = Math.Max(180, 120 + title.Length * 14);
    SetBallShape(BallShape.Pill, pillWidth, _appearance.PillHeight);
}

/// <summary>收起药丸，回到圆形纯球模式。</summary>
public void CollapseToCircle()
{
    BallContent.Content = BallText;
    BallContent.Visibility = Visibility.Collapsed;
    SetBallShape(BallShape.Circle, _appearance.CircleSize, _appearance.CircleSize);
}
```

### 4.4 使用示例

```csharp
// 场景 1: 倒计时结束提醒
ballWindow.ShowPillInfo("⏰", "倒计时结束", "3 分钟已到");

// 场景 2: 球内显示未读计数
ballWindow.ShowBallText("3");

// 场景 3: 错误提示
ballWindow.ShowPillInfo("⚠️", "名单加载失败", "请检查 names.txt");

// 场景 4: 成功反馈 → 3 秒后自动收回
ballWindow.ShowPillInfo("✅", "操作成功");
await Task.Delay(3000);
ballWindow.CollapseToCircle();
```

---

## 五、音量控制集成（智能检测外部媒体）

### 5.1 功能概述

悬浮球通过 Windows Core Audio API 监听系统音频活动，当检测到**非系统进程**正在持续播放音频时，自动展开为竖胶囊形态，嵌入垂直音量滑块供教师实时调节系统音量。

```
Circle（待机）
  │
  │  检测到非系统进程持续播放 > 3 秒
  │
  ▼
VerticalPill（音量模式）
  │  ┌──────────┐
  │  │    🔊    │ ← 音量图标
  │  │    ┃     │
  │  │    ┃╋╋╋  │ ← 竖条 Slider（实时调节系统音量）
  │  │    ┃     │
  │  │   65%    │ ← 音量百分比
  │  └──────────┘
  │
  │  外部媒体停止 + 3 秒无操作
  │
  ▼
Circle（待机）
```

### 5.2 音频检测：系统音 vs 外部媒体的区分

Windows 所有音频都通过同一个默认音频设备输出，但**不同来源属于不同的音频会话 (Audio Session)**，每个会话可以查到所属进程名。

#### 系统进程名单

系统提示音（U盘、通知、错误对话框）始终由以下进程托管：

| 进程 | 说明 |
|---|---|
| `audiodg.exe` | Windows 音频设备图隔离进程，所有系统音效的宿主 |
| `System` (PID 4) | 内核 |
| `svchost.exe` | Windows Audio 服务宿主 |
| `csrss.exe` | 客户端/服务器运行时 |
| `winlogon.exe` | 登录会话 |
| `dwm.exe` | 桌面窗口管理器 |

#### 判断逻辑

> 枚举所有活跃音频会话 → 过滤掉系统进程 → 剩余任何会话 = 外部媒体在播放

```csharp
private bool IsExternalMediaPlaying()
{
    var sessions = GetAudioSessions(); // IAudioSessionManager2 → enumerate

    foreach (var session in sessions)
    {
        if (session.State != AudioSessionState.Active)
            continue;

        string processName = GetProcessName(session.GetProcessId());

        if (IsSystemProcess(processName))
            continue; // 系统音 → 忽略

        return true;   // 非系统进程在播放 → 外部媒体
    }

    return false;
}

private static bool IsSystemProcess(string name)
{
    return name switch
    {
        "audiodg.exe"  => true,
        "system"       => true,
        "svchost.exe"  => true,
        "csrss.exe"    => true,
        "winlogon.exe" => true,
        "dwm.exe"      => true,
        _              => false,
    };
}
```

#### 实际场景覆盖

| 场景 | 活跃进程 | 系统进程？ | 触发？ |
|---|---|---|---|
| 浏览器放网课视频 | `chrome.exe` | ❌ | ✅ 3 秒后弹 |
| WMP 放音乐 | `wmplayer.exe` | ❌ | ✅ 3 秒后弹 |
| VLC / PotPlayer | `vlc.exe` / `potplayer.exe` | ❌ | ✅ 3 秒后弹 |
| U盘插入提示音 | `audiodg.exe` | ✅ | ❌ 被过滤 |
| PPT 切换音效 | `audiodg.exe` | ✅ | ❌ 被过滤 |
| Windows 错误提示音 | `audiodg.exe` | ✅ | ❌ 被过滤 |
| 钉钉/Teams 来电 | `DingTalk.exe` / `Teams.exe` | ❌ | ✅（合理——来电时需要调音量） |

### 5.3 Core Audio API COM Interop

需要定义以下 COM 接口（新建 `ClassToolkit/CoreAudioInterop.cs`）：

| 接口 | GUID | 作用 |
|---|---|---|
| `IMMDeviceEnumerator` | `A95664D2-9614-4F35-A746-DE8DB63617E6` | 枚举音频设备，获取默认渲染端点 |
| `IMMDevice` | `D666063F-1587-4E43-81F1-B948E807363F` | 代表一个音频设备，激活其他接口 |
| `IAudioSessionManager2` | `77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F` | 枚举所有音频会话（核心：通过进程 ID 区分来源） |
| `IAudioSessionControl2` | `BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D` | 单个会话的控制接口，可查询进程 ID 和状态 |
| `IAudioMeterInformation` | `C02216F6-8C67-4B5B-9D00-D008E73E0064` | 读取峰值电平（判断是否有音频活动） |
| `IAudioEndpointVolume` | `5CDF2C82-841E-4546-9722-0CF74078229A` | 读写系统主音量（Slider 控制的目标） |
| `IAudioSessionEnumerator` | `E2F5BB11-0570-40CA-ACDD-3AA01277DEE8` | 遍历会话集合 |

COM 对象创建入口：

```csharp
// 创建 MMDeviceEnumerator COM 实例
var enumerator = new MMDeviceEnumerator() as IMMDeviceEnumerator;
// 获取默认音频渲染设备
enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);
// 激活 IAudioSessionManager2
device.Activate(typeof(IAudioSessionManager2).GUID, 0, IntPtr.Zero, out var sessionManager);
```

### 5.4 状态机：时长门控 + 冷却退出

```
                     ┌──────────────────────────┐
                     │     Circle（纯球待机）     │
                     │  _isVolumeMode = false    │
                     └────────────┬─────────────┘
                                  │
                    轮询检测到外部媒体播放
                     activeFrames 累加
                                  │
                     activeFrames >= 15 (3秒)
                                  │
                                  ▼
                     ┌──────────────────────────┐
                     │  VerticalPill（音量模式） │
                     │  _isVolumeMode = true     │
                     │  轮询刷新滑块             │
                     └────────────┬─────────────┘
                                  │
                    外部媒体停止
                    idleFrames 累加
                                  │
              ┌───── 滑块被拖动？──┘
              │        │
              │    idleFrames 归零（容错）
              │        │
              └──→ 继续等待
                                  │
                     idleFrames >= 15 (3秒)
                     且滑块未被操作
                                  │
                                  ▼
                     ┌──────────────────────────┐
                     │     Circle（纯球待机）     │
                     └──────────────────────────┘
```

#### 关键变量

```csharp
// 位置：ClassToolkit/MainWindow.xaml.cs，MainWindow 字段区

private int _activeFrames = 0;       // 连续检测到外部媒体的帧数
private int _idleFrames = 0;         // 音频消失后的空闲帧数
private bool _isVolumeMode = false;  // 是否在音量控制模式
private bool _isSliderDragging;      // 用户是否正在拖动滑块
private float _lastVolumeLevel;      // 上次系统音量（用于检测外部变更）

private const int ACTIVE_THRESHOLD = 15;  // 15 × 200ms = 3 秒 → 展开
private const int IDLE_TIMEOUT = 15;      // 15 × 200ms = 3 秒 → 收回
```

#### 轮询 Tick（200ms 间隔）

```csharp
private void OnAudioPollTick(object? sender, EventArgs e)
{
    bool externalMedia = IsExternalMediaPlaying();

    if (externalMedia)
    {
        _idleFrames = 0; // 有外部媒体 → 清空空闲计数

        if (!_isVolumeMode)
        {
            _activeFrames++;
            if (_activeFrames >= ACTIVE_THRESHOLD)
            {
                ShowVolumeControl();       // 展开竖胶囊 + 音量控件
                _isVolumeMode = true;
                _activeFrames = 0;
            }
        }
        else
        {
            RefreshVolumeDisplay();        // 已展开 → 同步滑块位置
        }
    }
    else
    {
        _activeFrames = 0; // 无外部媒体 → 清空激活计数

        if (_isVolumeMode && !_isSliderDragging)
        {
            _idleFrames++;
            if (_idleFrames >= IDLE_TIMEOUT)
            {
                CollapseVolumeControl();   // 收回 Circle
                _isVolumeMode = false;
                _idleFrames = 0;
            }
        }
    }
}
```

### 5.5 音量控件布局（VerticalPill 内容）

窗口约 **80×360**，从上到下排列：

```
┌──────────────┐
│              │
│    🔊        │  ← TextBlock emoji，FontSize 20
│              │
│    ┃         │
│    ┃         │
│    ┃╋╋╋     │  ← Slider Orientation="Vertical"
│    ┃         │     Height 200，Width 40
│    ┃         │     Value 绑定 0-100
│    ┃         │
│              │
│    65%       │  ← TextBlock 音量数字，FontSize 16 Bold
│              │
└──────────────┘
```

#### 构建代码

```csharp
// 位置：ClassToolkit/MainWindow.xaml.cs，MainWindow 类内部

private Slider? _volumeSlider;
private TextBlock? _volumeLabel;

/// <summary>展开竖胶囊并嵌入音量控制控件。</summary>
private void ShowVolumeControl()
{
    var panel = new StackPanel
    {
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Center
    };

    // 音量图标
    panel.Children.Add(new TextBlock
    {
        Text = GetVolumeIcon(_lastVolumeLevel),
        FontSize = 20,
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(0, 8, 0, 12)
    });

    // 竖条 Slider
    _volumeSlider = new Slider
    {
        Orientation = Orientation.Vertical,
        Minimum = 0,
        Maximum = 100,
        Height = 200,
        Width = 40,
        HorizontalAlignment = HorizontalAlignment.Center
    };
    _volumeSlider.ValueChanged += OnVolumeSliderChanged;
    _volumeSlider.PreviewMouseLeftButtonDown += (_, _) => { _isSliderDragging = true;  _idleFrames = 0; };
    _volumeSlider.PreviewMouseLeftButtonUp   += (_, _) =>   _isSliderDragging = false;
    _volumeSlider.PreviewTouchDown           += (_, _) => { _isSliderDragging = true;  _idleFrames = 0; };
    _volumeSlider.PreviewTouchUp             += (_, _) =>   _isSliderDragging = false;
    panel.Children.Add(_volumeSlider);

    // 音量数字
    _volumeLabel = new TextBlock
    {
        Text = "0%",
        FontFamily = new FontFamily("Microsoft YaHei"),
        FontSize = 16,
        FontWeight = FontWeights.Bold,
        Foreground = (Brush)App.Current.FindResource("TextPrimary"),
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(0, 10, 0, 8)
    };
    panel.Children.Add(_volumeLabel);

    BallContent.Content = panel;
    BallContent.Visibility = Visibility.Visible;

    // 切换到竖胶囊
    SetBallShape(BallShape.VerticalPill,
        _appearance.VerticalPillWidth,
        _appearance.VerticalPillHeight);

    // 读取当前系统音量刷新 UI
    RefreshVolumeDisplay();
}

/// <summary>收回竖胶囊，恢复圆形。</summary>
private void CollapseVolumeControl()
{
    BallContent.Content = BallText;
    BallContent.Visibility = Visibility.Collapsed;
    _volumeSlider = null;
    _volumeLabel = null;
    SetBallShape(BallShape.Circle, _appearance.CircleSize, _appearance.CircleSize);
}
```

#### Slider 与系统音量的双向同步（防循环反馈）

```csharp
private bool _isUpdatingFromSystem;

/// <summary>用户拖动 Slider → 写系统音量。</summary>
private void OnVolumeSliderChanged(object sender,
    RoutedPropertyChangedEventArgs<double> e)
{
    if (_isUpdatingFromSystem) return; // 忽略来自系统同步的回写

    float newLevel = (float)(e.NewValue / 100.0);
    _audioEndpointVolume?.SetMasterVolumeLevelScalar(newLevel, Guid.Empty);
    _lastVolumeLevel = newLevel;
    UpdateVolumeLabel(newLevel);
}

/// <summary>定时刷新 → 从系统读音量 → 更新 Slider。</summary>
private void RefreshVolumeDisplay()
{
    if (_volumeSlider == null) return;
    _audioEndpointVolume?.GetMasterVolumeLevelScalar(out float level);

    _isUpdatingFromSystem = true;
    _volumeSlider.Value = Math.Round(level * 100);
    _isUpdatingFromSystem = false;

    UpdateVolumeLabel(level);
}

private void UpdateVolumeLabel(float level)
{
    if (_volumeLabel != null)
        _volumeLabel.Text = $"{Math.Round(level * 100)}%";
}

/// <summary>根据音量等级返回合适的图标。</summary>
private static string GetVolumeIcon(float level) => level switch
{
    0            => "🔇",
    < 0.33f       => "🔈",
    < 0.66f       => "🔉",
    _             => "🔊",
};
```

### 5.6 时间线演示

```
t=0s     Circle 待机
t=1s     老师打开浏览器，播放网课视频
         → chrome.exe 会话进入 Active 状态
         → IsExternalMediaPlaying() = true
         → activeFrames 开始累加

t=4s     activeFrames = 15 → ShowVolumeControl()
         → 250ms 动画展开 VerticalPill（80×360）
         → 显示音量图标 + 滑块 + 当前音量 "65%"

t=10s    老师拖动滑块从 65% → 80%
         → SetMasterVolumeLevelScalar(0.8)
         → _idleFrames 归零

t=300s   视频结束，老师关闭网页
         → IsExternalMediaPlaying() = false
         → idleFrames 开始累加

t=302s   idleFrames = 10
t=303s   idleFrames = 15 → CollapseVolumeControl()
         → 250ms 动画收回 Circle（60×60）
```

---

## 六、修改 / 新增位置清单

### 6.1 新建文件

| 文件 | 内容 |
|---|---|
| `ClassToolkit/CoreAudioInterop.cs` | COM 接口定义（`IMMDeviceEnumerator`、`IAudioSessionManager2`、`IAudioSessionControl2`、`IAudioEndpointVolume`、`IAudioMeterInformation` 等）、`MMDeviceEnumerator` COM 类、枚举（`EDataFlow`、`ERole`、`AudioSessionState`） |

### 6.2 XAML 改动（`ClassToolkit/MainWindow.xaml`）

| 位置 | 改动 |
|---|---|
| `Ellipse Fill=... Stroke=...` | **删除**，替换为 `Path x:Name="BallShapePath"` |
| `Path` 之后 | **新增** `ContentControl x:Name="BallContent"`（含默认 `TextBlock x:Name="BallText"`） |
| Menu Border | 保持不变 |

### 6.3 C# 新增（`ClassToolkit/MainWindow.xaml.cs`，`MainWindow` 类内部）

| 类别 | 内容 |
|---|---|
| **枚举/配置类** | `BallShape` 枚举、`BallAppearance` 配置类 |
| **形状字段** | `_currentShape`、`_appearance` |
| **形状方法** | `CreateClipGeometry()`、`SetBallShape()`、`UpdateClipFromAppearance()` |
| **内容方法** | `ShowBallText()`、`HideBallText()`、`ShowPillInfo()`、`CollapseToCircle()` |
| **音量字段** | `_audioPollTimer`、`_isVolumeMode`、`_isSliderDragging`、`_activeFrames`、`_idleFrames`、`_volumeSlider`、`_volumeLabel`、`_lastVolumeLevel`、`_isUpdatingFromSystem`、Core Audio COM 对象引用 |
| **音量常量** | `ACTIVE_THRESHOLD = 15`、`IDLE_TIMEOUT = 15` |
| **音量方法** | `IsExternalMediaPlaying()`、`IsSystemProcess()`、`ShowVolumeControl()`、`CollapseVolumeControl()`、`OnVolumeSliderChanged()`、`RefreshVolumeDisplay()`、`UpdateVolumeLabel()`、`GetVolumeIcon()`、`OnAudioPollTick()` |
| **启动** | 构造函数末尾启动 `_audioPollTimer`（`Interval = 200ms`） |
| **修改** | `Window_Loaded` 中 `UpdateClipToCircle()` → `UpdateClipFromAppearance()` |
| **修改** | `ClickBall` 中根据 `_currentShape` 区分 Circle（弹出菜单）和 VerticalPill（不弹菜单，让用户操作滑块） |

### 6.4 无需改动的部分

| 部分 | 原因 |
|---|---|
| Win32 `SetWindowPos` / `GetCursorPos` P/Invoke | 置顶和坐标获取与形状/内容无关 |
| `MakeSuperTopmost()` | 保持不变 |
| 拖拽三件套 | 拖拽逻辑只依赖 Left/Top，所有形状适用 |
| `ShowMenu()` / `CloseMenu()` | 菜单定位策略不变 |
| `OnToolClick()` / `LoadTools()` | 工具启动逻辑不受影响 |

### 6.5 可移除的部分

| 项目 | 说明 |
|---|---|
| `ClassToolkit.VolumeRecovery/` 整个项目 | 音量控制已内置到悬浮球，独立程序不再需要 |
| `data/tools.json` 中的 `["音量恢复"]` 条目 | 对应移除 |

---

## 七、实现顺序建议

| 阶段 | 内容 | 改动量 |
|---|---|---|
| **Phase 1** | 形状系统 | ~80 行 C# |
| | `BallShape` 枚举 + `CreateClipGeometry` 工厂 + `SetBallShape` 动画 | |
| **Phase 2** | 颜色解耦 + 内容层 | ~30 行 XAML + ~100 行 C# |
| | `Ellipse` → `Path` + `BallAppearance` + `ContentControl` + 四个内容 API | |
| **Phase 3** | Core Audio Interop | ~150 行 |
| | 新建 `CoreAudioInterop.cs`，所有 COM 接口定义 | |
| **Phase 4** | 音量控制集成 | ~150 行 C# |
| | 音频轮询 + 系统进程过滤 + 状态机 + `ShowVolumeControl` + Slider 双向同步 | |
| **Phase 5** | 清理 | ~5 行 |
| | 移除 VolumeRecovery 项目 + tools.json 条目更新 | |

---

## 八、关键注意事项

1. **`AllowsTransparency=True` 动画性能**：分层窗口每帧 resize 触发系统级重绘。`SetBallShape` 的 16ms 定时器仅在 250ms 动画期间运行，结束后立即停止。

2. **形状切换期间的用户交互**：动画过程中 Clip 每帧变化，但拖拽仍有效（WPF 的 `BeginAnimation` 不阻塞消息循环）。

3. **Circle 形态下的内容限制**：60×60 的圆形内只能容纳 1-3 个字符（字号 18-24）。

4. **COM 对象生命周期**：`IAudioEndpointVolume` 等 COM 对象需要在窗口关闭时通过 `Marshal.ReleaseComObject` 释放，避免泄漏。

5. **轮询开销**：200ms 一次 COM 调用，`GetMeteringChannelCount` + `GetChannelsPeakValues` 是读取共享内存中的 DSP 数据，不涉及磁盘 IO，CPU 开销可忽略。

6. **定时器频率 bug**（现有代码）：`MakeSuperTopmost` 中注释写"每 60000ms(60s)"但实际 `Interval = 500ms`（0.5 秒）。建议 Phase 1 顺手改注释或调整频率。

7. **VerticalPill 形态下的菜单行为**：竖胶囊展开时，点击悬浮球应让用户操作滑块而非弹出工具菜单。需要在 `ClickBall` → `LeftClickUp` 中根据 `_isVolumeMode` 做分支判断。

8. **多显示器**：悬浮球始终在主屏幕。`SystemParameters.PrimaryScreenWidth/Height` 在边界钳制中已使用，多屏场景无需额外处理。

9. **管理员权限下的音量控制**：`IAudioEndpointVolume` 不需要管理员权限即可读写主音量，标准用户权限足够。
