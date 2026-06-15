using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
/// 字体粗细，映射到 Avalonia FontWeight（100~900）。
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

    public static implicit operator SlideCornerRadius(double uniformRadius)
        => new()
        {
            TopLeft = uniformRadius,
            TopRight = uniformRadius,
            BottomRight = uniformRadius,
            BottomLeft = uniformRadius,
        };

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
    Absolute,
    Horizontal,
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
/// 画刷抽象基类，用于统一纯色填充和渐变填充。
/// </summary>
internal abstract class SlideBrush
{
}

/// <summary>
/// 纯色画刷，对应 <c>Fill="#FF0000"</c> 属性字符串。
/// </summary>
internal sealed class SlideSolidColorBrush : SlideBrush
{
    public required string Color { get; init; }
}

/// <summary>
/// 渐变停止点（0~1 位置 + 颜色）。
/// </summary>
internal sealed class SlideGradientStop
{
    public double Offset { get; init; }

    public required string Color { get; init; }
}

/// <summary>
/// 线性渐变画刷，起止点坐标范围为 0~1（相对于元素边界）。
/// </summary>
internal sealed class SlideLinearGradientBrush : SlideBrush
{
    public double X1 { get; init; }
    public double Y1 { get; init; }
    public double X2 { get; init; } = 1;
    public double Y2 { get; init; }

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

    public SlideThickness? Margin { get; init; }

    public Rect LocalBounds { get; set; }

    public Rect LayoutBounds { get; set; }

    public double ActualWidth { get; set; }

    public double ActualHeight { get; set; }
}

internal sealed class SlidePage
{
    public string Background { get; init; } = "#FFFFFF";

    public List<SlideElement> Children { get; } = new();

    public IReadOnlyList<SlideTextStyle>? Styles { get; init; }

    public Rect LayoutBounds { get; set; } = new(0, 0, SlideRenderer.CanvasWidth, SlideRenderer.CanvasHeight);
}

internal sealed class SlidePanelElement : SlideElement
{
    public double Padding { get; init; }

    /// <summary>
    /// 背景画刷，支持纯色或渐变。
    /// </summary>
    public SlideBrush? BackgroundBrush { get; init; }

    public SlideLayoutDirection Layout { get; init; }

    public double Gap { get; init; }

    public List<SlideElement> Children { get; } = new();
}

internal sealed class SlideRectElement : SlideElement
{
    /// <summary>
    /// 填充画刷，支持纯色（<see cref="SlideSolidColorBrush"/>）或渐变（<see cref="SlideLinearGradientBrush"/>）。
    /// </summary>
    public SlideBrush? FillBrush { get; init; }

    /// <summary>
    /// 描边画刷，支持纯色或渐变。
    /// </summary>
    public SlideBrush? StrokeBrush { get; init; }

    public double StrokeThickness { get; init; }

    public SlideCornerRadius? CornerRadius { get; init; }

    /// <summary>
    /// 元素阴影效果。
    /// </summary>
    public SlideShadow? Shadow { get; init; }

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

    public SlideFontWeight? FontWeight { get; init; }

    public IReadOnlyList<SlideSpan>? Spans { get; init; }

    public string? Style { get; init; }

    public TextLayout? TextLayout { get; set; }
}

internal sealed class SlideImageElement : SlideElement
{
    public required string Source { get; init; }

    public SlideImageStretch Stretch { get; init; } = SlideImageStretch.Uniform;

    public Bitmap? Bitmap { get; set; }
}
