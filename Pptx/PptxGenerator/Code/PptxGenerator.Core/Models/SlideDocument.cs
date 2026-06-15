using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PptxGenerator;

/// <summary>
/// 水平对齐方式。
/// </summary>
public enum SlideHorizontalAlignment
{
    Left,
    Center,
    Right,
}

/// <summary>
/// 垂直对齐方式。
/// </summary>
public enum SlideVerticalAlignment
{
    Top,
    Center,
    Bottom,
}

/// <summary>
/// 文本对齐方式。
/// </summary>
public enum SlideTextAlignment
{
    Left,
    Center,
    Right,
    Justify,
}

/// <summary>
/// 图片拉伸方式。
/// </summary>
public enum SlideImageStretch
{
    None,
    Fill,
    Uniform,
    UniformToFill,
}

/// <summary>
/// 字体粗细，映射到 UI 框架 FontWeight（100~900）。
/// </summary>
public enum SlideFontWeight
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
/// Panel 子元素排列方向。
/// </summary>
public enum SlideLayoutDirection
{
    /// <summary>绝对定位（默认行为）。</summary>
    Absolute,
    /// <summary>水平排列，子元素沿 X 轴依次排布。</summary>
    Horizontal,
    /// <summary>垂直排列，子元素沿 Y 轴依次排布。</summary>
    Vertical,
}

