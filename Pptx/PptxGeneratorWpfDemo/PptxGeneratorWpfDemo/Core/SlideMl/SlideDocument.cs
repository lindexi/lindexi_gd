using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace PptxGenerator;

internal enum SlideHorizontalAlignment
{
    Left,
    Center,
    Right,
}

internal enum SlideVerticalAlignment
{
    Top,
    Center,
    Bottom,
}

internal enum SlideTextAlignment
{
    Left,
    Center,
    Right,
    Justify,
}

internal enum SlideImageStretch
{
    None,
    Fill,
    Uniform,
    UniformToFill,
}

/// <summary>
/// 字体粗细，映射到 WPF FontWeight（100~900）。
/// </summary>
internal enum SlideFontWeight
{
    Thin = 100,
    ExtraLight = 200,
    Light = 300,
    Normal = 400,
    Medium = 500,
    SemiBold = 600,
    Bold = 700,
    ExtraBold = 800,
    Black = 900,
}

/// <summary>
/// 四角独立圆角值。
/// </summary>
public readonly record struct SlideCornerRadius
{
    public double TopLeft { get; init; }
    public double TopRight { get; init; }
    public double BottomRight { get; init; }
    public double BottomLeft { get; init; }

    /// <summary>
    /// 从单个值隐式转换（四角统一）。
    /// </summary>
    public static implicit operator SlideCornerRadius(double uniformRadius)
        => new()
        {
            TopLeft = uniformRadius,
            TopRight = uniformRadius,
            BottomRight = uniformRadius,
            BottomLeft = uniformRadius,
        };

    /// <summary>
    /// 从逗号分隔的字符串解析，如 "8,16,8,16"。
    /// 支持 1~4 个值，按 CSS border-radius 简写规则展开。
    /// </summary>
    public static SlideCornerRadius? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        var values = new double[4];
        for (var i = 0; i < parts.Length && i < 4; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            {
                return null;
            }

            values[i] = v;
        }

        return parts.Length switch
        {
            1 => new SlideCornerRadius { TopLeft = values[0], TopRight = values[0], BottomRight = values[0], BottomLeft = values[0] },
            2 => new SlideCornerRadius { TopLeft = values[0], TopRight = values[1], BottomRight = values[0], BottomLeft = values[1] },
            3 => new SlideCornerRadius { TopLeft = values[0], TopRight = values[1], BottomRight = values[2], BottomLeft = values[1] },
            _ => new SlideCornerRadius { TopLeft = values[0], TopRight = values[1], BottomRight = values[2], BottomLeft = values[3] },
        };
    }
}

/// <summary>
/// Panel 子元素排列方向。
/// </summary>
internal enum SlideLayoutDirection
{
    /// <summary>绝对定位（默认行为）。</summary>
    Absolute,
    /// <summary>水平排列，子元素沿 X 轴依次排布。</summary>
    Horizontal,
    /// <summary>垂直排列，子元素沿 Y 轴依次排布。</summary>
    Vertical,
}

/// <summary>
/// 四边间距值，用于 Margin 属性。
/// </summary>
public readonly record struct SlideThickness
{
    public double Left { get; init; }
    public double Top { get; init; }
    public double Right { get; init; }
    public double Bottom { get; init; }

    /// <summary>
    /// 从逗号分隔的字符串解析，如 "0,0,0,8"。
    /// 支持 1~4 个值，按 CSS margin 简写规则展开。
    /// </summary>
    public static SlideThickness? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Split(',', StringSplitOptions.TrimEntries);
        var values = new double[4];
        for (var i = 0; i < parts.Length && i < 4; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            {
                return null;
            }

            values[i] = v;
        }

        return parts.Length switch
        {
            1 => new SlideThickness { Left = values[0], Top = values[0], Right = values[0], Bottom = values[0] },
            2 => new SlideThickness { Left = values[1], Top = values[0], Right = values[1], Bottom = values[0] },
            3 => new SlideThickness { Left = values[1], Top = values[0], Right = values[1], Bottom = values[2] },
            _ => new SlideThickness { Left = values[0], Top = values[1], Right = values[2], Bottom = values[3] },
        };
    }
}

/// <summary>
/// 渐变停止点（0~1 位置 + 颜色）。
/// </summary>
internal sealed class SlideGradientStop
{
    /// <summary>
    /// 渐变位置（0~1）。
    /// </summary>
    public double Offset { get; init; }

    /// <summary>
    /// 颜色字符串（如 "#FF0000"）。
    /// </summary>
    public required string Color { get; init; }
}

/// <summary>
/// 线性渐变画刷，起止点坐标范围为 0~1（相对于元素边界）。
/// </summary>
internal sealed class SlideLinearGradientBrush
{
    public double X1 { get; init; }
    public double Y1 { get; init; }
    public double X2 { get; init; } = 1;
    public double Y2 { get; init; }

    /// <summary>
    /// 渐变停止点列表。
    /// </summary>
    public required IReadOnlyList<SlideGradientStop> Stops { get; init; }
}

/// <summary>
/// 元素阴影效果。
/// </summary>
internal sealed class SlideShadow
{
    public double OffsetX { get; set; }
    public double OffsetY { get; set; } = 4;
    public double Blur { get; set; } = 12;
    public string Color { get; set; } = "#00000033";
    public double Opacity { get; set; } = 1;

    /// <summary>
    /// 从属性字符串解析，如 "0 4 12 #00000033"。
    /// </summary>
    public static SlideShadow? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        var shadow = new SlideShadow();
        if (parts.Length > 0 && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var ox))
        {
            shadow.OffsetX = ox;
        }

        if (parts.Length > 1 && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var oy))
        {
            shadow.OffsetY = oy;
        }

        if (parts.Length > 2 && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var blur))
        {
            shadow.Blur = blur;
        }

        if (parts.Length > 3)
        {
            shadow.Color = parts[3];
        }

        return shadow;
    }
}

