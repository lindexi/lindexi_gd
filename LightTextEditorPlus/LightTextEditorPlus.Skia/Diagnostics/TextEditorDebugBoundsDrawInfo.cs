using SkiaSharp;

namespace LightTextEditorPlus.Diagnostics;

/// <summary>
/// 调试绘制的边界信息
/// </summary>
public class TextEditorDebugBoundsDrawInfo
{
    /// <summary>
    /// 调试绘制的边框颜色
    /// </summary>
    public SKColor? StrokeColor { get; set; }

    /// <summary>
    /// 调试绘制的边框粗细
    /// </summary>
    public float StrokeThickness { get; set; } = 1;

    /// <summary>
    /// 调试绘制的填充颜色
    /// </summary>
    public SKColor? FillColor { get; set; }
}