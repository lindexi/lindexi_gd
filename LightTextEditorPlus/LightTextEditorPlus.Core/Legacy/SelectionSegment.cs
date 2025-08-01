//namespace LightTextEditorPlus.Core.Document.Segments;

///// <summary>
///// 按照选择定位的一段。这是按照光标选择的内容，例如从 0 到 1 表示选择 0 到 1 范围内的字符，等于选择第 0 个字符。同理，例如从 1 到 10 表示选择第 1 到第 9 个字符
///// </summary>
//public readonly struct SelectionSegment
//{
//    public SelectionSegment(int selectionStart, int sectionLength)
//    {
//        SelectionStart = selectionStart;
//        SectionLength = sectionLength;
//    }

//    /// <summary>
//    /// 选择的起始点
//    /// </summary>
//    public int SelectionStart { get; }

//    /// <summary>
//    /// 选择的结束点
//    /// </summary>
//    public int SectionEnd => SelectionStart + SectionLength;

//    public int SectionLength { get; }
//}