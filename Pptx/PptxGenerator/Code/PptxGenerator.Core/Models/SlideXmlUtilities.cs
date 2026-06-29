namespace PptxGenerator.Models;

/// <summary>
/// 渲染度量信息，承载回填到 XML 中的实际尺寸。
/// </summary>
public sealed class SlideMlRenderedMetrics
{
    /// <summary>
    /// 实际渲染宽度。
    /// </summary>
    public double ActualWidth { get; init; }

    /// <summary>
    /// 实际渲染高度。
    /// </summary>
    public double ActualHeight { get; init; }

    /// <summary>
    /// 实际行数（仅文本元素有意义）。
    /// </summary>
    public int? ActualLineCount { get; init; }
}
