using System;

namespace LightTextEditorPlus.Core.Document.Segments;

/// <summary>
/// 表示段落的索引，表示文档里面的第几个段落
/// </summary>
/// <param name="Index">从0开始</param>
public readonly record struct ParagraphIndex(int Index) : IEquatable<int>
{
    /// <summary>
    /// 计算下一个段落的索引
    /// </summary>
    /// <param name="paragraph"></param>
    /// <returns></returns>
    public static ParagraphIndex operator ++(ParagraphIndex paragraph)
    {
        return new ParagraphIndex(paragraph.Index + 1);
    }

    /// <summary>
    /// 在原有段落索引上增加指定的值
    /// </summary>
    /// <param name="paragraph"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static ParagraphIndex operator +(ParagraphIndex paragraph, int value)
    {
        return new ParagraphIndex(paragraph.Index + value);
    }

    /// <summary>
    /// 计算上一个段落的索引
    /// </summary>
    /// <param name="paragraph"></param>
    /// <returns></returns>
    public static ParagraphIndex operator --(ParagraphIndex paragraph)
    {
        return new ParagraphIndex(paragraph.Index - 1);
    }

    /// <summary>
    /// 在原有段落索引上减去指定的值
    /// </summary>
    /// <param name="paragraph"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static ParagraphIndex operator -(ParagraphIndex paragraph, int value)
    {
        return new ParagraphIndex(paragraph.Index - value);
    }

    /// <summary>
    /// 判断两个段落索引是否相等
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(ParagraphIndex left, int right)
    {
        return left.Index == right;
    }

    /// <summary>
    /// 判断两个段落索引是否不相等
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(ParagraphIndex left, int right)
    {
        return !(left == right);
    }

    /// <summary>
    /// 判断两个段落索引大小关系
    /// </summary>
    public static bool operator >(ParagraphIndex left, int right)
    {
        return left.Index > right;
    }

    /// <summary>
    /// 判断两个段落索引大小关系
    /// </summary>
    public static bool operator <(ParagraphIndex left, int right)
    {
        return left.Index < right;
    }

    /// <summary>
    /// 判断两个段落索引大小关系
    /// </summary>
    public static bool operator >=(ParagraphIndex left, int right)
    {
        return left.Index >= right;
    }

    /// <summary>
    /// 判断两个段落索引大小关系
    /// </summary>
    public static bool operator <=(ParagraphIndex left, int right)
    {
        return left.Index <= right;
    }

    /// <inheritdoc />
    public bool Equals(int other)
    {
        return Index == other;
    }
}
