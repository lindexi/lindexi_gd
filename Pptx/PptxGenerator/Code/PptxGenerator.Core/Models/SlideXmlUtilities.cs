namespace PptxGenerator.Models;

/// <summary>
/// 渲染度量信息，承载回填到 XML 中的实际尺寸和位置。
/// </summary>
public sealed class SlideMlRenderedMetrics
{
    /// <summary>
    /// 渲染后的实际尺寸，格式为 "宽x高"（保留两位小数）。
    /// </summary>
    public string? RenderSize { get; init; }

    /// <summary>
    /// 渲染后的实际布局位置，格式为 "XxY"（保留两位小数）。
    /// </summary>
    public string? RenderLocation { get; init; }

    /// <summary>
    /// 实际行数（仅文本元素有意义）。
    /// </summary>
    public int? ActualLineCount { get; init; }
}
