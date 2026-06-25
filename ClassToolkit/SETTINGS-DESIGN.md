# ClassToolkit Settings 模块设计文档

## 架构概览

```
ClassToolkit.Settings/
├── Services/
│   └── ConfigService.cs            # 配置读写服务（JsonObject 字典 ↔ JSON 文件）
├── MainWindow.xaml                 # UI 层（分类栏 + 内容页 + 应用按钮）
└── MainWindow.xaml.cs              # UI 逻辑层（加载/保存/分类切换）

ClassToolkit.Core/
└── Utilities/
    └── DataPathHelper.cs           # 跨项目共享：解析 data/ 目录路径
```

**没有 Model 层**。配置在内存中以 `System.Text.Json.Nodes.JsonObject` 字典形式存在，等价于 Python 的 `dict`。添加新设置项不需要修改任何类定义。

### 数据流

```
启动 / 点击"应用"热重载时:
  config.json ──→ JsonNode.Parse() ──→ JsonObject 字典 ──→ LoadSettings() ──→ 填入 UI

点击"应用"保存时:
  UI 控件当前值 ──→ SaveSettings() ──→ 写入 JsonObject 字典 ──→ ConfigService.Save() ──→ config.json
```

### 运行时路径解析（DataPathHelper）

| 环境 | BaseDirectory | 向上找到 `.git`？ | 读写位置 |
|---|---|---|---|
| Debug（IDE 启动） | `bin/Debug/net10.0-windows/` | ✅ | 方案根 `data/config/config.json` |
| Release（打包发布） | 发布目录 | ❌ | exe 旁 `data/config/config.json` |

---

## 核心设计：字典式配置

整个配置就是一个 `JsonObject`（键值对集合）。读一个值：

```csharp
_config.Load()                         // → JsonObject
       ["BallSize"]                     // → JsonNode?
       ?.GetValue<int>()                // → int?
       ?? 60;                           // → int (缺省值 60)
```

写一个值：

```csharp
config["BallSize"] = 85;
_config.Save(config);
```

**所有类型转换由 `GetValue<T>()` 自动处理，无需手写解析逻辑。**

---

## 预留接口清单

### 接口 1：`ConfigService` — 读写配置字典

**文件**：`Services/ConfigService.cs`

| 方法 | 签名 | 作用 |
|---|---|---|
| `Load()` | `→ JsonObject` | 读 JSON 文件 → 字典。文件不存在/损坏时返回空 `JsonObject()` |
| `Save(JsonObject)` | `→ void` | 字典序列化 → 写回 JSON。目录不存在时自动创建 |

**此文件添加新配置项时无需修改。**

---

### 接口 2：`LoadSettings()` — 字典 → UI 控件

**文件**：`MainWindow.xaml.cs` 第 42–62 行

**作用**：启动/热重载时，用 `GetStr` / `GetBool` / `GetInt` 从字典取值填入 UI 控件。

**辅助方法**（第 26–32 行）：

| 方法 | 用法 | 缺省行为 |
|---|---|---|
| `GetStr(obj, key, fallback)` | `GetStr(config, "Language", "简体中文")` | key 不存在→返回 fallback |
| `GetBool(obj, key, fallback)` | `GetBool(config, "AutoStart")` | key 不存在→返回 false |
| `GetInt(obj, key, fallback)` | `GetInt(config, "BallSize", 60)` | key 不存在→返回 fallback |

**添加新设置项**：在对应分类区域加一行：

```csharp
MySlider.Value = GetInt(_settings, "MyNewKey", 80);
```

---

### 接口 3：`SaveSettings()` — UI 控件 → 字典

**文件**：`MainWindow.xaml.cs` 第 67–85 行

**作用**：点击"应用"时，把每个控件的当前值写入字典对应 key。

**添加新设置项**：在对应分类区域加一行：

```csharp
_settings["MyNewKey"] = (int)MySlider.Value;
```

---

### 接口 4：控件值读写辅助

