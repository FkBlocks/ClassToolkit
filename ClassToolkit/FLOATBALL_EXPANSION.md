# 悬浮球扩展方案

> 目标：悬浮球支持**任意形状切换**、**颜色/主题自适应**、**内嵌文本/emoji/图片展示**。

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
       ├─ Path/Geometry (形状由 BallShape 枚举驱动, 颜色绑定 DynamicResource)
       ├─ ContentControl (内嵌文本/emoji/图片, 按需显示)
       └─ Border Menu (保持不变)
```

**核心思路**：把硬编码的 `Ellipse` + `EllipseGeometry` Clip 拆成三个可独立控制的概念：
- **Clip 几何**（窗口外形）→ `BallShape` 枚举 + 工厂方法
- **填充/描边**（视觉皮肤）→ `DynamicResource` 绑定 + `BallAppearance` 配置
- **内容层**（嵌入信息）→ `ContentControl`，轻量时只显示 1-2 个字符，重量时展开药丸显示完整信息

---

## 二、形状系统

### 2.1 枚举定义

```csharp
// 位置：ClassToolkit/MainWindow.xaml.cs，MainWindow 类内部

/// <summary>
/// 悬浮球显示形态。可通过 BallAppearance.Shape 随时切换。
/// </summary>
public enum BallShape
{
    /// <summary>正圆形（默认，60×60）</summary>
    Circle,

    /// <summary>水平药丸（高度 60，宽度 160-260 自适应内容）</summary>
    Pill,

    /// <summary>圆角正方形</summary>
    RoundedSquare,

    /// <summary>竖长胶囊（如通知 badge）</summary>
    VerticalPill,
}
```

### 2.2 几何工厂

```csharp
// 位置：ClassToolkit/MainWindow.xaml.cs，MainWindow 类内部

/// <summary>
/// 根据形状枚举和窗口尺寸生成对应的 Clip 几何。
/// </summary>
/// <param name="shape">目标形状</param>
/// <param name="w">窗口当前 ActualWidth</param>
/// <param name="h">窗口当前 ActualHeight</param>
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
            // 药丸 = 两端半圆 + 中间矩形，用 RectangleGeometry + 大圆角模拟
            double rx = h / 2; // RadiusX = 半高 → 两端半圆
            double ry = h / 2;
            return new RectangleGeometry(new Rect(0, 0, w, h), rx, ry);
        }

        case BallShape.RoundedSquare:
        {
            double r = Math.Min(w, h) * 0.2; // 圆角 = 边长的 20%
            return new RectangleGeometry(new Rect(0, 0, w, h), r, r);
        }

        case BallShape.VerticalPill:
        {
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
/// 调用示例：SetBallShape(BallShape.Pill, 220, 60);
/// </summary>
/// <param name="shape">目标形状</param>
/// <param name="targetWidth">目标窗口宽度（WPF 单位）</param>
/// <param name="targetHeight">目标窗口高度</param>
private void SetBallShape(BallShape shape, double targetWidth, double targetHeight)
{
    _currentShape = shape;

    var duration = TimeSpan.FromMilliseconds(250);
    var easing = new SineEase { EasingMode = EasingMode.EaseInOut };

    // 宽高动画
    var widthAnim = new DoubleAnimation(targetWidth, duration) { EasingFunction = easing };
    var heightAnim = new DoubleAnimation(targetHeight, duration) { EasingFunction = easing };

    // 每帧刷新 Clip，保证动画过程中形状跟随
    var timer = new System.Windows.Threading.DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(16)
    };
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

    /// <summary>填充画刷（支持 DynamicResource 绑定）</summary>
    public Brush FillBrush { get; set; } = new SolidColorBrush(Color.FromArgb(0xEF, 0x12, 0x12, 0x12));

    /// <summary>描边画刷</summary>
    public Brush StrokeBrush { get; set; } = Brushes.White;

    /// <summary>描边粗细</summary>
    public double StrokeThickness { get; set; } = 1;

    /// <summary>圆形/圆角方形边长</summary>
    public double CircleSize { get; set; } = 60;

    /// <summary>药丸展开宽度</summary>
    public double PillWidth { get; set; } = 220;

    /// <summary>药丸/竖胶囊高度</summary>
    public double PillHeight { get; set; } = 60;
}
```

### 3.2 替换硬编码颜色

**现状**（XAML 第 20-22 行）：

```xml
<Ellipse Fill="#EF121212"
         Stroke="White"
         StrokeThickness="1"/>
