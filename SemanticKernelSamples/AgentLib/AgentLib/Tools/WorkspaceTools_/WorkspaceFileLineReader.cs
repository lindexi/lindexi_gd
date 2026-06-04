using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AgentLib.Tools;

/// <summary>
/// 辅助结构体，负责读取文件指定行范围的核心逻辑，包括字符截断、剩余行统计和结果格式化。
/// </summary>
/// <param name="maxCharacters">单次读取最大字符数限制。</param>
/// <param name="maxRemainingLinesToCount">最多统计的剩余行数上限。</param>
public readonly struct WorkspaceFileLineReader(int maxCharacters, int maxRemainingLinesToCount)
{
    private const int BufferSize = 4096;
    private readonly int _maxCharacters = maxCharacters;
    private readonly int _maxRemainingLinesToCount = maxRemainingLinesToCount;

    /// <summary>
    /// 读取文件的指定行范围，并返回格式化后的字符串结果。
    /// </summary>
    /// <param name="file">要读取的文件。</param>
    /// <param name="startLine">起始行号（1-based）。</param>
    /// <param name="endLine">结束行号（1-based）。</param>
    /// <param name="includeLineNumbers">是否在每行前添加行号。</param>
    /// <param name="displayPath">用于获取文件显示路径。</param>
    /// <returns>格式化后的结果字符串。</returns>
    public async Task<string> ReadAsync
    (
        FileInfo file,
        int startLine,
        int endLine,
        bool includeLineNumbers,
        string displayPath
    )
    {
        var contentBuilder = new StringBuilder();
        var leftover = new StringBuilder();
        int actualEndLine = startLine - 1;
        bool hasContent = false;
        bool reachedCharacterLimit = false;

        char[]? rentedBuffer = ArrayPool<char>.Shared.Rent(BufferSize);
        try
        {
            using var reader = new StreamReader(file.FullName, detectEncodingFromByteOrderMarks: true);
            Memory<char> buffer = rentedBuffer;

            // 阶段一：读取请求的行范围
            int currentLine = 0;
            bool contentPhaseComplete = false;

            while (!contentPhaseComplete)
            {
                int charsRead = await reader.ReadAsync(buffer).ConfigureAwait(false);
                if (charsRead == 0)
                {
                    // 文件末尾，处理可能残留的不完整行
                    if (leftover.Length > 0)
                    {
                        currentLine++;
                        if (currentLine >= startLine && currentLine <= endLine)
                        {
                            TryAppendLine(leftover.ToString(), currentLine, includeLineNumbers,
                                contentBuilder, ref hasContent, ref actualEndLine, ref reachedCharacterLimit);
                        }
                        leftover.Clear();
                    }
                    contentPhaseComplete = true;
                    break;
                }

                ReadOnlySpan<char> window = rentedBuffer.AsSpan(0, charsRead);

                while (true)
                {
                    int newlineIndex = window.IndexOf('\n');
                    if (newlineIndex < 0)
                    {
                        leftover.Append(window);
                        break;
                    }

                    ReadOnlySpan<char> lineSpan = window[..newlineIndex];
                    if (lineSpan.Length > 0 && lineSpan[^1] == '\r')
                    {
                        lineSpan = lineSpan[..^1];
                    }

                    currentLine++;

                    if (currentLine >= startLine && currentLine <= endLine)
                    {
                        string line;
                        if (leftover.Length > 0)
                        {
                            leftover.Append(lineSpan);
                            line = leftover.ToString();
                            leftover.Clear();
                        }
                        else
                        {
                            line = lineSpan.ToString();
                        }

                        if (!TryAppendLine(line, currentLine, includeLineNumbers,
                                contentBuilder, ref hasContent, ref actualEndLine, ref reachedCharacterLimit))
                        {
                            contentPhaseComplete = true;
                            break;
                        }

                        if (currentLine == endLine)
                        {
                            // 将该行之后的内容推入 leftover，供阶段二统计
                            leftover.Append(window[(newlineIndex + 1)..]);
                            contentPhaseComplete = true;
                            break;
                        }
                    }

                    window = window[(newlineIndex + 1)..];
                }
            }

            // 阶段二：统计剩余行数（仅在未因字符限制中断时进行）
            RemainingLinesResult remainingResult;
            if (!reachedCharacterLimit && currentLine >= endLine)
            {
                remainingResult = await CountRemainingLinesAsync(
                    reader, rentedBuffer, leftover).ConfigureAwait(false);
            }
            else
            {
                remainingResult = default;
            }

            // 格式化输出
            return BuildResult(startLine, actualEndLine, hasContent, reachedCharacterLimit,
                remainingResult, contentBuilder, displayPath);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rentedBuffer);
        }
    }

    /// <summary>
    /// 从当前读取位置开始统计剩余行数。先处理 <paramref name="leftover"/> 中的残留内容，
    /// 再从 <paramref name="reader"/> 继续读取。
    /// </summary>
    private async Task<RemainingLinesResult> CountRemainingLinesAsync(
        StreamReader reader,
        char[] rentedBuffer,
        StringBuilder leftover)
    {
        int count = 0;
        Memory<char> buffer = rentedBuffer;

        // 先处理 leftover 中已有的残留内容（由阶段一推入）
        if (leftover.Length > 0)
        {
            string leftoverText = leftover.ToString();
            leftover.Clear();
            ReadOnlySpan<char> span = leftoverText.AsSpan();

            while (true)
            {
                int newlineIndex = span.IndexOf('\n');
                if (newlineIndex < 0)
                {
                    // 不完整行，保留到 leftover 中后续继续处理
                    leftover.Append(span);
                    break;
                }

                count++;

                if (count > _maxRemainingLinesToCount)
                {
                    return new RemainingLinesResult { ExceedsLimit = true };
                }

                span = span[(newlineIndex + 1)..];
            }
        }

        while (count <= _maxRemainingLinesToCount)
        {
            int charsRead = await reader.ReadAsync(buffer).ConfigureAwait(false);
            if (charsRead == 0)
            {
                if (leftover.Length > 0)
                {
                    count++;
                    leftover.Clear();
                }

                if (count > _maxRemainingLinesToCount)
                {
                    return new RemainingLinesResult { ExceedsLimit = true };
                }

                return new RemainingLinesResult { Count = count, ReachedEndOfFile = true };
            }

            ReadOnlySpan<char> window = rentedBuffer.AsSpan(0, charsRead);

            while (true)
            {
                int newlineIndex = window.IndexOf('\n');
                if (newlineIndex < 0)
                {
                    leftover.Append(window);
                    break;
                }

                if (leftover.Length > 0)
                {
                    leftover.Clear();
                }

                count++;

                if (count > _maxRemainingLinesToCount)
                {
                    return new RemainingLinesResult { ExceedsLimit = true };
                }

                window = window[(newlineIndex + 1)..];
            }
        }

        return new RemainingLinesResult { ExceedsLimit = true };
    }

    /// <summary>
    /// 组装最终的输出字符串。
    /// </summary>
    private string BuildResult
    (
        int startLine,
        int actualEndLine,
        bool hasContent,
        bool reachedCharacterLimit,
        RemainingLinesResult remainingResult,
        StringBuilder contentBuilder,
        string displayPath
    )
    {
        var builder = new StringBuilder();
        builder.AppendLine("<MetaData>");
        builder.AppendLine($"文件: {displayPath}");

        string actualRangeText = hasContent ? $"{startLine}-{actualEndLine}" : "无";

        bool reachedEndOfFile;
        int? remainingLines;
        bool remainingLinesExceedsLimit;

        if (reachedCharacterLimit)
        {
            reachedEndOfFile = false;
            remainingLines = null;
            remainingLinesExceedsLimit = false;
        }
        else if (remainingResult.ExceedsLimit)
        {
            reachedEndOfFile = false;
            remainingLines = null;
            remainingLinesExceedsLimit = true;
        }
        else if (remainingResult.Count > 0)
        {
            reachedEndOfFile = false;
            remainingLines = remainingResult.Count;
            remainingLinesExceedsLimit = false;
        }
        else
        {
            reachedEndOfFile = true;
            remainingLines = null;
            remainingLinesExceedsLimit = false;
        }

        string statusSuffix = GetStatusSuffix(reachedCharacterLimit, reachedEndOfFile, remainingLinesExceedsLimit, remainingLines);
        builder.AppendLine($"行范围: {actualRangeText}{statusSuffix}");
        builder.AppendLine("</MetaData>");

        if (!hasContent)
        {
            builder.Append("指定范围内没有可读取的内容。");
        }
        else
        {
            builder.Append(contentBuilder.ToString());

            if (reachedCharacterLimit)
            {
                builder.Append($" 【已达到最大字符数限制 {_maxCharacters}，后续内容未显示。】");
            }
        }

        return builder.ToString();
    }

    private struct RemainingLinesResult
    {
        public int Count;
        public bool ExceedsLimit;
        public bool ReachedEndOfFile;
    }

    /// <summary>
    /// 尝试将一行追加到 contentBuilder。如果追加后超过字符限制则截断并返回 false。
    /// </summary>
    private bool TryAppendLine(
        string line,
        int currentLine,
        bool includeLineNumbers,
        StringBuilder contentBuilder,
        ref bool hasContent,
        ref int actualEndLine,
        ref bool reachedCharacterLimit)
    {
        string outputLine = includeLineNumbers ? $"{currentLine}: {line}" : line;
        int appendedLength = outputLine.Length + Environment.NewLine.Length;
        if (contentBuilder.Length + appendedLength > _maxCharacters)
        {
            int remainingCharacters = _maxCharacters - contentBuilder.Length;
            if (remainingCharacters > 0)
            {
                int lineCharactersToAppend = Math.Min(outputLine.Length, remainingCharacters);
                if (lineCharactersToAppend > 0)
                {
                    contentBuilder.Append(outputLine.AsSpan(0, lineCharactersToAppend));
                    hasContent = true;
                    actualEndLine = currentLine;
                }
            }

            reachedCharacterLimit = true;
            return false;
        }

        if (hasContent)
        {
            contentBuilder.Append(Environment.NewLine);
        }

        contentBuilder.Append(outputLine);
        hasContent = true;
        actualEndLine = currentLine;
        return true;
    }

    private string GetStatusSuffix(
        bool reachedCharacterLimit,
        bool reachedEndOfFile,
        bool remainingLinesExceedsLimit,
        int? remainingLines)
    {
        if (reachedCharacterLimit)
        {
            return "【超长截断】";
        }

        if (reachedEndOfFile)
        {
            return "【已读完】";
        }

        if (remainingLinesExceedsLimit)
        {
            return $"【剩余大于 {_maxRemainingLinesToCount} 行未读取】";
        }

        if (remainingLines.HasValue && remainingLines.Value > 0)
        {
            return $"【剩余 {remainingLines.Value} 行未读取】";
        }

        return "";
    }
}
