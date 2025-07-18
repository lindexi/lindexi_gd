using System;
using SkiaSharp;

namespace LightTextEditorPlus.Primitive;

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