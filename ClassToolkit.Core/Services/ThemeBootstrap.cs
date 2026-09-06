using ClassToolkit.Core.Utilities;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace ClassToolkit.Core.Services;

/// <summary>
/// 各进程统一的主题引导：启动时读配置应用主题，并监听系统深浅色切换。
/// 同时监听 config.json 变化——在设置里修改主题/主题色后，
/// 所有正在运行的进程实时重新应用，无需重启。
/// 每个 App 的 OnStartup 调用一次 <see cref="Initialize"/> 即可。
/// </summary>
public static class ThemeBootstrap
{
    /// <summary>主题应用完成后触发（含系统深浅色切换、config 变化后的重新应用），供窗口做手动刷新。</summary>
    public static event Action? ThemeApplied;

    private static FileSystemWatcher? _configWatcher;
    private static DateTime _lastConfigApplyUtc = DateTime.MinValue;

    /// <summary>启动时调用：读 config → 应用主题（含用户自定义主题色）→ 挂系统/配置文件监听。</summary>
    public static void Initialize()
    {
        ApplyCurrent();
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
        StartConfigWatcher();
    }

    /// <summary>
    /// 系统深浅色设置变化时重新应用主题。
    /// 通过 Dispatcher 回到 UI 线程，保证资源替换安全。
    /// </summary>
    private static void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General) return;
        Application.Current?.Dispatcher.Invoke(ApplyCurrent);
    }

    /// <summary>
    /// 监听 config.json：设置里保存主题/主题色后，所有进程（含常驻的悬浮球）实时生效。
    /// </summary>
    private static void StartConfigWatcher()
    {
        string configPath = DataPathHelper.GetDataPath("config/config.json");
        string? dir = Path.GetDirectoryName(configPath);
        if (dir == null || !Directory.Exists(dir)) return;

        _configWatcher = new FileSystemWatcher(dir, Path.GetFileName(configPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };
        _configWatcher.Changed += OnConfigFileChanged;
        _configWatcher.Created += OnConfigFileChanged;
    }

    private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ReapplyFromConfig));
        }
        catch
        {
            // 后台文件监听线程不允许异常逃逸，否则可能拖垮应用
        }
    }

    /// <summary>
    /// 防抖后重新应用：FileSystemWatcher 对一次完整写入可能触发多次事件，
    /// 且文件写入非原子（先截断再写），跳过写一半的瞬间。
    /// </summary>
    private static void ReapplyFromConfig()
    {
        try
        {
            var now = DateTime.UtcNow;
            if ((now - _lastConfigApplyUtc).TotalMilliseconds < 500) return;
            _lastConfigApplyUtc = now;
            ApplyCurrent();
        }
        catch
        {
            // 配置被写坏/读失败时静默跳过，等下一次文件事件再重试
        }
    }

    /// <summary>
    /// 读取配置并应用主题。
    /// config 中的 AccentColor 为用户自定义主题色（可空）：
    /// 有值则作为全局强调色覆盖（按钮/选中项/标题栏等），对所有窗口生效。
    /// </summary>
    private static void ApplyCurrent()
    {
        var config = new ConfigService().Load();
        string theme = config["Theme"]?.GetValue<string>() ?? "跟随系统";

        Dictionary<string, Color>? overrides = null;
        string? accent = config["AccentColor"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(accent) && ColorConverter.ConvertFromString(accent) is Color c)
            overrides = ThemeService.BuildAccentOverrides(theme, c);

        ThemeService.Apply(theme, overrides);
        ThemeApplied?.Invoke();
    }
}