**文件**：`MainWindow.xaml.cs` 第 117–128 行

| 方法 | 适用控件 | 说明 |
|---|---|---|
| `SetComboBoxByContent(cmb, text)` | `ComboBox` | 按显示文字选中项 |
| `GetComboBoxContent(cmb)` | `ComboBox` | 取当前选中项的文字 |

`TextBox`、`CheckBox`、`Slider` 等直接读 `.Text` / `.IsChecked` / `.Value`，无需辅助。

---

### 接口 5：左侧分类页路由 — 添加新分类

**3 步**：

**Step 1** — XAML 创建内容页（`MainWindow.xaml` 约 178 行起）：

```xml
<Grid x:Name="PageMyNew" Visibility="Collapsed">
    <StackPanel>
        <TextBlock Style="{StaticResource SettingTitleStyle}" Text="新分类"/>
        <!-- 你的控件 -->
    </StackPanel>
</Grid>
```

**Step 2** — XAML 注册分类栏条目（约 153 行）：

```xml
<ListBoxItem Tag="mynew">新分类名</ListBoxItem>
```

**Step 3** — C# 注册路由（约 91 行 `switch` 块）：

```csharp
case "mynew": PageMyNew.Visibility = Visibility.Visible; break;
```

同时在 switch 上方"全关"列表加：

```csharp
PageMyNew.Visibility = Visibility.Collapsed;
```

---

### 接口 6：`DataPathHelper` — 跨项目统一路径

**文件**：`ClassToolkit.Core/Utilities/DataPathHelper.cs`

| 方法 | 返回 | 使用方 |
|---|---|---|
| `DataPathHelper.GetDataPath("tools.json")` | `/方案根/data/tools.json` | 主程序读工具列表 |
| `DataPathHelper.GetDataPath("config/config.json")` | `/方案根/data/config/config.json` | Settings 读写配置 |
| `DataPathHelper.GetDataPath("mydata/names.txt")` | `/方案根/data/mydata/names.txt` | 任何工具读自定义数据 |

**此文件无需修改。** Debug 自动指向方案根目录，Release 指向 exe 旁。

---

### 接口 7：`ApplySettings_Click` — 保存 + 热重载

**文件**：`MainWindow.xaml.cs` 第 130–149 行

点击"应用"按钮 → `SaveSettings()` 写 JSON → `LoadSettings()` 从 JSON 反读回 UI（验证持久化）→ 按钮变灰 2 秒 → 显示 "✓ 设置已保存"。

**此方法无需修改。** 新配置项通过在 `LoadSettings()` 和 `SaveSettings()` 加代码即可参与完整流程。

---

## 扩展示例

### 添加一个"悬浮球透明度"配置

**Step 1** — XAML 加控件（外观页，`TxtMenuFontSize` 之后）：

```xml
<TextBlock Style="{StaticResource SettingLabelStyle}" Text="悬浮球透明度"/>
<TextBlock Style="{StaticResource SettingHintStyle}" Text="悬浮球的不透明程度（20–100）。"/>
<Slider x:Name="SldBallOpacity" Minimum="20" Maximum="100"
        Width="260" HorizontalAlignment="Left"
        IsSnapToTickEnabled="True" TickFrequency="5"
        Margin="0,2,0,12"/>
```

**Step 2** — `LoadSettings()` 外观区域加一行：

```csharp
SldBallOpacity.Value = GetInt(_settings, "BallOpacity", 80);
```

**Step 3** — `SaveSettings()` 外观区域加一行：

```csharp
_settings["BallOpacity"] = (int)SldBallOpacity.Value;
```

**完成。** 启动 → 外观页出现新滑块 → 改值 → 点"应用" → `config.json` 自动出现 `"BallOpacity": 85`。

### 插件添加自有配置

插件不需要修改宿主任何代码。在自己的代码里：

