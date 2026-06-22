namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// 富文本内联片段，用于 <c>Span</c> 子元素。
/// </summary>
public sealed class SlideMlSpan
{
    /// <summary>
    /// 片段文本内容。
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// 字体大小。
    /// </summary>
    public double? FontSize { get; init; }

    /// <summary>
    /// 字体名称。
    /// </summary>
    public string? FontName { get; init; }

    /// <summary>
    /// 字体颜色。
    /// </summary>
    public string? Foreground { get; init; }

    /// <summary>
    /// 是否为粗体。true 为粗体，false 或 null 为正常粗细。
    /// </summary>
    public bool? IsBold { get; init; }

    /// <summary>
    /// 是否为斜体。
    /// </summary>
    public bool? IsItalic { get; init; }

    /// <summary>
    /// 文本装饰（如 "Underline"）。
    /// </summary>
    public string? TextDecoration { get; init; }
}
