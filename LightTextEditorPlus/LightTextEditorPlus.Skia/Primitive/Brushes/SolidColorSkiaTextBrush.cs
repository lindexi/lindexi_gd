using LightTextEditorPlus.Core.Primitive;

using SkiaSharp;

using System.Collections.Generic;

using static LightTextEditorPlus.Primitive.GradientSkiaTextBrushRelativePoint;

namespace LightTextEditorPlus.Primitive;

/// <summary>
/// 纯色画刷
/// </summary>
/// <param name="color"></param>
public sealed class SolidColorSkiaTextBrush(SKColor color) : SkiaTextBrush
{
    /// <summary>
    /// 画刷的颜色
    /// </summary>
    public SKColor Color => color;

    /// <inheritdoc />
    protected internal override void Apply(SKPaint paint)
    {
        paint.Color = Color;
    }
}

public sealed class LinearGradientSkiaTextBrush : SkiaTextBrush
{
    public GradientSkiaTextBrushRelativePoint StartPoint { get; set; }
    public GradientSkiaTextBrushRelativePoint EndPoint { get; set; }
    public double Opacity { get; set; }
    public SkiaTextGradientStopCollection GradientStops { get; set; } = [];

    /// <inheritdoc />
    protected internal override void Apply(SKPaint paint)
    {
    }
}

public struct GradientSkiaTextBrushRelativePoint(float X, float Y, RelativeUnit Unit)
{

    public enum RelativeUnit : byte
    {
        Relative,
        Absolute,
    }
}

public class SkiaTextGradientStopCollection : List<SkiaTextGradientStop>
{
}

public readonly record struct SkiaTextGradientStop(SKColor Color, double Offset)
{
}