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
