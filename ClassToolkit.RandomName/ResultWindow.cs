using ClassToolkit.Core.Controls;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClassToolkit.RandomName;

/// <summary>
/// 点名结果展示窗口。宽度自适应最长名字，高度有上限，超出滚动。
/// 标题栏复用 Core/Styles/TitleBar.xaml 统一样式，自动适配浅色/深色主题。
/// </summary>
public class ResultWindow : CustomWindow
{
    private const double ITEM_HEIGHT = 42;
    private const int MAX_VISIBLE_ITEMS = 12;
    private const double HORIZONTAL_PADDING = 24;
    private const double VERTICAL_PADDING = 12;
    private const double INDEX_COLUMN_WIDTH = 40;
    private const double MIN_WIDTH = 240;
    private const double MAX_WIDTH = 560;
    private const double FONT_SIZE = 32;

    public ResultWindow(List<string> items, string title, SolidColorBrush accentBrush)
    {
        Owner = Application.Current.MainWindow;
        Title = title;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanMinimize;
        MinWidth = MIN_WIDTH;
        MinHeight = 160;

        var contentBg = (Brush)Application.Current.FindResource("ContentBackground");
        var textFg = (Brush)Application.Current.FindResource("TextPrimary");
        var separatorBrush = (Brush)Application.Current.FindResource("SeparatorColor");
        var controlBorder = (Brush)Application.Current.FindResource("ControlBorder");

        Background = contentBg;

        // ── 标题栏（复用 Core 统一 TitleBar 控件）──
        var titleBar = new TitleBar
        {
            Title = $"{title} · {items.Count} 人"
        };

        // ── 计算窗口尺寸 ──
        double textWidth = MeasureLongestText(items);
        double contentWidth = INDEX_COLUMN_WIDTH + textWidth + HORIZONTAL_PADDING * 2;
        contentWidth = Math.Max(MIN_WIDTH, Math.Min(MAX_WIDTH, contentWidth));

        int visibleRows = Math.Min(items.Count, MAX_VISIBLE_ITEMS);
        double totalHeight = titleBar.Height + visibleRows * ITEM_HEIGHT + VERTICAL_PADDING + 52; // 52 = footer

        Width = contentWidth;
        Height = totalHeight;
        if (items.Count > MAX_VISIBLE_ITEMS)
            Width = contentWidth + 18; // 滚动条宽度

        // ── 根布局 ──
        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // 标题栏
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 内容
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // 底部按钮

        Grid.SetRow(titleBar, 0);

        // ── 内容滚动区 ──
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(0)
        };
        Grid.SetRow(scrollViewer, 1);

        var itemsPanel = new VirtualizingStackPanel
        {
            Margin = new Thickness(0, VERTICAL_PADDING / 2, 0, VERTICAL_PADDING / 2)
        };

        for (int i = 0; i < items.Count; i++)
        {
            itemsPanel.Children.Add(BuildItemRow(
                index: i + 1,
                text: items[i],
                isLast: i == items.Count - 1,
                accentBrush,
                textFg,
                separatorBrush));
        }

        scrollViewer.Content = itemsPanel;

        // ── 底部关闭按钮 ──
        var footerBorder = new Border
        {
            Background = contentBg,
            BorderBrush = separatorBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(0, 8, 0, 12),
            Child = new Button
            {
                Content = "关闭",
                Width = 90,
                Height = 30,
                FontFamily = new FontFamily("Microsoft YaHei"),
                FontSize = 13,
                Foreground = textFg,
                Background = (Brush)Application.Current.FindResource("ControlBackground"),
                BorderBrush = controlBorder,
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        var footerBtn = (Button)footerBorder.Child;
        footerBtn.Click += (_, _) => Close();
        Grid.SetRow(footerBorder, 2);

        rootGrid.Children.Add(titleBar);
        rootGrid.Children.Add(scrollViewer);
        rootGrid.Children.Add(footerBorder);
        Content = rootGrid;
    }

    /// <summary>
    /// 构建单行：圆角序号徽标 + 名字/学号，底部分隔线
    /// </summary>
    private static Border BuildItemRow(
        int index, string text, bool isLast,
        Brush accentBrush, Brush textFg, Brush separatorBrush)
    {
        var rowGrid = new Grid
        {
            Height = ITEM_HEIGHT,
            Margin = new Thickness(HORIZONTAL_PADDING, 0, HORIZONTAL_PADDING, 0)
        };
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(INDEX_COLUMN_WIDTH) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // 序号 — 圆角小徽标（accent 色背景 + 白字）
        var indexBorder = new Border
        {
            Width = 26,
            Height = 26,
            CornerRadius = new CornerRadius(13),
            Background = accentBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = index.ToString(),
                FontFamily = new FontFamily("Microsoft YaHei"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        Grid.SetColumn(indexBorder, 0);

        // 名字 / 学号
        var nameBlock = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Microsoft YaHei"),
            FontSize = FONT_SIZE,
            Foreground = textFg,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(nameBlock, 1);

        rowGrid.Children.Add(indexBorder);
        rowGrid.Children.Add(nameBlock);

        // 分隔线（最后一行不加）
        var border = new Border { Child = rowGrid };
        if (!isLast)
        {
            border.BorderBrush = separatorBrush;
            border.BorderThickness = new Thickness(0, 0, 0, 1);
        }

        return border;
    }

    /// <summary>
    /// 用 FormattedText 测量最长字符串的渲染宽度
    /// </summary>
    private static double MeasureLongestText(List<string> items)
    {
        if (items.Count == 0) return 120;

        var typeface = new Typeface("Microsoft YaHei");
        double maxWidth = 0;

        foreach (var item in items)
        {
            var ft = new FormattedText(
                item,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                FONT_SIZE,
                Brushes.Black,
                VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

            if (ft.Width > maxWidth)
                maxWidth = ft.Width;
        }

        return Math.Ceiling(maxWidth);
    }
}
