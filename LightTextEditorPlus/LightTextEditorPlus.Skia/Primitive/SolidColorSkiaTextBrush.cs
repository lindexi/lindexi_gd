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
    protected internal override void Apply(SKPaint paint)
    {
        paint.Color = Color;
    }
}