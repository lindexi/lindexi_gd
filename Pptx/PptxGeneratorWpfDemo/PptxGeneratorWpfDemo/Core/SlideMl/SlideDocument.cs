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

    public Rect LayoutBounds { get; set; } = new(0, 0, SlidePipelineContext.DefaultCanvasWidth, SlidePipelineContext.DefaultCanvasHeight);
}

internal sealed class SlidePanelElement : SlideElement
{
    public double Padding { get; init; }

    public string? Background { get; init; }

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

    public double CornerRadius { get; init; }
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
}

internal sealed class SlideImageElement : SlideElement
{
    public required string Source { get; init; }

    public SlideImageStretch Stretch { get; init; } = SlideImageStretch.Uniform;
}
