using System.Text;

namespace PptxGenerator.Streaming;

/// <summary>
/// 从 LLM token 流中增量切分出完整 XML 片段。
/// 维护一个 StringBuilder 缓冲区，使用深度计数器算法识别完整的顶层 XML 元素。
/// </summary>
public sealed class SlideMlFragmentExtractor
{
    private readonly StringBuilder _buffer = new();

    /// <summary>
    /// 追加增量文本到内部缓冲区。
    /// </summary>
    /// <param name="text">增量文本。</param>
    public void Append(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _buffer.Append(text);
    }

    /// <summary>
    /// 尝试从缓冲区中提取完整的顶层 XML 元素。
    /// </summary>
    /// <returns>返回 0~N 个完整片段，残留文本保留在缓冲区。</returns>
    public List<string> TryExtractFragments()
    {
        var fragments = new List<string>(4);
        var bufferString = _buffer.ToString();
        var consumed = 0;

        while (consumed < bufferString.Length)
        {
            var (fragment, endPos) = ScanNextFragment(bufferString, consumed);

            if (fragment is null)
            {
                // 没有找到完整片段，保留剩余内容
                break;
            }

            fragments.Add(fragment);
            consumed = endPos;
        }

        // 移除已消费的内容
        if (consumed > 0)
        {
            _buffer.Remove(0, consumed);
        }

        return fragments;
    }

    /// <summary>
    /// 获取未完成的缓冲区内容（用于 EOF 时的容错处理）。
    /// </summary>
    /// <returns>缓冲区中尚未形成完整片段的文本。</returns>
    public string GetRemaining()
    {
        return _buffer.ToString();
    }

    /// <summary>
    /// 从指定位置开始扫描，尝试提取一个完整的顶层 XML 元素。
    /// </summary>
    /// <param name="text">完整缓冲区文本。</param>
    /// <param name="startPos">起始扫描位置。</param>
    /// <returns>片段文本和结束位置；如果无法提取完整片段则返回 (null, startPos)。</returns>
    private static (string? fragment, int endPos) ScanNextFragment(string text, int startPos)
    {
        var pos = startPos;
        var depth = 0;
        var fragmentStart = -1;

        while (pos < text.Length)
        {
            // 跳过前导空白
            if (depth == 0 && fragmentStart < 0 && char.IsWhiteSpace(text[pos]))
            {
                pos++;
                continue;
            }

            if (pos >= text.Length)
            {
                break;
            }

            var ch = text[pos];

            // 遇到 <
            if (ch == '<')
            {
                // 检查后续字符
                if (pos + 1 >= text.Length)
                {
                    // 缓冲区在 < 处截断，无法判断
                    return (null, startPos);
                }

                var next = text[pos + 1];

                // 注释 <!-- -->
                if (next == '!' && pos + 3 < text.Length && text[pos + 2] == '-' && text[pos + 3] == '-')
                {
                    var commentEnd = FindSequence(text, pos + 4, "-->");
                    if (commentEnd < 0)
                    {
                        // 注释不完整，保留在缓冲区
                        return (null, startPos);
                    }

                    pos = commentEnd + 3;
                    continue;
                }

                // 处理指令 <? ?>
                if (next == '?')
                {
                    var piEnd = FindSequence(text, pos + 2, "?>");
                    if (piEnd < 0)
                    {
                        return (null, startPos);
                    }

                    pos = piEnd + 2;
                    continue;
                }

                // 结束标签 </
                if (next == '/')
                {
                    var tagEnd = text.IndexOf('>', pos + 2);
                    if (tagEnd < 0)
                    {
                        // 结束标签不完整
                        return (null, startPos);
                    }

                    depth--;
                    pos = tagEnd + 1;

                    // 深度回到 0，说明顶层元素闭合
                    if (depth == 0 && fragmentStart >= 0)
                    {
                        return (text.Substring(fragmentStart, pos - fragmentStart), pos);
                    }

                    continue;
                }

                // CDATA <![CDATA[ ]]>
                if (next == '!' && pos + 8 < text.Length && text.AsSpan(pos, 9).Equals("<![CDATA[", StringComparison.Ordinal))
                {
                    var cdataEnd = FindSequence(text, pos + 9, "]]>");
                    if (cdataEnd < 0)
                    {
                        return (null, startPos);
                    }

                    pos = cdataEnd + 3;
                    continue;
                }

                // 开始标签
                if (char.IsLetter(next) || next == '_')
                {
                    if (depth == 0)
                    {
                        fragmentStart = pos;
                    }

                    // 读取标签直到 > 或 />
                    var (isSelfClosing, tagEndPos) = ScanTag(text, pos);

                    if (tagEndPos < 0)
                    {
                        // 标签不完整
                        return (null, startPos);
                    }

                    if (isSelfClosing)
                    {
                        // 自闭合标签，深度不变
                        if (depth == 0 && fragmentStart >= 0)
                        {
                            pos = tagEndPos + 1;
                            return (text.Substring(fragmentStart, pos - fragmentStart), pos);
                        }
                    }
                    else
                    {
                        depth++;
                    }

                    pos = tagEndPos + 1;
                    continue;
                }

                // 其他 < 情况（不应该出现），跳过
                pos++;
                continue;
            }

            // 非 < 字符，在深度 0 时跳过（片段之间的空白等）
            if (depth == 0)
            {
                pos++;
                continue;
            }

            // 在元素内部，跳过文本内容
            pos++;
        }

        // 扫描完毕但深度未归零，说明片段不完整
        return (null, startPos);
    }

    /// <summary>
    /// 扫描一个 XML 标签，确定是否自闭合以及结束位置。
    /// </summary>
    /// <param name="text">文本。</param>
    /// <param name="tagStart">标签起始位置（指向 '<'）。</param>
    /// <returns>是否自闭合和 '>' 的位置（-1 表示标签不完整）。</returns>
    private static (bool isSelfClosing, int tagEndPos) ScanTag(string text, int tagStart)
    {
        var pos = tagStart + 1;
        var inSingleQuote = false;
        var inDoubleQuote = false;

        while (pos < text.Length)
        {
            var ch = text[pos];

            if (inSingleQuote)
            {
                if (ch == '\'')
                {
                    inSingleQuote = false;
                }

                pos++;
                continue;
            }

            if (inDoubleQuote)
            {
                if (ch == '"')
                {
                    inDoubleQuote = false;
                }

                pos++;
                continue;
            }

            if (ch == '\'')
            {
                inSingleQuote = true;
                pos++;
                continue;
            }

            if (ch == '"')
            {
                inDoubleQuote = true;
                pos++;
                continue;
            }

            if (ch == '>')
            {
                // 检查是否为 />
                if (pos > tagStart + 1 && text[pos - 1] == '/')
                {
                    return (true, pos);
                }

                return (false, pos);
            }

            pos++;
        }

        // 标签不完整
        return (false, -1);
    }

    /// <summary>
    /// 在文本中查找指定子串，返回起始位置。
    /// </summary>
    /// <param name="text">搜索文本。</param>
    /// <param name="start">起始位置。</param>
    /// <param name="sequence">要查找的子串。</param>
    /// <returns>子串起始位置，未找到返回 -1。</returns>
    private static int FindSequence(string text, int start, string sequence)
    {
        return text.IndexOf(sequence, start, StringComparison.Ordinal);
    }
}