/// <summary>
/// 四角独立圆角值。
/// </summary>
public readonly record struct SlideCornerRadius
{
    /// <summary>
    /// 左上角圆角半径。
    /// </summary>
    public double TopLeft { get; init; }

    /// <summary>
    /// 右上角圆角半径。
    /// </summary>
    public double TopRight { get; init; }

    /// <summary>
    /// 右下角圆角半径。
    /// </summary>
    public double BottomRight { get; init; }

    /// <summary>
    /// 左下角圆角半径。
    /// </summary>
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
/// 四边间距值，用于 Margin 属性。
/// </summary>
public readonly record struct SlideThickness
{
    /// <summary>
    /// 左边距。
    /// </summary>
    public double Left { get; init; }

    /// <summary>
    /// 上边距。
    /// </summary>
    public double Top { get; init; }

    /// <summary>
    /// 右边距。
    /// </summary>
    public double Right { get; init; }

    /// <summary>
    /// 下边距。
    /// </summary>
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
public sealed class SlideGradientStop
{
    /// <summary>
    /// 渐变位置（0~1）。
    /// </summary>
    public double Offset { get; init; }

    /// <summary>
    /// 颜色字符串（如 "#FF0000"）。
    /// </summary>
    public string Color { get; init; } = string.Empty;
}

/// <summary>
/// 线性渐变画刷，起止点坐标范围为 0~1（相对于元素边界）。
/// </summary>
public sealed class SlideLinearGradientBrush
{
    /// <summary>
    /// 渐变起点 X 坐标。
    /// </summary>
    public double X1 { get; init; }

    /// <summary>
    /// 渐变起点 Y 坐标。
    /// </summary>
    public double Y1 { get; init; }

    /// <summary>
    /// 渐变终点 X 坐标。
    /// </summary>
    public double X2 { get; init; } = 1;

    /// <summary>
    /// 渐变终点 Y 坐标。
    /// </summary>
    public double Y2 { get; init; }

    /// <summary>
    /// 渐变停止点列表。
    /// </summary>
    public IReadOnlyList<SlideGradientStop> Stops { get; init; } = Array.Empty<SlideGradientStop>();
}

/// <summary>
/// 元素阴影效果。
/// </summary>
public sealed class SlideShadow
{
    /// <summary>
    /// 阴影水平偏移。
    /// </summary>
    public double OffsetX { get; set; }

    /// <summary>
    /// 阴影垂直偏移。
    /// </summary>
    public double OffsetY { get; set; } = 4;

    /// <summary>
    /// 阴影模糊半径。
    /// </summary>
    public double Blur { get; set; } = 12;

    /// <summary>
    /// 阴影颜色字符串。
    /// </summary>
    public string Color { get; set; } = "#00000033";

    /// <summary>
    /// 阴影不透明度。
    /// </summary>
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
public sealed class SlideSpan
{
    /// <summary>
    /// 片段文本内容。
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// 字体大小。
    /// </summary>
    public double? FontSize { get; init; }

    /// <summary>
    /// 字体名称。
    /// </summary>
    public string? FontName { get; init; }

    /// <summary>
    /// 字体颜色。
    /// </summary>
    public string? Foreground { get; init; }

    /// <summary>
    /// 字体粗细。
    /// </summary>
    public SlideFontWeight? FontWeight { get; init; }

    /// <summary>
    /// 字体样式（如 "Italic"）。
    /// </summary>
    public string? FontStyle { get; init; }

    /// <summary>
    /// 文本装饰（如 "Underline"）。
    /// </summary>
    public string? TextDecoration { get; init; }
}

/// <summary>
/// 文本样式定义，用于 <c>Page.Styles</c> 中的 <c>TextStyle</c> 元素。
/// </summary>
public sealed class SlideTextStyle
{
    /// <summary>
    /// 样式标识符，供 <c>Style</c> 属性引用。
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 字体大小。
    /// </summary>
    public double? FontSize { get; init; }

    /// <summary>
    /// 字体粗细。
    /// </summary>
    public SlideFontWeight? FontWeight { get; init; }

    /// <summary>
    /// 字体颜色。
    /// </summary>
    public string? Foreground { get; init; }

    /// <summary>
    /// 字体名称。
    /// </summary>
    public string? FontName { get; init; }

    /// <summary>
    /// 行高。
    /// </summary>
    public double? LineHeight { get; init; }

    /// <summary>
    /// 文本对齐。
    /// </summary>
    public SlideTextAlignment? TextAlignment { get; init; }
}

/// <summary>
/// SlideML 元素的抽象基类，包含所有元素的公共属性。
/// UI 框架（WPF/Avalonia）相关的缓存属性由渲染引擎自行维护。
/// </summary>
public abstract class SlideElement
{
    /// <summary>
    /// 元素唯一标识符。
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 元素水平位置。
    /// </summary>
    public double? X { get; init; }

    /// <summary>
    /// 元素垂直位置。
    /// </summary>
    public double? Y { get; init; }

    /// <summary>
    /// 元素宽度。
    /// </summary>
    public double? Width { get; init; }

    /// <summary>
    /// 元素高度。
    /// </summary>
    public double? Height { get; init; }

    /// <summary>
    /// 水平对齐方式。
    /// </summary>
    public SlideHorizontalAlignment? HorizontalAlignment { get; init; }

    /// <summary>
    /// 垂直对齐方式。
    /// </summary>
    public SlideVerticalAlignment? VerticalAlignment { get; init; }

    /// <summary>
    /// 元素透明度（0~1）。
    /// </summary>
    public double Opacity { get; init; } = 1;

    /// <summary>
    /// 元素外边距。在流式布局中影响元素间距，在绝对定位中影响坐标偏移。
    /// </summary>
    public SlideThickness? Margin { get; init; }

    /// <summary>
    /// 元素在自身坐标系中的局部边界。
    /// </summary>
    public SlideRect LocalBounds { get; set; }

    /// <summary>
    /// 元素在父容器中的布局边界。
    /// </summary>
    public SlideRect LayoutBounds { get; set; }

    /// <summary>
    /// 元素实际渲染宽度。
    /// </summary>
    public double ActualWidth { get; set; }

    /// <summary>
    /// 元素实际渲染高度。
    /// </summary>
    public double ActualHeight { get; set; }
}

/// <summary>
/// SlideML 页面根元素。
/// </summary>
public sealed class SlidePage
{
    /// <summary>
    /// 页面背景颜色字符串。
    /// </summary>
    public string Background { get; init; } = "#FFFFFF";

    /// <summary>
    /// 页面子元素列表。
    /// </summary>
    public List<SlideElement> Children { get; } = [];

    /// <summary>
    /// 页面级文本样式定义，供 <c>Style</c> 属性引用。
    /// </summary>
    public IReadOnlyList<SlideTextStyle>? Styles { get; init; }

    /// <summary>
    /// 页面布局边界。
    /// </summary>
    public SlideRect LayoutBounds { get; set; } = new(0, 0, SlidePipelineContext.DefaultCanvasWidth, SlidePipelineContext.DefaultCanvasHeight);
}

/// <summary>
/// Panel 容器元素，可包含子元素。
/// </summary>
public sealed class SlidePanelElement : SlideElement
{
    /// <summary>
    /// 内边距。
    /// </summary>
    public double Padding { get; init; }

    /// <summary>
    /// 背景颜色字符串。
    /// </summary>
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

    /// <summary>
    /// 子元素列表。
    /// </summary>
    public List<SlideElement> Children { get; } = [];
}

/// <summary>
/// 矩形元素。
/// </summary>
public sealed class SlideRectElement : SlideElement
{
    /// <summary>
    /// 填充颜色字符串。
    /// </summary>
    public string? Fill { get; init; }

    /// <summary>
    /// 描边颜色字符串。
    /// </summary>
    public string? Stroke { get; init; }

    /// <summary>
    /// 描边宽度。
    /// </summary>
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

/// <summary>
/// 文本元素。
/// </summary>
public sealed class SlideTextElement : SlideElement
{
    /// <summary>
    /// 文本内容。
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// 字体名称。
    /// </summary>
    public string FontName { get; init; } = "Microsoft YaHei";

    /// <summary>
    /// 字体大小。
    /// </summary>
    public double FontSize { get; init; } = 16;

    /// <summary>
    /// 字体颜色字符串。
    /// </summary>
    public string Foreground { get; init; } = "#000000";

    /// <summary>
    /// 文本对齐方式。
    /// </summary>
    public SlideTextAlignment TextAlignment { get; init; } = SlideTextAlignment.Left;

    /// <summary>
    /// 行高（倍数）。
    /// </summary>
    public double LineHeight { get; init; } = 1.2;

    /// <summary>
    /// 实际行数（由渲染引擎计算回填）。
    /// </summary>
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

/// <summary>
/// 图片元素。
/// </summary>
public sealed class SlideImageElement : SlideElement
{
    /// <summary>
    /// 图片源路径。
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// 图片拉伸方式。
    /// </summary>
    public SlideImageStretch Stretch { get; init; } = SlideImageStretch.Uniform;
}