```csharp
var configService = new ConfigService();  // 自动指向同一个 config.json
var config = configService.Load();

// 读自己的 key
string myValue = config["MyPlugin_Key"]?.GetValue<string>() ?? "默认值";

// 写自己的 key
config["MyPlugin_Key"] = "新值";
configService.Save(config);
```

宿主不认识 `MyPlugin_Key`——也不需要认识。字典方案下，key 完全由读写方协商。

---

## 对比：强类型 vs 字典

| | 强类型（旧） | 字典（新） |
|---|---|---|
| 添加配置步骤 | Model + Load + Save + UI = 4 步 | Load + Save + UI = **2 步** |
| 插件自有配置 | 不可行 | 原生支持 |
| 类型安全 | 编译期 | 运行时（GetValue\<T\> + fallback 兜底） |
| 与 Python 版方案 | 不同 | **完全一致** |
| 文件数 | 3（含 Model） | 2（无 Model） |

---

## 文件清单

| 文件 | 职责 | 添加设置时需改否 |
|---|---|---|
| `Services/ConfigService.cs` | JSON ↔ JsonObject | ❌ 不改 |
| `MainWindow.xaml` | 分类栏 + 所有设置页 UI | ✅ 加控件 |
| `MainWindow.xaml.cs` | LoadSettings / SaveSettings / 分类路由 | ✅ 各加一行 |
| `Core/Utilities/DataPathHelper.cs` | 跨项目路径解析 | ❌ 不改 |

---

## 添加控件速查手册

每种控件类型给出完整 XAML 模板 + LoadSettings / SaveSettings 各一行代码。**所有模板已自动适配明暗主题，直接复制即可。**

> 关键规则：XAML 中使用 `{DynamicResource ...}` 引用颜色以随主题切换；C# 中使用 `GetStr`/`GetBool`/`GetInt` 读取并给缺省值。

---

### 1. 文本标签（只读）

纯展示文字，不需要持久化，只加 XAML：

```xml
<TextBlock Style="{StaticResource SettingLabelStyle}" Text="标签文字"/>
<TextBlock Style="{StaticResource SettingHintStyle}" Text="灰色提示说明。"/>
```

| 样式 Key | 用途 | 字号 | 颜色 |
|---|---|---|---|
| `SettingTitleStyle` | 页面标题 | 22pt SemiBold | TextPrimary |
| `SettingLabelStyle` | 设置项标签 | 13.5pt | TextPrimary |
| `SettingHintStyle` | 灰色提示 | 12pt | TextSecondary |

---

### 2. 文本输入框（TextBox）

```xml
<!-- XAML -->
<TextBlock Style="{StaticResource SettingLabelStyle}" Text="我的输入"/>
<TextBlock Style="{StaticResource SettingHintStyle}" Text="提示说明。"/>
<TextBox x:Name="TxtMyInput" Style="{StaticResource SettingTextBoxStyle}"
         Text="默认值" Width="280" HorizontalAlignment="Left"/>
```

```csharp
// LoadSettings
TxtMyInput.Text = GetStr(_settings, "MyInput", "默认值");

// SaveSettings
_settings["MyInput"] = TxtMyInput.Text;
```

| 要点 | 说明 |
|---|---|
| 前缀 `Txt` | 命名规范，和现有控件一致 |
| `Style="{StaticResource SettingTextBoxStyle}"` | **必须加**——获得主题适配的 Background / Foreground / Border / CaretBrush |
| `GetStr` 的第三个参数 | 配置文件里不存在该 key 时的默认值 |
| `Width="280"` | 建议值；短输入可用 `80` |

---

### 3. 复选框（CheckBox）

```xml
<!-- XAML -->
<TextBlock Style="{StaticResource SettingLabelStyle}" Text="功能开关"/>
<TextBlock Style="{StaticResource SettingHintStyle}" Text="启用后将自动执行……"/>
<CheckBox x:Name="ChkMyFeature" Style="{StaticResource SettingCheckBoxStyle}"
          Content="启用我的功能"/>
```