```

**改造**：把 `Ellipse` 替换为 `Path`，形状由 Geometry 驱动，颜色用 `DynamicResource`：

```xml
<!-- 位置：ClassToolkit/MainWindow.xaml，替换原有 Ellipse -->

<Path x:Name="BallShapePath"
      Fill="{DynamicResource ControlBackground}"
      Stroke="{DynamicResource ControlBorder}"
      StrokeThickness="1"/>

<!-- 内嵌内容层 -->
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

| 方式 | 适用场景 | 示例 |
|---|---|---|
| `DynamicResource` | 跟随主题自动切换 | `Fill="{DynamicResource SidebarAccent}"` |
| 硬编码 `SolidColorBrush` | 固定品牌色 | `new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E))` |
| 代码运行时修改 | 用户自定义 | `_appearance.FillBrush = new SolidColorBrush(userColor)` |

推荐默认用 `DynamicResource`，并暴露 `BallAppearance` 给设置窗口，让用户可在设置界面自定义填充色/描边色。

---

## 四、内嵌文本 / 信息展示

### 4.1 内容层级设计

```
BallContent (ContentControl)
  ├─ Collapsed         → 纯球模式（无文字）
  ├─ TextBlock only    → 球内短文本（1-3 字，Circle 形态）
  └─ StackPanel        → 药丸展开完整信息（Pill 形态）
       ├─ Image        → 图标/头像
       ├─ TextBlock    → 标题
       └─ TextBlock    → 副标题/emoji
```

### 4.2 核心 API

```csharp
// 位置：ClassToolkit/MainWindow.xaml.cs，MainWindow 类内部

/// <summary>
/// 在悬浮球内显示短文本（如倒计时数字、未读计数）。
/// 适用于 Circle 形态，2-3 个字符最佳。
/// </summary>
public void ShowBallText(string text)
{
    BallContent.Visibility = Visibility.Visible;
    BallText.Text = text;
    BallText.FontSize = text.Length <= 2 ? 24 : 18;
}

/// <summary>
/// 隐藏球内文字，回到纯球模式。
/// </summary>
public void HideBallText()
{
    BallContent.Visibility = Visibility.Collapsed;
    BallText.Text = "";
}

/// <summary>
/// 展开药丸形态并显示完整信息卡片。
/// 自动切换到 Pill 形状，内部排列图标+标题+详情。
/// </summary>
/// <param name="icon">图标字符（emoji 如 "⚠️" / "✅"）或留空</param>
/// <param name="title">标题行文字</param>
/// <param name="detail">详情行文字（可选）</param>
/// <param name="durationMs">展开动画时长（毫秒），0=无动画</param>
public void ShowPillInfo(string icon, string title, string detail = "", int durationMs = 250)
{
    // 构建药丸内容
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
            Text = icon,
            FontSize = 22,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
    }

    var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
    textStack.Children.Add(new TextBlock
    {
        Text = title,
        FontFamily = new FontFamily("Microsoft YaHei"),
        FontSize = 13,
        FontWeight = FontWeights.Bold,
        Foreground = (Brush)Application.Current.FindResource("TextPrimary"),
        TextTrimming = TextTrimming.CharacterEllipsis
    });

    if (!string.IsNullOrEmpty(detail))
    {
        textStack.Children.Add(new TextBlock
        {
            Text = detail,
            FontFamily = new FontFamily("Microsoft YaHei"),
            FontSize = 11,
            Foreground = (Brush)Application.Current.FindResource("TextSecondary"),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
    }

    panel.Children.Add(textStack);
    BallContent.Content = panel;
    BallContent.Visibility = Visibility.Visible;

    // 切换到药丸形状
    double pillWidth = Math.Max(180, 120 + title.Length * 14);
    SetBallShape(BallShape.Pill, pillWidth, 60);
}

/// <summary>
/// 收起药丸，回到圆形纯球模式。
/// </summary>
public void CollapseToCircle()
{
    BallContent.Content = BallText; // 恢复默认 TextBlock
    BallContent.Visibility = Visibility.Collapsed;
    SetBallShape(BallShape.Circle, _appearance.CircleSize, _appearance.CircleSize);
}
```

### 4.3 使用示例

