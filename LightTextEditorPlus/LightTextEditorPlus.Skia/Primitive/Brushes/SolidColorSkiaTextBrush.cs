using System;

using SkiaSharp;

namespace LightTextEditorPlus.Primitive;

/// <summary>
/// 纯色画刷
/// </summary>
/// <param name="Color">画刷的颜色</param>
public sealed record SolidColorSkiaTextBrush(SKColor Color) : SkiaTextBrush, IEquatable<SolidColorSkiaTextBrush>
{
    /// <inheritdoc />
    protected internal override void Apply(in SkiaTextBrushRenderContext context)
    {
        SKPaint skPaint = context.Paint;

        skPaint.Color = Color;

        if (context.Opacity < 1)
        {
            // 处理透明度
            skPaint.Color = skPaint.Color.WithAlpha((byte) (skPaint.Color.Alpha * context.Opacity));
        }
    }

    /// <inheritdoc />
    public override SKColor AsSolidColor() => Color;
}