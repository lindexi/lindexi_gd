using System.Diagnostics;

namespace LightTextEditorPlus.Core.Document.Segments;

/// <summary>
/// 文档的按照字符定位的一段
/// </summary>
public readonly struct DocumentCharSegment
{
    [DebuggerStepThrough]
    public DocumentCharSegment(DocumentOffset startOffset, int length)
    {
        StartOffset = startOffset;
        Length = length;
    }

    /// <summary>
    /// 获取该段的起始位置的偏移量
    /// </summary>
    public DocumentOffset StartOffset { get; }

    /// <summary>
    /// 获取该段的长度，这个长度不包含行结束符
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// 获取该段的结束位置后一个位置的偏移量，EndOffset = Offset + Length；
    /// </summary>
    public DocumentOffset EndOffset => StartOffset + Length;
}