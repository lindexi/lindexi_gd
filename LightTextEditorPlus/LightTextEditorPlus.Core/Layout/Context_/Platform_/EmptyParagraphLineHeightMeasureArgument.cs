using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 空段的行高测量参数
/// </summary>
/// <param name="Paragraph">段落</param>
/// <param name="ParagraphIndex">段落序号</param>
/// <param name="UpdateLayoutContext">布局上下文信息</param>
public readonly record struct EmptyParagraphLineHeightMeasureArgument(
    ITextParagraph Paragraph,
    ParagraphIndex ParagraphIndex,
    UpdateLayoutContext UpdateLayoutContext)
{
    /// <summary>
    /// 段落属性
    /// </summary>
    public ParagraphProperty ParagraphProperty => Paragraph.ParagraphProperty;

    /// <summary>
    /// 段落起始的字符属性
    /// </summary>
    public IReadOnlyRunProperty ParagraphStartRunProperty => Paragraph.ParagraphStartRunProperty;
}
