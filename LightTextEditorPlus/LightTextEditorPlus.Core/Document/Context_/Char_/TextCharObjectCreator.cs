using System.Collections.Generic;
using System.Text;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 文本字符创建器
/// </summary>
internal static class TextCharObjectCreator
{
    public static List<ICharObject> TextToCharObjectList(string text)
    {
        // 特殊处理的情况有：
        // 1. 表情符号
        // 2. 换行字符。包括 \r\n 和 \n 和 \r 的情况
        // 3. 空字符串
        if (string.IsNullOrEmpty(text))
        {
            return new List<ICharObject>();
        }

        var charObjectList = new List<ICharObject>(text.Length);
        var isLastCharCarriageReturn = false;// 上一个字符是否 \r 字符
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (rune.Value is '\r')
            {
                isLastCharCarriageReturn = true;
                charObjectList.Add(LineBreakCharObject.Instance);
                continue;
            }
            else if (rune.Value is '\n')
            {
                // 需要判断上一个字符是否 \r 字符
                if (isLastCharCarriageReturn)
                {
                    // 啥都不干，跳过 \r\n 的情况，只在 \r 时添加换行符
                }
                else
                {
                    // 单个 \n 字符的情况
                    charObjectList.Add(LineBreakCharObject.Instance);
                }
            }
            else
            {
                /*
                ## Paragraph segmentation

                   The coarsest, and also simplest, segmentation task is paragraph segmentation. Most of the time, paragraphs are simply separated by newline (U+000A) characters, though Unicode in its infinite wisdom specifies a number of code point sequences that function as paragraph separators in plain text:

                   U+000A LINE FEED
                   U+000B VERTICAL TAB
                   U+000C FORM FEED
                   U+000D CARRIAGE RETURN
                   U+000D U+000A (CR + LF)
                   U+0085 NEXT LINE
                   U+2008 LINE SEPARATOR
                   U+2009 PARAGRAPH SEPARATOR

                By https://raphlinus.github.io/text/2020/10/26/text-layout.html
                 */
                charObjectList.Add(new RuneCharObject(rune));
            }

            isLastCharCarriageReturn = false;
        }

        return charObjectList;
    }

    /// <summary>
    /// 根据传入的字符串创建字符对象
    /// </summary>
    /// <param name="text">传入的字符串</param>
    /// <param name="charIndex">字符的起始点</param>
    /// <param name="charCount">表示一个字符所使用的 char 数量。对于一些 表情 符号，需要两个 char 才能表示</param>
    /// <returns></returns>
    public static ICharObject CreateCharObject(string text, int charIndex, int charCount = 1)
    {
        if (charCount == 1)
        {
            return new SingleCharObject(text[charIndex]);
        }
        else
        {
            return new TextSpanCharObject(text, charIndex, charCount);
        }
    }
}
