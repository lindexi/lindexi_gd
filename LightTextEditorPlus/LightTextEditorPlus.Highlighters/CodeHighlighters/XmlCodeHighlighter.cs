using System;

using Microsoft.CodeAnalysis.Text;

namespace LightTextEditorPlus.Highlighters.CodeHighlighters;

/// <summary>
/// 为 XML 文本提供基于语法结构的高亮。
/// </summary>
internal sealed class XmlCodeHighlighter
{
    /// <summary>
    /// 尝试按 XML 语义应用高亮。
    /// </summary>
    /// <param name="context">代码内容与着色输出上下文。</param>
    /// <returns>识别并完成 XML 高亮时返回 <see langword="true"/>。</returns>
    public bool TryApplyHighlight(in HighlightCodeContext context)
    {
        string code = context.PlainCode;
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        var hasHighlight = false;
        var index = 0;
        while (index < code.Length)
        {
            if (TryHighlightComment(code, ref index, context.ColorCode))
            {
                hasHighlight = true;
                continue;
            }

            if (TryHighlightCData(code, ref index, context.ColorCode))
            {
                hasHighlight = true;
                continue;
            }

            if (TryHighlightTag(code, ref index, context.ColorCode))
            {
                hasHighlight = true;
                continue;
            }

            index++;
        }

        return hasHighlight;
    }

    private static bool TryHighlightComment(string code, ref int index, IColorCode colorCode)
    {
        if (!code.AsSpan(index).StartsWith("<!--", StringComparison.Ordinal))
        {
            return false;
        }

        var end = code.IndexOf("-->", index + 4, StringComparison.Ordinal);
        if (end < 0)
        {
            HighlightSpan(colorCode, index, code.Length - index, ScopeType.Comment);
            index = code.Length;
            return true;
        }

        HighlightSpan(colorCode, index, end + 3 - index, ScopeType.Comment);
        index = end + 3;
        return true;
    }

    private static bool TryHighlightCData(string code, ref int index, IColorCode colorCode)
    {
        if (!code.AsSpan(index).StartsWith("<![CDATA[", StringComparison.Ordinal))
        {
            return false;
        }

        var end = code.IndexOf("]]>", index + 9, StringComparison.Ordinal);
        if (end < 0)
        {
            HighlightSpan(colorCode, index, code.Length - index, ScopeType.DeclarationTypeSyntax);
            index = code.Length;
            return true;
        }

        HighlightSpan(colorCode, index, end + 3 - index, ScopeType.DeclarationTypeSyntax);
        index = end + 3;
        return true;
    }

    private static bool TryHighlightTag(string code, ref int index, IColorCode colorCode)
    {
        if (code[index] != '<')
        {
            return false;
        }

        var currentIndex = index + 1;
        if (currentIndex >= code.Length)
        {
            return false;
        }

        if (code[currentIndex] is '!' or '<')
        {
            return false;
        }

        if (code[currentIndex] == '/')
        {
            currentIndex++;
            SkipWhitespace(code, ref currentIndex);
            HighlightName(code, ref currentIndex, colorCode);
            MoveToTagEnd(code, ref currentIndex);
            index = currentIndex;
            return true;
        }

        if (code[currentIndex] == '?')
        {
            currentIndex++;
            SkipWhitespace(code, ref currentIndex);
            HighlightName(code, ref currentIndex, colorCode);
            HighlightAttributes(code, ref currentIndex, colorCode, processingInstruction: true);
            index = currentIndex;
            return true;
        }

        HighlightName(code, ref currentIndex, colorCode);
        HighlightAttributes(code, ref currentIndex, colorCode, processingInstruction: false);
        index = currentIndex;
        return true;
    }

    private static void HighlightAttributes(string code, ref int index, IColorCode colorCode, bool processingInstruction)
    {
        while (index < code.Length)
        {
            SkipWhitespace(code, ref index);
            if (index >= code.Length)
            {
                return;
            }

            if (processingInstruction)
            {
                if (code[index] == '?' && index + 1 < code.Length && code[index + 1] == '>')
                {
                    index += 2;
                    return;
                }
            }
            else
            {
                if (code[index] == '>')
                {
                    index++;
                    return;
                }

                if (code[index] == '/' && index + 1 < code.Length && code[index + 1] == '>')
                {
                    index += 2;
                    return;
                }
            }

            if (!HighlightName(code, ref index, colorCode))
            {
                index++;
                continue;
            }

            SkipWhitespace(code, ref index);
            if (index >= code.Length || code[index] != '=')
            {
                continue;
            }

            index++;
            SkipWhitespace(code, ref index);
            HighlightAttributeValue(code, ref index, colorCode);
        }
    }

    private static bool HighlightName(string code, ref int index, IColorCode colorCode)
    {
        var start = index;
        while (index < code.Length && IsXmlNameChar(code[index]))
        {
            index++;
        }

        var length = index - start;
        if (length <= 0)
        {
            return false;
        }

        HighlightSpan(colorCode, start, length, ScopeType.ClassMember);
        return true;
    }

    private static void HighlightAttributeValue(string code, ref int index, IColorCode colorCode)
    {
        if (index >= code.Length)
        {
            return;
        }

        var quote = code[index];
        if (quote is not ('"' or '\''))
        {
            return;
        }

        var start = index;
        index++;
        while (index < code.Length && code[index] != quote)
        {
            index++;
        }

        if (index < code.Length)
        {
            index++;
        }

        HighlightSpan(colorCode, start, index - start, ScopeType.String);
    }

    private static void MoveToTagEnd(string code, ref int index)
    {
        while (index < code.Length)
        {
            if (code[index] == '>')
            {
                index++;
                return;
            }

            index++;
        }
    }

    private static void SkipWhitespace(string code, ref int index)
    {
        while (index < code.Length && char.IsWhiteSpace(code[index]))
        {
            index++;
        }
    }

    private static bool IsXmlNameChar(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch is '_' or ':' or '-' or '.';
    }

    private static void HighlightSpan(IColorCode colorCode, int start, int length, ScopeType scopeType)
    {
        if (length <= 0)
        {
            return;
        }

        colorCode.FillCodeColor(new TextSpan(start, length), scopeType);
    }
}
