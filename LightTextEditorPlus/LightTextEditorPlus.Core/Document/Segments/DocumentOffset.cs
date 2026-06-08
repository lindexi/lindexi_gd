using System;
using System.Diagnostics;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Document.Segments;

/// <summary>
/// 表示文档的偏移量
/// </summary>
public readonly struct DocumentOffset : IEquatable<DocumentOffset>, IEquatable<int>
{
    /// <summary>
    /// 创建文档的偏移量
    /// </summary>
    [DebuggerStepThrough]
    public DocumentOffset(int offset)
    {
        if (offset < DefaultDocumentOffsetValue)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "文档的偏移量最小值不能小于 -1 默认值");
        }

        Offset = offset;
    }

    private DocumentOffset(int offset, bool isInternal)
    {
        Offset = offset;
    }

    /// <summary>
    /// 文档偏移量
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// 默认的文档偏移
    /// </summary>
    public static DocumentOffset DefaultDocumentOffset => new DocumentOffset(DefaultDocumentOffsetValue, true);

    private const int DefaultDocumentOffsetValue = -1;

        /// <summary>
        /// 根据原始文本中的 UTF-16 索引创建文档偏移量。
        /// 自动处理代理对字符（如 emoji）和 \r\n 折叠。
        /// </summary>
        /// <param name="text">原始文本（使用 UTF-16 编码）。</param>
        /// <param name="utf16Index">UTF-16 索引，即 string[index] 的 index。</param>
        /// <returns>对应的文档字符偏移。</returns>
        public static DocumentOffset FromUtf16Index(string text, int utf16Index)
            => new(global::LightTextEditorPlus.Core.Utils.TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, utf16Index));

        /// <summary>
        /// 转换代码
    /// </summary>
    /// <param name="offset"></param>
    public static implicit operator int(DocumentOffset offset)
    {
        return offset.Offset;
    }

    /// <summary>
    /// 转换代码
    /// </summary>
    /// <param name="offset"></param>
    public static implicit operator DocumentOffset(int offset)
    {
        return new DocumentOffset(offset);
    }

    /// <summary>
    /// 判断大小
    /// </summary>
    /// <returns></returns>
    public static bool operator >(int a, DocumentOffset b)
    {
        return a > b.Offset;
    }

    /// <summary>
    /// 判断大小
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator >(DocumentOffset a, DocumentOffset b)
    {
        return a.Offset > b.Offset;
    }

    /// <summary>
    /// 判断大小
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator >=(int a, DocumentOffset b)
    {
        return a >= b.Offset;
    }

    /// <summary>
    /// 判断大小
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator >=(DocumentOffset a, DocumentOffset b)
    {
        return a.Offset >= b.Offset;
    }

    /// <summary>
    /// 判断大小
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator <(int a, DocumentOffset b)
    {
        return a < b.Offset;
    }

    /// <summary>
    /// 判断大小
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator <(DocumentOffset a, DocumentOffset b)
    {
        return a.Offset < b.Offset;
    }

    /// <summary>
    /// 判断大小
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator <=(int a, DocumentOffset b)
    {
        return a <= b.Offset;
    }

    /// <summary>
    /// 判断大小
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator <=(DocumentOffset a, DocumentOffset b)
    {
        return a.Offset <= b.Offset;
    }

    /// <summary>
    /// 判断大小
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator >(DocumentOffset a, int b)
    {
        return a.Offset > b;
    }

    /// <summary>
    /// 判断大小
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator >=(DocumentOffset b, int a)
    {
        return b.Offset >= a;
    }

    /// <summary>
    /// 判断大小
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator <(DocumentOffset b, int a)
    {
        return b.Offset < a;
    }

    /// <summary>
    /// 判断大小
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator <=(DocumentOffset b, int a)
    {
        return b.Offset <= a;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is DocumentOffset documentOffset)
        {
            return Offset.Equals(documentOffset.Offset);
        }

        return false;
    }

    /// <inheritdoc />
    public bool Equals(DocumentOffset other)
    {
        return Offset.Equals(other.Offset);
    }

    /// <inheritdoc />
    public bool Equals(int other)
    {
        return Offset.Equals(other);
    }

    /// <summary>
    /// 判断两个文档偏移相等
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator ==(DocumentOffset b, DocumentOffset a)
    {
        return b.Equals(a);
    }

    /// <summary>
    /// 判断两个文档偏移相等
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator ==(DocumentOffset b, int a)
    {
        return b.Equals(a);
    }

    /// <summary>
    /// 判断两个文档偏移不相等
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator !=(DocumentOffset b, int a)
    {
        return !(b == a);
    }

    /// <summary>
    /// 判断两个文档偏移相等
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator ==(int a, DocumentOffset b)
    {
        return b.Equals(a);
    }

    /// <summary>
    /// 判断两个文档偏移不相等
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator !=(int a, DocumentOffset b)
    {
        return !(b == a);
    }

    /// <summary>
    /// 判断两个文档偏移不相等
    /// </summary>
    /// <param name="b"></param>
    /// <param name="a"></param>
    /// <returns></returns>
    public static bool operator !=(DocumentOffset b, DocumentOffset a)
    {
        return !(b == a);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Offset;
    }

    /// <inheritdoc />
    public override string ToString() => Offset.ToString();
}