```csharp
// ── 场景 1: 倒计时结束后，悬浮球显示提醒 ──
ballWindow.ShowPillInfo("⏰", "倒计时结束", "3 分钟已到", durationMs: 300);

// ── 场景 2: 显示未读通知数 ──
ballWindow.ShowBallText("3"); // 球内显示 "3"

// ── 场景 3: 错误提示 ──
ballWindow.ShowPillInfo("⚠️", "名单加载失败", "请检查 names.txt");

// ── 场景 4: 成功反馈 → 3 秒后自动收回 ──
ballWindow.ShowPillInfo("✅", "操作成功");
await Task.Delay(3000);
ballWindow.CollapseToCircle();
```

---

## 五、修改 / 新增位置清单

### 5.1 XAML 改动（`ClassToolkit/MainWindow.xaml`）

| 行号 | 改动 |
|---|---|
| 7 | 窗口 `Height="60" Width="60"` 改为 `Height="{Binding CircleSize}"` 或保持动态控制 |
| 12 | 新增 `Background="Transparent"`（已有，保持） |
| 20-22 | **删除**硬编码 `Ellipse`，**替换为** `Path x:Name="BallShapePath"` + `ContentControl x:Name="BallContent"` |
| 24-36 | Menu Border 保持不变 |

### 5.2 C# 新增字段/属性（`ClassToolkit/MainWindow.xaml.cs`，`MainWindow` 类内部）

| 位置 | 内容 |
|---|---|
| ~16 行（类顶部） | `BallShape` 枚举 |
| ~16 行 | `BallAppearance` 配置类 |
| ~48-58 行（字段区） | `private BallAppearance _appearance`、`private BallShape _currentShape` |
| 构造函数 | 初始化 `_appearance` + 调用 `ApplyAppearance()` |
| 新方法 | `CreateClipGeometry()`、`SetBallShape()`、`UpdateClipFromAppearance()` |
| 新方法 | `ShowBallText()`、`HideBallText()`、`ShowPillInfo()`、`CollapseToCircle()` |
| 修改 | `Window_Loaded` 中 `UpdateClipToCircle()` → `UpdateClipFromAppearance()` |
| 修改 | `ClickBall`（~269 行）→ 药丸形态点击行为区分（点内容 vs 点空白） |

### 5.3 无需改动的部分

| 部分 | 原因 |
|---|---|
| Win32 `SetWindowPos` / `GetCursorPos` P/Invoke | 置顶和坐标获取与形状无关 |
| `MakeSuperTopmost()` | 保持不变 |
| 拖拽三件套（`ClickBall`/`OnDragFloatBall`/`LeftClickUp`） | 拖拽逻辑只依赖窗口 Left/Top，药丸形态同样适用 |
| `ShowMenu()` / `CloseMenu()` | 菜单定位策略不变，但药丸形态可能需要调偏移量（可选优化） |
| `OnToolClick()` / `LoadTools()` | 不受影响 |

---

## 六、实现顺序建议

| 阶段 | 内容 | 预计改动量 |
|---|---|---|
| **Phase 1** | 形状系统：`BallShape` 枚举 + `CreateClipGeometry` 工厂 + `SetBallShape` 动画 | ~80 行 |
| **Phase 2** | 颜色解耦：`Ellipse` → `Path` + `BallAppearance` 配置 + DynamicResource 绑定 | ~30 行 XAML + ~20 行 C# |
| **Phase 3** | 内容层：`ContentControl` + `ShowBallText`/`ShowPillInfo`/`CollapseToCircle` | ~80 行 |
| **Phase 4** | 自定义扩展：在设置窗口中加颜色选择器/形状预设切换 | 酌情 |

---

## 七、关键注意事项

1. **`AllowsTransparency=True` 窗口的动画性能**：分层窗口每帧 resize 都会触发系统级重绘。`SetBallShape` 中的 16ms 定时器仅在动画期间运行，动画结束立即停止，不会造成持续开销。

2. **药丸形态的拖拽**：窗口变宽后，拖拽的鼠标捕获和坐标计算保持不变。`ActualWidth` 只会影响边界钳制公式，已经做了动态读取。

3. **Clip 与内容的协调**：圆形形态下，`BallContent` 中的内容要足够小（字号 18-24）才能完整显示在球内。药丸形态下空间充足。

4. **`UpdateClipToCircle` 废弃**：原有方法仅支持固定圆形，改为 `UpdateClipFromAppearance` 统一处理所有形状。

5. **定时器频率 bug**：当前代码注释写"每 60000ms(60s)"但实际 `Interval = TimeSpan.FromMilliseconds(500)`（0.5 秒）。建议 Phase 1 顺便改为 60s 或注释更正。
