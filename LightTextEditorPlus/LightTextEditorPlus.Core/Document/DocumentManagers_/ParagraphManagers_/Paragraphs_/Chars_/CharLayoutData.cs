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
    public Point StartPoint { set; get; }

    public ParagraphCharOffset CharIndex { set; get; }

    // todo 提供获取是第几行，第几个字符功能

    /// <summary>
    /// 当前所在的行
    /// </summary>
    public LineLayoutData? CurrentLine { set; get; }
}