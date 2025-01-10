using SkiaSharp;

namespace LightTextEditorPlus.Diagnostics;

/// <summary>
/// 刻度线的调试绘制信息
/// </summary>
/// <param name="DebugColor">颜色</param>
/// <param name="StrokeThickness">线条粗细</param>
public readonly record struct HandwritingPaperGradationDebugDrawInfo(SKColor DebugColor, float StrokeThickness);
