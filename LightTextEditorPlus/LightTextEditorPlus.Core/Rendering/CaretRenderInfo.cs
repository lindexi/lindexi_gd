using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 光标下的渲染信息
/// </summary>
public readonly struct CaretRenderInfo
{
    internal CaretRenderInfo(int lineIndex, int hitLineOffset, CharData? charData, ParagraphData paragraphData, ParagraphCaretOffset hitOffset, CaretOffset caretOffset, Rect lineBounds)
    {
        LineIndex = lineIndex;
        HitLineOffset = hitLineOffset;
        CharData = charData;
        ParagraphData = paragraphData;
        HitOffset = hitOffset;
        CaretOffset = caretOffset;
        LineBounds = lineBounds;
    }

    /// <summary>
    /// 行在段落里的序号
    /// </summary>
    public int LineIndex { get; }

    public Rect LineBounds { get; }

    /// <summary>
    /// 命中到行的哪个字符
    /// </summary>
    public int HitLineOffset { get; }

    /// <summary>
    /// 命中的字符。如果是空段，那将没有命中哪个字符。对于在行首或段首的，那将命中在字符前面
    /// </summary>
    public CharData? CharData { get; }

    /// <summary>
    /// 是否一个空段
    /// </summary>
    public bool IsEmptyParagraph => ParagraphData.CharCount == 0;

    internal ParagraphData ParagraphData { get; }
    internal ParagraphCaretOffset HitOffset { get; }

    public CaretOffset CaretOffset { get; }
}