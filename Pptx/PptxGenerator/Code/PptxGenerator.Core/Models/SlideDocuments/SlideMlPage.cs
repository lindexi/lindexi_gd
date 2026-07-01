namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// SlideML 页面根元素。
/// </summary>
public sealed class SlideMlPage
{
    /// <summary>
    /// 页面背景颜色字符串。
    /// </summary>
    public string Background { get; init; } = "#FFFFFF";

    /// <summary>
    /// 页面子元素列表。
    /// </summary>
    public List<SlideMlElement> Children { get; } = [];

    /// <summary>
    /// 页面布局边界。
    /// </summary>
    public SlideMlRect LayoutBounds { get; set; } = new(0, 0, SlideDocumentContext.DefaultCanvasWidth, SlideDocumentContext.DefaultCanvasHeight);
}
