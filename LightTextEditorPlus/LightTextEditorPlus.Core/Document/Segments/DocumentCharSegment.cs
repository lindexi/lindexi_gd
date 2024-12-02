using System.Diagnostics;

namespace LightTextEditorPlus.Core.Document.Segments;

/// <summary>
/// 文档的按照字符定位的一段
/// </summary>
/// 对应 <see cref="LightTextEditorPlus.Core.Carets.Selection"/> 采用光标单位，这个类型是采用 <see cref="DocumentOffset"/> 文档字符单位
public readonly struct DocumentCharSegment
{
    /// <summary>
    /// 创建文档的按照字符定位的一段
    /// </summary>
    /// <param name="startOffset"></param>
    /// <param name="length"></param>
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