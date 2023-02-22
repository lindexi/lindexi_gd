using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 光标下的渲染信息
/// </summary>
public readonly struct CaretRenderInfo
{
    internal CaretRenderInfo(int lineIndex, int hitLineOffset, CharData charData, ParagraphData paragraphData, ParagraphCaretOffset hitOffset, CaretOffset caretOffset)
    {
        LineIndex = lineIndex;
        HitLineOffset = hitLineOffset;
        CharData = charData;
        ParagraphData = paragraphData;
        HitOffset = hitOffset;
        CaretOffset = caretOffset;
    }

    /// <summary>
    /// 行在段落里的序号
    /// </summary>
    public int LineIndex { get; }

    /// <summary>
    /// 命中到行的哪个字符
    /// </summary>
    public int HitLineOffset { get; }

    public CharData CharData { get; }

    /// <summary>
    /// 是否一个空段
    /// </summary>
    public bool IsEmptyParagraph => ParagraphData.CharCount == 0;

    internal ParagraphData ParagraphData { get; }
    internal ParagraphCaretOffset HitOffset { get; }

    public CaretOffset CaretOffset { get; }
}