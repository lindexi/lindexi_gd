namespace PptxGenerator.Models.SlideDocuments;

/// <summary>
/// 文本元素。
/// </summary>
public sealed class SlideMlTextElement : SlideMlElement
{
    /// <summary>
    /// 文本内容。
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// 字体名称。
    /// </summary>
    public string FontName { get; init; } = "Microsoft YaHei";

    /// <summary>
    /// 字体大小。
    /// </summary>
    public double FontSize { get; init; } = 16;

    /// <summary>
    /// 字体颜色字符串。
    /// </summary>
    public string Foreground { get; init; } = "#000000";

    /// <summary>
    /// 文本对齐方式。
    /// </summary>
    public SlideMlTextAlignment TextAlignment { get; init; } = SlideMlTextAlignment.Left;

    /// <summary>
    /// 实际行数（由渲染引擎计算回填）。
    /// </summary>
    public int ActualLineCount { get; set; }

    /// <summary>
    /// 是否为粗体。true 为粗体，false 或 null 为正常粗细。
    /// </summary>
    public bool? IsBold { get; init; }

    /// <summary>
    /// 是否为斜体。
    /// </summary>
    public bool? IsItalic { get; init; }

    /// <summary>
    /// 富文本内联片段。非空时优先于 <see cref="Text"/> 渲染。
    /// </summary>
    public IReadOnlyList<SlideMlSpan>? Spans { get; init; }

    /// <summary>
    /// 引用的 TextStyle 标识符。
    /// </summary>
    public string? Style { get; init; }
}
