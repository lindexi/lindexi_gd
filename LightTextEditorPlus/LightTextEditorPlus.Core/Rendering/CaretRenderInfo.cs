using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 光标下的渲染信息
/// </summary>
public readonly struct CaretRenderInfo
{
    internal CaretRenderInfo(int lineIndex, int hitLineOffset, ParagraphCaretOffset hitOffset, CaretOffset caretOffset, LineLayoutData lineLayoutData)
    {
        LineIndex = lineIndex;
        HitLineOffset = hitLineOffset;
        HitOffset = hitOffset;
        CaretOffset = caretOffset;
        LineLayoutData = lineLayoutData;
    }

    /// <summary>
    /// 行在段落里的序号
    /// </summary>
    public int LineIndex { get; }

    /// <summary>
    /// 段落在文档里属于第几段
    /// </summary>
    public int ParagraphIndex => ParagraphData.Index;

    /// <summary>
    /// 这一行的字符列表
    /// </summary>
    public ReadOnlyListSpan<CharData> LineCharDataList => LineLayoutData.GetCharList();

    /// <summary>
    /// 行的范围
    /// </summary>
    public Rect LineBounds => LineLayoutData.GetLineBounds();

    /// <summary>
    /// 命中到行的哪个字符
    /// </summary>
    public int HitLineOffset { get; }

    /// <summary>
    /// 是否命中到行的起点
    /// </summary>
    public bool IsLineStart => CaretOffset.IsAtLineStart;

    /// <summary>
    /// 命中的字符。如果是空段，那将没有命中哪个字符。对于在行首或段首的，那将命中在光标前面的字符
    /// </summary>
    public CharData? CharData
    {
        get
        {
            if (LineLayoutData.CharCount == 0)
            {
                return null;
            }
            else
            {
                return LineLayoutData.GetCharList()[HitLineOffset];
            }
        }
    }

    /// <summary>
    /// 获取在光标之后的字符。如果是空段，那就空
    /// </summary>
    /// <returns></returns>
    public CharData? GetCharDataAfterCaretOffset()
    {
        if (CaretOffset.IsAtLineStart)
        {
            return CharData;
        }

        var hitCharOffset = new ParagraphCharOffset(HitOffset.Offset + 1);
        var hitCharData = ParagraphData.GetCharData(hitCharOffset);
        return hitCharData;
    }

    /// <summary>
    /// 是否一个空段
    /// </summary>
    public bool IsEmptyParagraph => ParagraphData.CharCount == 0;

    internal ParagraphData ParagraphData => LineLayoutData.CurrentParagraph;
    internal ParagraphCaretOffset HitOffset { get; }

    internal LineLayoutData LineLayoutData { get; }

    /// <summary>
    /// 光标偏移量
    /// </summary>
    public CaretOffset CaretOffset { get; }
}