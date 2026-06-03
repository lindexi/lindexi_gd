using System;
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
    private readonly int _maxCharacters = maxCharacters;
    private readonly int _maxRemainingLinesToCount = maxRemainingLinesToCount;

    /// <summary>
    /// 读取文件的指定行范围，并返回格式化后的字符串结果。
    /// </summary>
    /// <param name="file">要读取的文件。</param>
    /// <param name="startLine">起始行号（1-based）。</param>
    /// <param name="endLine">结束行号（1-based）。</param>
    /// <param name="includeLineNumbers">是否在每行前添加行号。</param>
    /// <param name="getDisplayPath">用于获取文件显示路径的委托。</param>
    /// <returns>格式化后的结果字符串。</returns>
    public async Task<string> ReadAsync(
        FileInfo file,
        int startLine,
        int endLine,
        bool includeLineNumbers,
        Func<string, string> getDisplayPath)
    {
        var builder = new StringBuilder();
        int currentLine = 0;
        bool hasContent = false;
        bool reachedCharacterLimit = false;
        bool reachedEndOfFile = true;
        int actualEndLine = startLine - 1;
        var contentBuilder = new StringBuilder();

        using var reader = new StreamReader(file.FullName, detectEncodingFromByteOrderMarks: true);

        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            currentLine++;
            if (currentLine < startLine)
            {
                continue;
            }

            if (currentLine > endLine)
            {
                reachedEndOfFile = false;
                break;
            }

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
                break;
            }

            if (hasContent)
            {
                contentBuilder.Append(Environment.NewLine);
            }

            contentBuilder.Append(outputLine);
            hasContent = true;
            actualEndLine = currentLine;

            // 读完请求范围后立即跳出，避免消耗下一行
            if (currentLine == endLine)
            {
                // 尝试读取下一行来判断是否还有剩余内容
                if (await reader.ReadLineAsync().ConfigureAwait(false) is null)
                {
                    // 后面没有行了，文件已读完
                    reachedEndOfFile = true;
                }
                else
                {
                    reachedEndOfFile = false;
                }
                break;
            }
        }

        // 统计剩余行数（如果已读取完用户请求的范围且未因字符限制中断）
        int? remainingLines = null;
        bool remainingLinesExceedsLimit = false;
        if (!reachedCharacterLimit && !reachedEndOfFile)
        {
            // 此时已消耗了一行（用于判断是否有剩余），从 1 开始计数
            int countedRemainingLines = 1;
            while (countedRemainingLines < _maxRemainingLinesToCount
                   && await reader.ReadLineAsync().ConfigureAwait(false) is not null)
            {
                countedRemainingLines++;
            }

            if (countedRemainingLines == _maxRemainingLinesToCount)
            {
                // 检查是否还有更多行
                if (await reader.ReadLineAsync().ConfigureAwait(false) is not null)
                {
                    remainingLinesExceedsLimit = true;
                }
                else
                {
                    remainingLines = countedRemainingLines;
                }
            }
            else
            {
                remainingLines = countedRemainingLines;
            }
        }

        builder.AppendLine("<MetaData>");
        builder.AppendLine($"文件: {getDisplayPath(file.FullName)}");
        string actualRangeText = hasContent ? $"{startLine}-{actualEndLine}" : "无";
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
