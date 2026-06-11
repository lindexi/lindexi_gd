using System.Collections.Generic;
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

    public Rect LocalBounds { get; set; }

    public Rect LayoutBounds { get; set; }

    public double ActualWidth { get; set; }

    public double ActualHeight { get; set; }
}

internal sealed class SlidePage
{
    public string Background { get; init; } = "#FFFFFF";

    public List<SlideElement> Children { get; } = [];

    public Rect LayoutBounds { get; set; } = new(0, 0, SlideRenderContext.DefaultCanvasWidth, SlideRenderContext.DefaultCanvasHeight);
}

internal sealed class SlidePanelElement : SlideElement
{
    public double Padding { get; init; }

    public string? Background { get; init; }

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
