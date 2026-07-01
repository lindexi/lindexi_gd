namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// Panel 容器元素，可包含子元素。
/// </summary>
public sealed class SlideMlPanelElement : SlideMlElement
{
    /// <summary>
    /// 内边距，支持四边独立值。如 "0,0,0,0" 或 "8 16" 格式。
    /// </summary>
    public SlideMlThickness Padding { get; init; }

    /// <summary>
    /// 背景画刷（纯色或渐变）。
    /// </summary>
    public ISlideMlBrush? Background { get; init; }

    /// <summary>
    /// 子元素排列方向。默认 <see cref="SlideMlLayoutDirection.Absolute"/>（绝对定位）。
    /// </summary>
    public SlideMlLayoutDirection Layout { get; init; }

    /// <summary>
    /// 流式布局下子元素之间的默认间距（px）。
    /// </summary>
    public double Gap { get; init; }

    /// <summary>
    /// 子元素列表。
    /// </summary>
    public List<SlideMlElement> Children { get; } = [];
}
