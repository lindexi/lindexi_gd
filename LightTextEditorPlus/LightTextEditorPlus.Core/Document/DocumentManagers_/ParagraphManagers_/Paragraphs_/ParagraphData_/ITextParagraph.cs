using LightTextEditorPlus.Core.Document.Segments;

using System.Text;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示文本的一个段落
/// </summary>
public interface ITextParagraph
{
    /// <summary>
    /// 段落属性样式
    /// </summary>
    ParagraphProperty ParagraphProperty { get; }

    /// <summary>
    /// 段落起始的字符属性，表示在段落第一个字符之前的样式
    /// </summary>
    IReadOnlyRunProperty ParagraphStartRunProperty { get; }

    /// <summary>
    /// 获取当前段落是文档的第几段，从0开始
    /// </summary>
    ParagraphIndex Index { get; }

    /// <summary>
    /// 这一段的字符长度。不包括 \n 字符
    /// </summary>
    int CharCount { get; }

    /// <summary>
    /// 获取段落的文本
    /// </summary>
    /// <returns></returns>
    string GetText();

    /// <summary>
    /// 获取段落的文本，将文本写入到 StringBuilder 里面
    /// </summary>
    /// <param name="stringBuilder"></param>
    void GetText(StringBuilder stringBuilder);

    /// <summary>
    /// 获取段落的字符数据列表
    /// </summary>
    /// <returns></returns>
    TextReadOnlyListSpan<CharData> GetParagraphCharDataList();
}