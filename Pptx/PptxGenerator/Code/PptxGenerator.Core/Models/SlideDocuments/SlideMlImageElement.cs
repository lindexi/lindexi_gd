namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// 图片元素。
/// </summary>
public sealed class SlideMlImageElement : SlideMlElement
{
    /// <summary>
    /// 图片源路径。
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// 图片拉伸方式。
    /// </summary>
    public SlideMlImageStretch Stretch { get; init; } = SlideMlImageStretch.Uniform;
}
