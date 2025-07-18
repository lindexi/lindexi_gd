using System;
using SkiaSharp;

namespace LightTextEditorPlus.Primitive;

/// <summary>
/// 线性渐变的 Skia 文本画刷
/// </summary>
public sealed class LinearGradientSkiaTextBrush : SkiaTextBrush
{
    /// <summary>
    /// 起始点
    /// </summary>
    public required GradientSkiaTextBrushRelativePoint StartPoint { get; init; }

    /// <summary>
    /// 结束点
    /// </summary>
    public required GradientSkiaTextBrushRelativePoint EndPoint { get; init; }

    /// <summary>
    /// 不透明度
    /// </summary>
    public double Opacity { get; init; } = 1;

    /// <summary>
    /// 渐变刻度
    /// </summary>
    public required SkiaTextGradientStopCollection GradientStops { get; init; }

    /// <inheritdoc />
    protected internal override void Apply(in SkiaTextBrushRenderContext context)
    {
        SKPaint paint = context.Paint;

        SKRect renderBounds = context.RenderBounds;
        SKPoint startPoint = StartPoint.ToSKPoint(renderBounds);
        SKPoint endPoint = EndPoint.ToSKPoint(renderBounds);

        double opacity = context.Opacity * Opacity;
        opacity = Math.Min(opacity, 1);

        var (colorList, offsetList) = GradientStops.GetCacheList(opacity);

        var linearGradient = SKShader.CreateLinearGradient(startPoint, endPoint, colorList, offsetList, SKShaderTileMode.Clamp);

        paint.Shader = linearGradient;
    }

    /// <inheritdoc />
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