using System;

namespace SimpleWrite.Business.TextEditors.AutoIndentStrategies;

/// <summary>
/// Markdown 文档的自动缩进策略。仅复制当前行的前导空格。
/// </summary>
public class MarkdownAutoIndentStrategy : IAutoIndentStrategy
{
    /// <inheritdoc />
    public string GetIndentText(string currentLineText, int caretColumnInLine)
    {
        ArgumentNullException.ThrowIfNull(currentLineText);

        var leadingCount = CountLeadingWhitespace(currentLineText);
        if (leadingCount == 0)
        {
            return string.Empty;
        }

        // 只取光标之前的前导空格部分
        var effectiveCount = Math.Min(leadingCount, caretColumnInLine);
        return currentLineText[..effectiveCount];
    }

    /// <summary>
    /// 计算字符串开头连续空白字符的数量。
    /// </summary>
    private static int CountLeadingWhitespace(string text)
    {
        var count = 0;
        foreach (var c in text)
        {
            if (c is ' ' or '\t')
            {
                count++;
            }
            else
            {
                break;
            }
        }

        return count;
    }
}
