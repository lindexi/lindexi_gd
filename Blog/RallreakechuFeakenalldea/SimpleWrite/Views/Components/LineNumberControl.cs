using System;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Primitive;

namespace SimpleWrite.Views.Components;

/// <summary>
/// 行号控件，负责在编辑器左侧绘制行号
/// </summary>
public class LineNumberControl : Control
{
    private TextEditor? _textEditor;

    /// <summary>
    /// 行号区域宽度
    /// </summary>
    public static readonly StyledProperty<double> LineNumberWidthProperty =
        AvaloniaProperty.Register<LineNumberControl, double>(nameof(LineNumberWidth), 48);

    /// <summary>
    /// 行号区域宽度
    /// </summary>
    public double LineNumberWidth
    {
        get => GetValue(LineNumberWidthProperty);
        set => SetValue(LineNumberWidthProperty, value);
    }

    /// <summary>
    /// 续行符号，null 表示不显示
    /// </summary>
    public static readonly StyledProperty<string?> ContinuationSymbolProperty =
        AvaloniaProperty.Register<LineNumberControl, string?>(nameof(ContinuationSymbol), "·");

    /// <summary>
    /// 续行符号，null 表示不显示
    /// </summary>
    public string? ContinuationSymbol
    {
        get => GetValue(ContinuationSymbolProperty);
        set => SetValue(ContinuationSymbolProperty, value);
    }

    /// <summary>
    /// 行号字体大小，默认跟随文本
    /// </summary>
    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<LineNumberControl, double>(nameof(FontSize), 14);

    /// <summary>
    /// 行号字体大小
    /// </summary>
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// 行号颜色
    /// </summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<LineNumberControl, IBrush?>(nameof(Foreground));

    /// <summary>
    /// 行号颜色
    /// </summary>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// 行号背景色
    /// </summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<LineNumberControl, IBrush?>(nameof(Background));

    /// <summary>
    /// 行号背景色
    /// </summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>
    /// 所属的 ScrollViewer，用于滚动时触发行号重绘。
    /// 在 XAML 中通过绑定注入，例如 ScrollViewer="{Binding #MyScrollViewer}"
    /// </summary>
    public static readonly StyledProperty<ScrollViewer?> ScrollViewerProperty =
        AvaloniaProperty.Register<LineNumberControl, ScrollViewer?>(nameof(ScrollViewer));

    /// <summary>
    /// 所属的 ScrollViewer
    /// </summary>
    public ScrollViewer? ScrollViewer
    {
        get => GetValue(ScrollViewerProperty);
        set => SetValue(ScrollViewerProperty, value);
    }

    static LineNumberControl()
    {
        AffectsRender<LineNumberControl>(
            LineNumberWidthProperty, ContinuationSymbolProperty,
            FontSizeProperty, ForegroundProperty, BackgroundProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ScrollViewerProperty)
        {
            if (change.OldValue is ScrollViewer oldScrollViewer)
            {
                oldScrollViewer.ScrollChanged -= OnScrollChanged;
            }

            if (change.NewValue is ScrollViewer newScrollViewer)
            {
                newScrollViewer.ScrollChanged += OnScrollChanged;
            }

            InvalidateVisual();
        }
    }

    /// <summary>
    /// 关联到文本编辑器
    /// </summary>
    /// <param name="textEditor"></param>
    internal void Attach(TextEditor textEditor)
    {
        _textEditor = textEditor;
        textEditor.LayoutCompleted += OnLayoutCompleted;
        Dispatcher.UIThread.Post(InvalidateVisual);
    }

    /// <summary>
    /// 解除与文本编辑器的关联
    /// </summary>
    internal void Detach()
    {
        if (_textEditor is not null)
        {
            _textEditor.LayoutCompleted -= OnLayoutCompleted;
            _textEditor = null;
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // 滚动时需要重绘行号，因为行号只绘制可见范围，
        // 滚动到新区域时必须重新绘制才能显示新的行号
        InvalidateVisual();
    }

    private void OnLayoutCompleted(object? sender, EventArgs e)
    {
        // LayoutCompleted 可能在渲染过程中触发（如 ForceLayout → EnsureLayoutUpdated → LayoutCompleted），
        // 此时直接调用 InvalidateVisual 会导致 Avalonia 抛出
        // "Visual was invalidated during the render pass" 异常。
        // 使用 Post 延迟到当前渲染帧结束后再重绘。
        Dispatcher.UIThread.Post(InvalidateVisual);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(LineNumberWidth, 0);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // 1. 画背景
        if (Background is { } background)
        {
            context.FillRectangle(background, new Rect(0, 0, Bounds.Width, Bounds.Height));
        }

        if (_textEditor is null)
        {
            return;
        }

        // 2. 获取布局信息
        if (!_textEditor.TextEditorCore.TryGetRenderInfo(out RenderInfoProvider? renderInfo))
        {
            // 文本是脏的，等 LayoutCompleted 后自动重绘
            return;
        }

        // 3. 计算可见范围
        double viewportTop = 0;
        double viewportBottom = double.MaxValue;

        if (ScrollViewer is { } scrollViewer)
        {
            viewportTop = scrollViewer.Offset.Y;
            viewportBottom = scrollViewer.Offset.Y + scrollViewer.Viewport.Height;
        }

        var foreground = Foreground ?? Brushes.Gray;
        var typeface = new Typeface(FontFamily.Default);
        var culture = CultureInfo.CurrentUICulture;
        double rightEdge = LineNumberWidth - 8;

        // 4. 遍历段落
        var paragraphList = renderInfo.GetParagraphRenderInfoList();
        for (int i = 0; i < paragraphList.Count; i++)
        {
            var paragraph = paragraphList[i];
            var lineList = paragraph.GetLineRenderInfoList();

            foreach (var line in lineList)
            {
                TextRect outlineBounds = line.OutlineBounds;

                // 跳过可见区域上方的行
                if (outlineBounds.Bottom < viewportTop)
                {
                    continue;
                }

                // 超出可见区域下方，终止遍历
                if (outlineBounds.Top > viewportBottom)
                {
                    return;
                }

                double centerY = outlineBounds.Y + outlineBounds.Height / 2;

                string text;
                if (line.IsFirstLine)
                {
                    // 行号 = 段落序号 + 1
                    text = (line.ParagraphIndex.Index + 1).ToString();
                }
                else
                {
                    // 续行符号
                    text = ContinuationSymbol ?? "";
                }

                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                var formattedText = new FormattedText(
                    text,
                    culture,
                    Avalonia.Media.FlowDirection.LeftToRight,
                    typeface,
                    FontSize,
                    foreground);

                double textWidth = formattedText.Width;
                double textHeight = formattedText.Height;
                double x = rightEdge - textWidth;
                double y = centerY - textHeight / 2;

                context.DrawText(formattedText, new Point(x, y));
            }
        }
    }
}
