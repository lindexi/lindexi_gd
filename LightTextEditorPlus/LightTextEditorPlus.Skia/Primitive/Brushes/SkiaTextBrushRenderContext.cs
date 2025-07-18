using SkiaSharp;

namespace LightTextEditorPlus.Primitive;

/// <summary>
/// 画刷应用到渲染的上下文
/// </summary>
/// <param name="Paint"></param>
/// <param name="Canvas"></param>
/// <param name="RenderBounds">渲染范围，给渐变色使用</param>
/// <param name="Opacity"></param>
public readonly record struct SkiaTextBrushRenderContext(SKPaint Paint, SKCanvas Canvas, SKRect RenderBounds, double Opacity);