```csharp
// LoadSettings
ChkMyFeature.IsChecked = GetBool(_settings, "MyFeature");

// SaveSettings
_settings["MyFeature"] = ChkMyFeature.IsChecked ?? false;
```

| 要点 | 说明 |
|---|---|
| 前缀 `Chk` | 命名规范 |
| `Style="{StaticResource SettingCheckBoxStyle}"` | 获得主题 Foreground + 字体大小 |
| `GetBool` 缺省 `false` | 不传第三个参数时默认 `false` |

---

### 4. 下拉框（ComboBox）

```xml
<!-- XAML -->
<TextBlock Style="{StaticResource SettingLabelStyle}" Text="选择项"/>
<TextBlock Style="{StaticResource SettingHintStyle}" Text="从下拉列表中选择。"/>
<ComboBox x:Name="CmbMyChoice"
          FontSize="13" FontFamily="Microsoft YaHei UI"
          ItemContainerStyle="{StaticResource ThemeComboBoxItem}"
          Padding="10,5" Width="220" HorizontalAlignment="Left">
    <ComboBoxItem Content="选项一"/>
    <ComboBoxItem Content="选项二"/>
    <ComboBoxItem Content="选项三"/>
</ComboBox>
```

```csharp
// LoadSettings
SetComboBoxByContent(CmbMyChoice, GetStr(_settings, "MyChoice", "选项一"));

// SaveSettings
_settings["MyChoice"] = GetComboBoxContent(CmbMyChoice);
```

| 要点 | 说明 |
|---|---|
| 前缀 `Cmb` | 命名规范 |
| `ItemContainerStyle="{StaticResource ThemeComboBoxItem}"` | **必须加**——下拉项获得主题文字色 + hover 蓝色高亮 |
| **不要**手动加 `Background` / `Foreground` | 全局隐式 ComboBox 样式已覆盖 |
| `Width="220"` | 推荐的 ComboBox 宽度；更宽的内容可用 `280` |
| `SetComboBoxByContent` | 按显示文字匹配项，不用关心索引 |
| `GetComboBoxContent` | 返回选中项的纯文字 |

---

### 5. 滑块（Slider）

```xml
<!-- XAML -->
<TextBlock Style="{StaticResource SettingLabelStyle}" Text="数值调节"/>
<TextBlock Style="{StaticResource SettingHintStyle}" Text="范围 50–200，步长 10。"/>
<Slider x:Name="SldMyValue" Minimum="50" Maximum="200" Value="100"
        Width="260" HorizontalAlignment="Left"
        IsSnapToTickEnabled="True" TickFrequency="10"
        Margin="0,2,0,12"/>
<TextBlock x:Name="TxtMyValueHint" Text="当前: 100"
           FontSize="12" Foreground="{DynamicResource TextSecondary}"
           FontFamily="Microsoft YaHei UI" Margin="0,0,0,16"/>
```

```csharp
// LoadSettings
SldMyValue.Value = GetInt(_settings, "MyValue", 100);
TxtMyValueHint.Text = $"当前: {(int)SldMyValue.Value}";

// SaveSettings
_settings["MyValue"] = (int)SldMyValue.Value;
```

```csharp
// 构造函数里加实时更新提示文字
SldMyValue.ValueChanged += (_, e) =>
    TxtMyValueHint.Text = $"当前: {(int)e.NewValue}";
```

| 要点 | 说明 |
|---|---|
| 前缀 `Sld` | 命名规范 |
| `TextSecondary` | 提示文字用次要色，不喧宾夺主 |
| `ValueChanged` | 拖拽时实时显示当前值 |

---

### 6. 颜色选择器（Color Dot 模式）

复制分隔线颜色选择器的现有模式。适用于用户从预设颜色中选择。

