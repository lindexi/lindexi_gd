using System.Linq;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 段内行测量布局参数
/// </summary>
/// <param name="ParagraphIndex">当前是第几段</param>
/// <param name="LineIndex">当前是段内第几行</param>
/// <param name="Paragraph"></param>
/// <param name="CharDataList"></param>
/// <param name="LineMaxWidth">这一行能布局的最大宽度</param>
/// <param name="CurrentStartPoint">当前行的起始点，相对于段落的坐标</param>
/// <param name="UpdateLayoutContext"></param>
public readonly record struct WholeLineLayoutArgument(ParagraphIndex ParagraphIndex, int LineIndex,
    ITextParagraph Paragraph, in TextReadOnlyListSpan<CharData> CharDataList, double LineMaxWidth,
    TextPointInParagraph CurrentStartPoint, UpdateLayoutContext UpdateLayoutContext)
{
    public ParagraphProperty ParagraphProperty => Paragraph.ParagraphProperty;

    /// <summary>
    /// 调试使用的这一行的文本
    /// </summary>
    public string DebugText => $"第 {ParagraphIndex.Index} 段，第 {LineIndex} 行。文本：{string.Join("", CharDataList.Select(t => t.CharObject.ToText()))}";
}
