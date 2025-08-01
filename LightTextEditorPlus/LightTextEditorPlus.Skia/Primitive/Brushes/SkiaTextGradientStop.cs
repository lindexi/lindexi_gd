using SkiaSharp;

namespace LightTextEditorPlus.Primitive;

/// <summary>
/// 渐变色的刻度点
/// </summary>
/// <param name="Color"></param>
/// <param name="Offset"></param>
public readonly record struct SkiaTextGradientStop(SKColor Color, float Offset)
{
}