/// <summary>
/// 富文本内联片段，用于 <c>Span</c> 子元素。
/// </summary>
internal sealed class SlideSpan
{
    /// <summary>
    /// 片段文本内容。
    /// </summary>
    public required string Text { get; init; }

    public double? FontSize { get; init; }
    public string? FontName { get; init; }
    public string? Foreground { get; init; }
    public SlideFontWeight? FontWeight { get; init; }
    public string? FontStyle { get; init; }
    public string? TextDecoration { get; init; }
}

/// <summary>
/// 文本样式定义，用于 <c>Page.Styles</c> 中的 <c>TextStyle</c> 元素。
/// </summary>
internal sealed class SlideTextStyle
{
    /// <summary>
    /// 样式标识符，供 <c>Style</c> 属性引用。
    /// </summary>
    public required string Id { get; init; }

    public double? FontSize { get; init; }
    public SlideFontWeight? FontWeight { get; init; }
    public string? Foreground { get; init; }
    public string? FontName { get; init; }
    public double? LineHeight { get; init; }
    public SlideTextAlignment? TextAlignment { get; init; }
}

internal abstract class SlideElement
{
    public required string Id { get; init; }

    public double? X { get; init; }

    public double? Y { get; init; }

    public double? Width { get; init; }

    public double? Height { get; init; }

    public SlideHorizontalAlignment? HorizontalAlignment { get; init; }

    public SlideVerticalAlignment? VerticalAlignment { get; init; }

    public double Opacity { get; init; } = 1;

    /// <summary>
    /// 元素外边距。在流式布局中影响元素间距，在绝对定位中影响坐标偏移。
    /// </summary>
    public SlideThickness? Margin { get; init; }

    public Rect LocalBounds { get; set; }

    public Rect LayoutBounds { get; set; }

    public double ActualWidth { get; set; }

    public double ActualHeight { get; set; }
}

internal sealed class SlidePage
{
    public string Background { get; init; } = "#FFFFFF";

    public List<SlideElement> Children { get; } = [];

    /// <summary>
    /// 页面级文本样式定义，供 <c>Style</c> 属性引用。
    /// </summary>
    public IReadOnlyList<SlideTextStyle>? Styles { get; init; }

    public Rect LayoutBounds { get; set; } = new(0, 0, SlidePipelineContext.DefaultCanvasWidth, SlidePipelineContext.DefaultCanvasHeight);
}

internal sealed class SlidePanelElement : SlideElement
{
    public double Padding { get; init; }

    public string? Background { get; init; }

    /// <summary>
    /// 渐变背景（优先于 <see cref="Background"/> 属性）。
    /// </summary>
    public SlideLinearGradientBrush? BackgroundElement { get; init; }

    /// <summary>
    /// 子元素排列方向。默认 <see cref="SlideLayoutDirection.Absolute"/>（绝对定位）。
    /// </summary>
    public SlideLayoutDirection Layout { get; init; }

    /// <summary>
    /// 流式布局下子元素之间的默认间距（px）。
    /// </summary>
    public double Gap { get; init; }

    public List<SlideElement> Children { get; } = [];
}

internal sealed class SlideRectElement : SlideElement
{
    public string? Fill { get; init; }

    public string? Stroke { get; init; }

    public double StrokeThickness { get; init; }

    /// <summary>
    /// 圆角半径。支持单值（统一圆角）或 <see cref="SlideCornerRadius"/>（四角独立）。
    /// </summary>
    public SlideCornerRadius? CornerRadius { get; init; }

    /// <summary>
    /// 渐变填充（优先于 <see cref="Fill"/> 属性）。
    /// </summary>
    public SlideLinearGradientBrush? FillElement { get; init; }

    /// <summary>
    /// 渐变描边（优先于 <see cref="Stroke"/> 属性）。
    /// </summary>
    public SlideLinearGradientBrush? StrokeElement { get; init; }

    /// <summary>
    /// 元素阴影效果。
    /// </summary>
    public SlideShadow? Shadow { get; init; }

    /// <summary>
    /// 阴影的原始字符串表示，用于 XML 回填。
    /// </summary>
    public string? ShadowString { get; init; }

    /// <summary>
    /// 虚线描边模式（逗号分隔的数值列表）。
    /// </summary>
    public IReadOnlyList<double>? StrokeDashArray { get; init; }
}

internal sealed class SlideTextElement : SlideElement
{
    public required string Text { get; init; }

    public string FontName { get; init; } = "Microsoft YaHei";

    public double FontSize { get; init; } = 16;

    public string Foreground { get; init; } = "#000000";

    public SlideTextAlignment TextAlignment { get; init; } = SlideTextAlignment.Left;

    public double LineHeight { get; init; } = 1.2;

    public int ActualLineCount { get; set; }

    /// <summary>
    /// 字体粗细。
    /// </summary>
    public SlideFontWeight? FontWeight { get; init; }

    /// <summary>
    /// 富文本内联片段。非空时优先于 <see cref="Text"/> 渲染。
    /// </summary>
    public IReadOnlyList<SlideSpan>? Spans { get; init; }

    /// <summary>
    /// 引用的 TextStyle 标识符。
    /// </summary>
    public string? Style { get; init; }
}

internal sealed class SlideImageElement : SlideElement
{
    public required string Source { get; init; }

    public SlideImageStretch Stretch { get; init; } = SlideImageStretch.Uniform;
}
