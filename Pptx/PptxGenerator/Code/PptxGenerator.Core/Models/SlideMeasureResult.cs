namespace PptxGenerator;

/// <summary>
/// 承载 PreMeasure 阶段的测量结果，无 UI 框架依赖。
/// </summary>
public sealed class SlideMeasureResult
{
    /// <summary>
    /// 测量后的宽度。
    /// </summary>
    public double MeasuredWidth { get; init; }

    /// <summary>
    /// 测量后的高度。
    /// </summary>
    public double MeasuredHeight { get; init; }

    /// <summary>
    /// 实际行数（仅文本元素有意义）。
    /// </summary>
    public int? ActualLineCount { get; init; }
}