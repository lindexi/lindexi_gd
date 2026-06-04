using System;

namespace SimpleWrite.Business.TextEditors.AutoIndentStrategies;

/// <summary>
/// XML 文档的自动缩进策略。复制当前行前导空格，并根据标签嵌套层级增加或减少缩进。
/// </summary>
public class XmlAutoIndentStrategy : IAutoIndentStrategy
{
    /// <summary>
    /// 默认缩进空格数。
    /// </summary>
    public const int DefaultIndentSize = 4;

    /// <summary>
    /// 初始化 <see cref="XmlAutoIndentStrategy"/> 实例。
    /// </summary>
    /// <param name="indentSize">每次缩进增加的空格数，默认为 4。</param>
    public XmlAutoIndentStrategy(int indentSize = DefaultIndentSize)
    {
        if (indentSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(indentSize), "Indent size must be non-negative.");
        }

        IndentSize = indentSize;
    }

    /// <summary>
    /// 每次缩进增加的空格数。
    /// </summary>
    public int IndentSize { get; }

    /// <inheritdoc />
    public string GetIndentText(string currentLineText, int caretColumnInLine)
    {
        ArgumentNullException.ThrowIfNull(currentLineText);

        var leadingCount = CountLeadingWhitespace(currentLineText);
        var effectiveLeadingCount = Math.Min(leadingCount, caretColumnInLine);

        // 分析光标之前的文本中的标签嵌套层级
        var textBeforeCaret = currentLineText[..Math.Min(caretColumnInLine, currentLineText.Length)];
        var netTagLevel = CalculateNetTagLevel(textBeforeCaret);

        var totalIndent = effectiveLeadingCount + netTagLevel * IndentSize;
        if (totalIndent <= 0)
        {
            return string.Empty;
        }

        return new string(' ', totalIndent);
    }

    /// <summary>
    /// 计算文本中未闭合标签的净层级差（开标签数 - 闭标签数）。
    /// </summary>
    private static int CalculateNetTagLevel(string text)
    {
        var netLevel = 0;
        var i = 0;

        while (i < text.Length)
        {
            if (text[i] == '<')
            {
                if (i + 1 >= text.Length)
                {
                    break;
                }

                var next = text[i + 1];
                if (next == '/')
                {
                    // 闭合标签 </tag>
                    netLevel--;
                    i += 2;
                }
                else if (next is '?' or '!')
                {
                    // 处理指令或注释 <?xml ...?> 或 <!-- ... -->
                    i += 2;
                }
                else
                {
                    // 开标签 <tag>
                    // 检查是否为自闭合标签 <tag />
                    var slashIndex = text.IndexOf('/', i + 1);
                    var closeIndex = text.IndexOf('>', i + 1);
                    if (closeIndex > 0 && (slashIndex < 0 || slashIndex > closeIndex || slashIndex != closeIndex - 1))
                    {
                        // 不是自闭合，且不是以 /> 结尾
                        if (slashIndex < 0 || slashIndex > closeIndex)
                        {
                            netLevel++;
                        }
                    }

                    i = closeIndex > 0 ? closeIndex + 1 : i + 1;
                }
            }
            else
            {
                i++;
            }
        }

        return netLevel;
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
