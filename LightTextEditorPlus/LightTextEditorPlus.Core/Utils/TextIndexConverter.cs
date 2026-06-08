using System;
using System.Text;

using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Utils;

/// <summary>
/// 提供 UTF-16 字符串索引与文档字符偏移之间的转换。
/// 文档字符偏移中，代理对字符（如 emoji）算 1 个字符，\r\n 折叠为 1 个字符。
/// </summary>
public static class TextIndexConverter
{
    /// <summary>
    /// 将 UTF-16 字符串索引转换为文档字符偏移。
    /// 文档字符偏移中，\r\n 和代理对字符各算 1 个字符。
    /// </summary>
    /// <param name="text">原始文本（使用 UTF-16 编码）。</param>
    /// <param name="utf16Index">UTF-16 索引，即 string[index] 的 index。</param>
    /// <returns>对应的文档字符偏移。</returns>
    public static DocumentOffset ConvertUtf16IndexToDocumentOffset(string text, int utf16Index)
    {
        ArgumentNullException.ThrowIfNull(text);
        return ConvertUtf16IndexToDocumentOffset(text.AsSpan(), utf16Index);
    }

    /// <summary>
    /// 将 UTF-16 字符串索引转换为文档字符偏移。
    /// 文档字符偏移中，\r\n 和代理对字符各算 1 个字符。
    /// </summary>
    /// <param name="text">原始文本（使用 UTF-16 编码）的跨度。</param>
    /// <param name="utf16Index">UTF-16 索引，即 text[index] 的 index。</param>
    /// <returns>对应的文档字符偏移。</returns>
    public static DocumentOffset ConvertUtf16IndexToDocumentOffset(ReadOnlySpan<char> text, int utf16Index)
    {
        if (utf16Index < 0)
            throw new ArgumentOutOfRangeException(nameof(utf16Index));

        if (utf16Index == 0)
            return new DocumentOffset(0);

        if (utf16Index > text.Length)
            throw new ArgumentOutOfRangeException(nameof(utf16Index));

        var offset = 0;
        var currentUtf16Index = 0;
        var isLastCharCarriageReturn = false;

        foreach (Rune rune in text.EnumerateRunes())
        {
            if (currentUtf16Index >= utf16Index)
                break;

            if (rune.Value is '\r')
            {
                isLastCharCarriageReturn = true;
                offset++;
                currentUtf16Index += rune.Utf16SequenceLength;
                continue;
            }

            if (rune.Value is '\n')
            {
                currentUtf16Index += rune.Utf16SequenceLength;
                if (isLastCharCarriageReturn)
                {
                    isLastCharCarriageReturn = false;
                    continue;
                }

                offset++;
                continue;
            }

            isLastCharCarriageReturn = false;
            offset++;
            currentUtf16Index += rune.Utf16SequenceLength;
        }

        return new DocumentOffset(offset);
    }

    /// <summary>
    /// 将文档字符偏移转换为 UTF-16 字符串索引（即 string 中的下标）。
    /// </summary>
    /// <param name="text">原始文本（使用 UTF-16 编码）。</param>
    /// <param name="documentOffset">文档字符偏移。</param>
    /// <returns>对应的 UTF-16 索引（string 下标）。</returns>
    public static int ConvertDocumentOffsetToUtf16Index(string text, DocumentOffset documentOffset)
    {
        ArgumentNullException.ThrowIfNull(text);
        return ConvertDocumentOffsetToUtf16Index(text.AsSpan(), documentOffset);
    }

    /// <summary>
    /// 将文档字符偏移转换为 UTF-16 字符串索引（即 string 中的下标）。
    /// </summary>
    /// <param name="text">原始文本（使用 UTF-16 编码）的跨度。</param>
    /// <param name="documentOffset">文档字符偏移。</param>
    /// <returns>对应的 UTF-16 索引（string 下标）。</returns>
    public static int ConvertDocumentOffsetToUtf16Index(ReadOnlySpan<char> text, DocumentOffset documentOffset)
    {
        int offset = documentOffset.Offset;

        if (offset <= 0)
            return offset;

        if (offset >= text.Length)
            return text.Length;

        var currentDocumentOffset = 0;
        var currentUtf16Index = 0;
        var isLastCharCarriageReturn = false;

        foreach (Rune rune in text.EnumerateRunes())
        {
            if (currentDocumentOffset >= offset)
                return currentUtf16Index;

            if (rune.Value is '\r')
            {
                isLastCharCarriageReturn = true;
                currentDocumentOffset++;
                currentUtf16Index += rune.Utf16SequenceLength;
                continue;
            }

            if (rune.Value is '\n')
            {
                currentUtf16Index += rune.Utf16SequenceLength;
                if (isLastCharCarriageReturn)
                {
                    isLastCharCarriageReturn = false;
                    continue;
                }

                currentDocumentOffset++;
                continue;
            }

            isLastCharCarriageReturn = false;
            currentDocumentOffset++;
            currentUtf16Index += rune.Utf16SequenceLength;
        }

        return currentUtf16Index;
    }

    /// <summary>
    /// 计算 UTF-16 范围内的文档字符长度。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <param name="utf16Start">UTF-16 起始索引（含）。</param>
    /// <param name="utf16Length">UTF-16 长度（char 数）。</param>
    /// <returns>对应的文档字符长度。</returns>
    public static int GetDocumentLength(string text, int utf16Start, int utf16Length)
    {
        ArgumentNullException.ThrowIfNull(text);
        return GetDocumentLength(text.AsSpan(), utf16Start, utf16Length);
    }

    /// <summary>
    /// 计算 UTF-16 范围内的文档字符长度。
    /// </summary>
    /// <param name="text">原始文本的跨度。</param>
    /// <param name="utf16Start">UTF-16 起始索引（含）。</param>
    /// <param name="utf16Length">UTF-16 长度（char 数）。</param>
    /// <returns>对应的文档字符长度。</returns>
    public static int GetDocumentLength(ReadOnlySpan<char> text, int utf16Start, int utf16Length)
    {
        var utf16EndExclusive = utf16Start + utf16Length;
        return ConvertUtf16IndexToDocumentOffset(text, utf16EndExclusive)
             - ConvertUtf16IndexToDocumentOffset(text, utf16Start);
    }
}
