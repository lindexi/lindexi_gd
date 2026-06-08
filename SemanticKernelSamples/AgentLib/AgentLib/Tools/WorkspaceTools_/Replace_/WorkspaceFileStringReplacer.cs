using System;

namespace AgentLib.Tools;

/// <summary>
/// 负责对文件内容执行字符串替换，保证 oldString 在内容中唯一匹配。
/// 仅处理内容替换逻辑，不涉及路径解析、快照校验或文件读写。
/// </summary>
public sealed class WorkspaceFileStringReplacer
{
    /// <summary>
    /// 在文件内容中查找 <paramref name="oldString"/> 并替换为 <paramref name="newString"/>。
    /// <paramref name="oldString"/> 必须在内容中恰好出现一次，否则返回失败结果。
    /// </summary>
    /// <param name="content">文件的完整内容。</param>
    /// <param name="oldString">要替换的原始文本。</param>
    /// <param name="newString">替换后的新文本。</param>
    /// <param name="displayPath">用于结果信息中显示的文件路径。</param>
    /// <returns>替换操作的结果，包含是否成功、消息以及替换后的新内容。</returns>
    public StringReplaceOutcome ReplaceInContent(string content, string oldString, string newString, string displayPath)
    {
        int occurrenceCount = CountOccurrences(content, oldString);

        if (occurrenceCount == 0)
        {
            return new StringReplaceOutcome(
                Success: false,
                Message: $"在文件 {displayPath} 中未找到要替换的文本: {oldString}。请使用 ReadFileLines 重新读取文件内容，确认 oldString 与文件内容完全一致（包括空白和缩进）。",
                NewContent: null);
        }

        if (occurrenceCount > 1)
        {
            return new StringReplaceOutcome(
                Success: false,
                Message: $"在文件 {displayPath} 中找到 {occurrenceCount} 处匹配。oldString 必须在文件中唯一匹配，请添加更多上下文（前后各 3-5 行）使匹配唯一。",
                NewContent: null);
        }

        string newContent = content.Replace(oldString, newString, StringComparison.Ordinal);
        return new StringReplaceOutcome(
            Success: true,
            Message: "OK",
            NewContent: newContent);
    }

    private static int CountOccurrences(string content, string value)
    {
        int count = 0;
        int index = 0;

        while (index <= content.Length - value.Length)
        {
            int found = content.IndexOf(value, index, StringComparison.Ordinal);
            if (found < 0)
            {
                break;
            }

            count++;
            index = found + value.Length;
        }

        return count;
    }
}

/// <summary>
/// 表示字符串替换操作的内部结果，包含是否成功、消息以及替换后的新内容。
/// </summary>
/// <param name="Success">替换操作是否成功。</param>
/// <param name="Message">操作结果描述信息。</param>
/// <param name="NewContent">替换后的新内容；失败时为 null。</param>
public readonly record struct StringReplaceOutcome(bool Success, string Message, string? NewContent);
