namespace LightTextEditorPlus.Core.Document.Segments;

/// <summary>
/// 表示段落的偏移量
/// </summary>
readonly struct ParagraphOffset
{
    public ParagraphOffset(int offset)
    {
        Offset = offset;
    }

    /// <summary>
    /// 段落偏移量
    /// </summary>
    public int Offset { get; }
}