```xml
<!-- XAML -->
<TextBlock Style="{StaticResource SettingLabelStyle}" Text="强调色"/>
<TextBlock Style="{StaticResource SettingHintStyle}" Text="选择一个颜色。"/>
<StackPanel Orientation="Horizontal" Margin="0,2,0,12">
    <Border Width="28" Height="28" CornerRadius="14" Background="#4A7CF7"
            Cursor="Hand" ToolTip="蓝色"
            MouseLeftButtonDown="MyColor_Click" Tag="#4A7CF7"/>
    <Border Width="28" Height="28" CornerRadius="14" Background="#38A169"
            Cursor="Hand" ToolTip="绿色"
            MouseLeftButtonDown="MyColor_Click" Tag="#38A169"/>
    <!-- 更多颜色... -->
</StackPanel>
<!-- 存储：一个隐藏的 TextBox 记录选中的颜色 -->
<TextBox x:Name="TxtMyColor" Visibility="Collapsed"/>
```

```csharp
// LoadSettings
string savedColor = GetStr(_settings, "MyColor", "#4A7CF7");
TxtMyColor.Text = savedColor;
ApplyMyColor(savedColor);

// SaveSettings
_settings["MyColor"] = TxtMyColor.Text;

// 点击事件
private void MyColor_Click(object sender, MouseButtonEventArgs e)
{
    if (sender is Border border && border.Tag is string hex)
    {
        TxtMyColor.Text = hex;
        ApplyMyColor(hex);
    }
}

private void ApplyMyColor(string hex)
{
    var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    brush.Freeze();
    TargetElement.Background = brush;  // 替换为接收颜色的元素
}
```

---

### 7. 添加新分类栏

在已有 4 个分类之外加一个新的侧边栏分类。

**Step 1 — XAML 创建页面**（在 `</Grid>` 结束标签前，即最后一个 `PageXxx` Grid 之后）：

```xml
<!-- ═══ 我的分类 ═══ -->
<Grid x:Name="PageMyCategory" Visibility="Collapsed">
    <StackPanel>
        <TextBlock Style="{StaticResource SettingTitleStyle}" Text="我的分类"/>
        <!-- 这里放你的控件 -->
    </StackPanel>
</Grid>
```

**Step 2 — XAML 注册侧边栏条目**（在 `CategoryList` 的 `<ListBoxItem>` 列表末尾）：

```xml
<ListBoxItem Tag="mycategory">我的分类</ListBoxItem>
```

**Step 3 — C# 注册路由**（在 `OnCategoryChanged` 方法中）：

```csharp
// 在 "全关" 列表中添加
PageMyCategory.Visibility = Visibility.Collapsed;

// 在 switch 中添加
case "mycategory": PageMyCategory.Visibility = Visibility.Visible; break;
```

| 要点 | 说明 |
|---|---|
| `Tag` 值与 `case` 字符串必须一致 | 这是路由 key |
| 新建的 `Grid` 必须在内容区 `<ScrollViewer>` 内部 | 否则不会跟随滚动 |
| `x:Name` 命名规范 | 前缀 `Page` + 驼峰 |

---

## 完整扩展流程（3 步）

以添加一个"启用新功能"复选框为例：

**Step 1 — XAML**：在对应页面 `<StackPanel>` 中添加：

```xml
<TextBlock Style="{StaticResource SettingLabelStyle}" Text="新功能"/>
<TextBlock Style="{StaticResource SettingHintStyle}" Text="开启后将启用实验性功能。"/>
<CheckBox x:Name="ChkNewFeature" Style="{StaticResource SettingCheckBoxStyle}"
          Content="启用新功能"/>
```

**Step 2 — LoadSettings**（在对应分类区域）：

```csharp
ChkNewFeature.IsChecked = GetBool(_settings, "NewFeature");
```

**Step 3 — SaveSettings**（在对应分类区域）：

```csharp
_settings["NewFeature"] = ChkNewFeature.IsChecked ?? false;
```

**完成**。启动 → 对应页面出现新复选框 → 切换 → 点"应用" → `config.json` 自动出现 `"NewFeature": true`。主题自动适配，暗色模式下文字白色、背景深灰。
