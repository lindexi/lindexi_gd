using SkiaSharp;

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