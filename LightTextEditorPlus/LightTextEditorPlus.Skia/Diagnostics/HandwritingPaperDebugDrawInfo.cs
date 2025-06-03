namespace LightTextEditorPlus.Diagnostics;

/// <summary>
/// 四线格的调试信息
/// </summary>
public readonly record struct HandwritingPaperDebugDrawInfo
{
    /// <summary>
    /// 四线格的顶部线的调试绘制信息
    /// </summary>
    public HandwritingPaperGradationDebugDrawInfo TopLineGradationDebugDrawInfo { get; init; }

    /// <summary>
    /// 四线格的中线的调试绘制信息
    /// </summary>
    public HandwritingPaperGradationDebugDrawInfo MiddleLineGradationDebugDrawInfo { get; init; }

    /// <summary>
    /// 四线格的基线的调试绘制信息
    /// </summary>
    public HandwritingPaperGradationDebugDrawInfo BaselineGradationDebugDrawInfo { get; init; }

    /// <summary>
    /// 四线格的底部线的调试绘制信息
    /// </summary>
    public HandwritingPaperGradationDebugDrawInfo BottomLineGradationDebugDrawInfo { get; init; }
}