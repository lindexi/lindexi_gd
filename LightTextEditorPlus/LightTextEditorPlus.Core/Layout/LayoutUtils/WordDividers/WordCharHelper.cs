using System;
using System.Collections.Generic;
using System.Text;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils.Patterns;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

internal static class WordCharHelper
{
    public static IEnumerable<string> TraversalSplit(TextReadOnlyListSpan<CharData> runList, int currentIndex = 0)
    {
        foreach ((int start, int count) in Traversal(runList, currentIndex))
        {
            var stringBuilder = new StringBuilder();
            foreach (CharData charData in runList.Slice(start, count))
            {
                stringBuilder.Append(charData.CharObject.ToText());
            }

            yield return stringBuilder.ToString();
        }
    }

    public static IEnumerable<(int Start, int Count)> Traversal(TextReadOnlyListSpan<CharData> runList, int currentIndex = 0)
    {
        while (currentIndex < runList.Count)
        {
            var count = ReadWordCharCount(runList, currentIndex);
            yield return (currentIndex, count);
            currentIndex += count;
        }
    }

    public static int ReadWordCharCount
        (TextReadOnlyListSpan<CharData> runList, int currentIndex, UpdateLayoutContext? context = null)
    {
        if (currentIndex >= runList.Count)
        {
            // 读到不能继续读取了，返回 0 个字符
            return 0;
        }

        // 读取当前的字符所在的单词的字符数量
        if (TryReadWordCharCount(CheckSpace, out var count)
            || TryReadWordCharCount(CheckEnglish, out count)
            || TryReadWordCharCount(CheckNumber, out count)
            || TryReadWordCharCount(CheckTibetan, out count)
            || TryReadWordCharCount(CheckMongolian, out count))
        {
            return count;
        }

        if (context != null && context.TextEditor.ArrangingType == ArrangingType.Mongolian)
        {
            // 如果是蒙文排版模式，则读取蒙科立蒙古文字符。这是由于蒙科立编码是在自定义编码区，只有猜，不能有明确的范围
            // 只有使用特殊的蒙古文字体才能正确显示
            if (TryReadWordCharCount(CheckMenksoftMongolian, out count))
            {
                return count;
            }
        }

        // 至少能够读取出当前的一个字符
        return 1;

        bool TryReadWordCharCount(Predicate<CharData> match, out int charCount)
        {
            CharData currentCharData = runList[currentIndex];
            if (!match(currentCharData))
            {
                charCount = 0;
                return false;
            }

            charCount = 1;

            var index = currentIndex + 1;// 跳过当前，继续读取

            for (; index < runList.Count; index++)
            {
                currentCharData = runList[index];
                if (!match(currentCharData))
                {
                    break;
                }
            }

            charCount = index - currentIndex;

            return true;
        }
    }

    /// <summary>
    /// 判断是否蒙科立蒙古文字符
    /// </summary>
    /// <param name="charData"></param>
    /// <returns></returns>
    private static bool CheckMenksoftMongolian(CharData charData)
    {
        return RegexPatterns.MenksoftMongolian.IsInRange(charData);
    }

    /// <summary>
    /// 判断是否蒙古文字符
    /// </summary>
    /// <param name="charData"></param>
    /// <returns></returns>
    private static bool CheckMongolian(CharData charData)
    {
        return RegexPatterns.MongolianPattern.IsInRange(charData);
    }

    private static bool CheckTibetan(CharData charData)
    {
        return RegexPatterns.TibetanPattern.IsInRange(charData);
    }

    private static bool CheckNumber(CharData charData)
    {
        return RegexPatterns.NumberPattern.IsInRange(charData) || IsPoint(charData);

        static bool IsPoint(CharData charData)
        {
            return charData.CharObject.CodePoint.Equals('.');
        }

        //static bool IsAllPoint(string charText)
        //{
        //    for (var i = 0; i < charText.Length; i++)
        //    {
        //        if (charText[i] != '.')
        //        {
        //            return false;
        //        }
        //    }

        //    return true;
        //}
    }

    private static bool CheckEnglish(CharData charData)
    {
        bool isLatin = RegexPatterns.EnglishLetterPattern.IsInRange(charData.CharObject.CodePoint);
        return isLatin;
    }

    private static bool CheckSpace(CharData charData)
    {
        // todo 后续考虑多空格问题
        return charData.CharObject.CodePoint.Equals(RegexPatterns.BlankSpaceChar);
    }
}
