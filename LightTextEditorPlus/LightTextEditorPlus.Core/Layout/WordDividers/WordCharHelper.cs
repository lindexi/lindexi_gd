﻿using System;
using System.Collections.Generic;
using System.Text;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils.Patterns;

namespace LightTextEditorPlus.Core.Layout.WordDividers;

internal static class WordCharHelper
{
    public static IEnumerable<string> TraversalSplit(ReadOnlyListSpan<CharData> runList, int currentIndex = 0)
    {
        foreach ((int start, int count) in Traversal(runList, currentIndex))
        {
            var stringBuilder = new StringBuilder();
            foreach (CharData charData in runList.Slice(start,count))
            {
                stringBuilder.Append(charData.CharObject.ToText());
            }

            yield return stringBuilder.ToString();
        }
    }

    public static IEnumerable<(int Start, int Count)> Traversal(ReadOnlyListSpan<CharData> runList, int currentIndex = 0)
    {
        while (currentIndex < runList.Count)
        {
            var count = ReadWordCharCount(runList, currentIndex);
            yield return (currentIndex, count);
            currentIndex += count;
        }
    }

    public static int ReadWordCharCount(ReadOnlyListSpan<CharData> runList, int currentIndex)
    {
        // 读取当前的字符所在的单词的字符数量
        if (TryReadWordCharCount(CheckSpace, out var count)
            || TryReadWordCharCount(CheckEnglish, out count)
            || TryReadWordCharCount(CheckNumber, out count)
            || TryReadWordCharCount(CheckTibetan, out count)
            || TryReadWordCharCount(CheckMongolian, out count))
        {
            return count;
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

    private static bool CheckMongolian(CharData charData)
    {
        string currentCharText = charData.CharObject.ToText();
        return RegexPatterns.MongolianPattern.AreAllInRange(currentCharText);
    }

    private static bool CheckTibetan(CharData charData)
    {
        string currentCharText = charData.CharObject.ToText();
        return RegexPatterns.TibetanPattern.AreAllInRange(currentCharText);
    }

    private static bool CheckNumber(CharData charData)
    {
        // todo 后续优化 ToText 调用多次
        string currentCharText = charData.CharObject.ToText();
        return RegexPatterns.NumberPattern.AreAllInRange(currentCharText) || IsAllPoint(currentCharText);

        static bool IsAllPoint(string charText)
        {
            for (var i = 0; i < charText.Length; i++)
            {
                if (charText[i] != '.')
                {
                    return false;
                }
            }

            return true;
        }
    }

    private static bool CheckEnglish(CharData charData)
    {
        string currentCharText = charData.CharObject.ToText();
        bool isLatin = RegexPatterns.EnglishLetterPattern.AreAllInRange(currentCharText);
        return isLatin;
    }

    private static bool CheckSpace(CharData charData)
    {
        // todo 后续考虑多空格问题
        string currentCharText = charData.CharObject.ToText();
        return currentCharText == RegexPatterns.BlankSpace;
    }
}