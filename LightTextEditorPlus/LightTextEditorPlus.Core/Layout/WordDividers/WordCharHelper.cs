using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils.Patterns;

namespace LightTextEditorPlus.Core.Layout.WordDividers;

internal static class WordCharHelper
{
    public static int ReadWordCharCount(ReadOnlyListSpan<CharData> runList, int currentIndex)
    {
        // 读取当前的字符所在的单词的字符数量
        if (TryReadWordCharCount(CheckSpace, out var count)
            || TryReadWordCharCount(CheckEnglish, out count))
        {
            return count;
        }

        return 0;

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

    private static bool CheckEnglish(CharData charData)
    {
        string currentCharText = charData.CharObject.ToText();
        bool isLatin = RegexPatterns.LetterPattern.AreAllInRange(currentCharText);
        return isLatin;
    }

    private static bool CheckSpace(CharData charData)
    {
        string currentCharText = charData.CharObject.ToText();
        return currentCharText == RegexPatterns.BlankSpace;
    }
}