namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// 文本样式定义，用于 <c>Page.Styles</c> 中的 <c>TextStyle</c> 元素。
/// </summary>
public sealed class SlideMlTextStyle
{
    /// <summary>
    /// 样式标识符，供 <c>Style</c> 属性引用。
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 字体大小。
    /// </summary>
    public double? FontSize { get; init; }

    /// <summary>
    /// 是否为粗体。true 为粗体，false 或 null 为正常粗细。
    /// </summary>
    public bool? IsBold { get; init; }

    /// <summary>
    /// 字体颜色。
    /// </summary>
    public string? Foreground { get; init; }

    /// <summary>
    /// 字体名称。
    /// </summary>
    public string? FontName { get; init; }

    /// <summary>
    /// 文本对齐。
    /// </summary>
    public SlideMlTextAlignment? TextAlignment { get; init; }
}
