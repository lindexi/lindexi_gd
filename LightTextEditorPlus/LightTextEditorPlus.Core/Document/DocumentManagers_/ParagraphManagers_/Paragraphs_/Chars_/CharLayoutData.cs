using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 字符的布局信息，包括字符所在的段落和所在的行，字符所在的相对于文本框的坐标
/// </summary>
class CharLayoutData : IParagraphCache
{
    public CharLayoutData(CharData charData, ParagraphData paragraph)
    {
        CharData = charData;
        Paragraph = paragraph;
        paragraph.InitVersion(this);
    }

    public CharData CharData { get; }

    internal ParagraphData Paragraph { get; }

    public uint CurrentParagraphVersion { get; set; }

    public bool IsInvalidVersion() => Paragraph.IsInvalidVersion(this);

    public void UpdateVersion() => Paragraph.UpdateVersion(this);

    /// <summary>
    /// 左上角的点，相对于文本框
    /// </summary>
    /// 可用来辅助布局上下标
    public TextPoint StartPoint { set; get; }

    //public TextPoint BaselineStartPoint { set; get; }

    /// <summary>
    /// 字符是当前段落 <see cref="Paragraph"/> 的第几个字符
    /// </summary>
    /// 调试作用
    public ParagraphCharOffset CharIndex { set; get; }

    /// <summary>
    /// 字符是当前行的第几个字
    /// </summary>
    public int CharIndexInLine
    {
        get
        {
            if (CurrentLine is null)
            {
                return -1;
            }

            return CharIndex.Offset - CurrentLine.CharStartParagraphIndex;
        }
    }

    /// <summary>
    /// 当前所在的行
    /// </summary>
    public LineLayoutData? CurrentLine { set; get; }

    public override string ToString()
    {
        return $"'{CharData.CharObject}' 第{Paragraph.Index}段，第{CurrentLine?.LineInParagraphIndex}行，段内第{CharIndex.Offset}个字符，行内第{CharIndexInLine}个字符";
    }
}
