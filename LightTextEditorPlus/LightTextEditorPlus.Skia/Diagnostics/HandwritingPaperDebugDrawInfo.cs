namespace LightTextEditorPlus.Diagnostics;

/// <summary>
/// 四线格的调试信息
/// </summary>
public readonly record struct HandwritingPaperDebugDrawInfo
{
    public HandwritingPaperGradationDebugDrawInfo TopLineGradationDebugDrawInfo { get; init; }
    public HandwritingPaperGradationDebugDrawInfo MiddleLineGradationDebugDrawInfo { get; init; }
    public HandwritingPaperGradationDebugDrawInfo BaselineGradationDebugDrawInfo { get; init; }
    public HandwritingPaperGradationDebugDrawInfo BottomLineGradationDebugDrawInfo { get; init; }
}
