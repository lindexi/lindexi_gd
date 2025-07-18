using System;

using SkiaSharp;

using System.Collections.Generic;


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
    protected internal override void Apply(in SkiaTextBrushRenderContext context)
    {
        context.Paint.Color = color;
    }
}

public sealed class LinearGradientSkiaTextBrush : SkiaTextBrush
{
    public GradientSkiaTextBrushRelativePoint StartPoint { get; init; }
    public GradientSkiaTextBrushRelativePoint EndPoint { get; init; }
    //public double Opacity { get; init; }
    public SkiaTextGradientStopCollection GradientStops { get; init; } = [];

    /// <inheritdoc />
    protected internal override void Apply(in SkiaTextBrushRenderContext context)
    {
        SKPaint paint = context.Paint;

        SKRect renderBounds = context.RenderBounds;
        SKPoint startPoint = StartPoint.ToSKPoint(renderBounds);
        SKPoint endPoint = EndPoint.ToSKPoint(renderBounds);

        var (colorList, offsetList) = GradientStops.GetList();
        var linearGradient = SKShader.CreateLinearGradient(startPoint, endPoint, colorList, offsetList, SKShaderTileMode.Clamp);
        paint.Shader = linearGradient;
    }
}

public readonly record struct GradientSkiaTextBrushRelativePoint(float X, float Y, GradientSkiaTextBrushRelativePoint.RelativeUnit Unit)
{
    public SKPoint ToSKPoint(SKRect bounds)
    {
        return Unit switch
        {
            RelativeUnit.Relative => new SKPoint(bounds.Left + X * bounds.Width, bounds.Top + Y * bounds.Height),
            RelativeUnit.Absolute => new SKPoint(X, Y),
            _ => throw new ArgumentOutOfRangeException(nameof(Unit), Unit, null)
        };
    }

    public enum RelativeUnit : byte
    {
        Relative,
        Absolute,
    }
}

public class SkiaTextGradientStopCollection : List<SkiaTextGradientStop>
{
    internal (SKColor[] ColorList, float[] OffsetList) GetList()
    {
        SKColor[] colorList = new SKColor[this.Count];
        float[] offsetList = new float[this.Count];

        for (var i = 0; i < this.Count; i++)
        {
            var (skColor, offset) = this[i];
            colorList[i] = skColor;
            offsetList[i] = offset;
        }

        return (colorList, offsetList);
    }
}

public readonly record struct SkiaTextGradientStop(SKColor Color, float Offset)
{
}