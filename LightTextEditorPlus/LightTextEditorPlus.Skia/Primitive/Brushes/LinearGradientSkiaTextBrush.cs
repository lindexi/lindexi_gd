using System;
using SkiaSharp;

namespace LightTextEditorPlus.Primitive;

/// <summary>
/// 线性渐变的 Skia 文本画刷
/// </summary>
public sealed class LinearGradientSkiaTextBrush : SkiaTextBrush, IEquatable<LinearGradientSkiaTextBrush>
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

        var linearGradient =
            SKShader.CreateLinearGradient(startPoint, endPoint, colorList, offsetList, SKShaderTileMode.Clamp);

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

    /// <inheritdoc />
    public bool Equals(LinearGradientSkiaTextBrush? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!StartPoint.Equals(other.StartPoint))
        {
            return false;
        }

        if (!EndPoint.Equals(other.EndPoint))
        {
            return false;
        }

        if (!Opacity.Equals(other.Opacity))
        {
            return false;
        }

        if (!GradientStops.Equals(other.GradientStops))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is LinearGradientSkiaTextBrush other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(StartPoint, EndPoint, Opacity, GradientStops);
    }
}