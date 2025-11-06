using System;

using SkiaSharp;

namespace LightTextEditorPlus.Primitive;

/// <summary>
/// 纯色画刷
/// </summary>
/// <param name="color"></param>
public sealed class SolidColorSkiaTextBrush(SKColor color) : SkiaTextBrush, IEquatable<SolidColorSkiaTextBrush>
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

    /// <inheritdoc />
    public override SKColor AsSolidColor() => Color;

    /// <inheritdoc />
    public bool Equals(SolidColorSkiaTextBrush? other)
    {
        if (other is null)
        {
            return false;
        }

        return Color.Equals(other.Color);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is SolidColorSkiaTextBrush other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Color);
    }
}