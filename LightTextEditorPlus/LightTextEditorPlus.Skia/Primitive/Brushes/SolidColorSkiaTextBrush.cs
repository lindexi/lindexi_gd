using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Drawing;


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
        SKPaint skPaint = context.Paint;

        skPaint.Color = color;

        if (context.Opacity < 1)
        {
            // 处理透明度
            skPaint.Color = skPaint.Color.WithAlpha((byte) (skPaint.Color.Alpha * context.Opacity));
        }
    }

    public override SKColor AsSolidColor() => Color;
}

public sealed class LinearGradientSkiaTextBrush : SkiaTextBrush
{
    public GradientSkiaTextBrushRelativePoint StartPoint { get; init; }
    public GradientSkiaTextBrushRelativePoint EndPoint { get; init; }
    public double Opacity { get; init; } = 1;
    public SkiaTextGradientStopCollection GradientStops { get; init; } = [];

    /// <inheritdoc />
    protected internal override void Apply(in SkiaTextBrushRenderContext context)
    {
        SKPaint paint = context.Paint;

        SKRect renderBounds = context.RenderBounds;
        SKPoint startPoint = StartPoint.ToSKPoint(renderBounds);
        SKPoint endPoint = EndPoint.ToSKPoint(renderBounds);

        double opacity = context.Opacity * Opacity;
        opacity = Math.Min(opacity, 1);

        var (colorList, offsetList) = GradientStops.GetList(opacity);

        var linearGradient = SKShader.CreateLinearGradient(startPoint, endPoint, colorList, offsetList, SKShaderTileMode.Clamp);

        paint.Shader = linearGradient;
    }

    public override SKColor AsSolidColor()
    {
        if (GradientStops.Count > 0)
        {
            SKColor skColor = GradientStops[0].Color;
            return skColor.WithAlpha((byte) (skColor.Alpha * Opacity));
        }
        else
        {
            // 没有渐变色时返回默认黑色
            return SKColors.Black;
        }
    }
}

public readonly record struct GradientSkiaTextBrushRelativePoint
{
    public GradientSkiaTextBrushRelativePoint(float x, float y, GradientSkiaTextBrushRelativePoint.RelativeUnit unit = GradientSkiaTextBrushRelativePoint.RelativeUnit.Relative)
    {
        X = x;
        Y = y;
        Unit = unit;

        if (unit != RelativeUnit.Relative)
        {
            if (X is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(X), $"当 RelativeUnit 为 Relative 时，应该是在 [0-1] 范围内");
            }

            if (Y is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(Y), $"当 RelativeUnit 为 Relative 时，应该是在 [0-1] 范围内");
            }
        }
    }

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

    public float X { get; init; }
    public float Y { get; init; }
    public GradientSkiaTextBrushRelativePoint.RelativeUnit Unit { get; init; }

    public void Deconstruct(out float X, out float Y, out GradientSkiaTextBrushRelativePoint.RelativeUnit Unit)
    {
        X = this.X;
        Y = this.Y;
        Unit = this.Unit;
    }
}

public class SkiaTextGradientStopCollection : List<SkiaTextGradientStop>
{
    internal (SKColor[] ColorList, float[] OffsetList) GetList(double opacity)
    {
        SKColor[] colorList = new SKColor[this.Count];
        float[] offsetList = new float[this.Count];

        for (var i = 0; i < this.Count; i++)
        {
            var (skColor, offset) = this[i];
            colorList[i] = skColor.WithAlpha((byte) (skColor.Alpha * opacity));
            offsetList[i] = offset;
        }

        return (colorList, offsetList);
    }
}

public readonly record struct SkiaTextGradientStop(SKColor Color, float Offset)
{
}