namespace LightTextEditorPlus.Core.Document;

class LayoutCharData : IParagraphCache
{
    public LayoutCharData(CharData charData, ParagraphData paragraph)
    {
        CharData = charData;
        Paragraph = paragraph;
        paragraph.InitVersion(this);
    }

    public CharData CharData { get; }

    internal ParagraphData Paragraph { get; }

    public uint CurrentParagraphVersion { get; set; }

    /// <summary>
    /// 字符数据是否失效
    /// </summary>
    /// <remarks>如果字符数据版本和段落版本不同步，则字符数据没有被布局更新，证明数据失效</remarks>
    /// <returns></returns>
    public bool IsInvalidVersion() => Paragraph.IsInvalidVersion(this);

    /// <summary>
    /// 从段落更新缓存版本信息
    /// </summary>
    public void UpdateVersion() => Paragraph.UpdateVersion(this);
}