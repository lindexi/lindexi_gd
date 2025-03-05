using System.Globalization;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

internal class WordDivider : IWordDivider
{
    public DivideWordResult DivideWord(in DivideWordArgument argument)
    {
        int currentIndex = 0;

        var currentCharData = argument.CurrentRunList[currentIndex];

        if (IsPunctuationNotInLineEnd(currentCharData.CharObject.CodePoint))
        {
            currentIndex++;
        }

        var totalCount = currentIndex;
        var charCount = WordCharHelper.ReadWordCharCount(argument.CurrentRunList, currentIndex);
        totalCount += charCount;

        // 不能放在行首的符号
        var wordNextCharIndex = currentIndex + charCount;
        if (wordNextCharIndex < argument.CurrentRunList.Count)
        {
            CharData charDataInNextWord = argument.CurrentRunList[wordNextCharIndex];
            Utf32CodePoint codePoint = charDataInNextWord.CharObject.CodePoint;

            if (IsPunctuationNotInLineStart(codePoint))
            {
                // 如果下一个字符是标点符号，且不能放在行首，那就不要放在这一行里面
                totalCount += 1;
            }
        }

        return new DivideWordResult(totalCount);
    }

    private static bool IsPunctuationNotInLineEnd(Utf32CodePoint codePoint)
    {
        UnicodeCategory unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(codePoint.Value);

        return unicodeCategory is UnicodeCategory.OpenPunctuation
            or UnicodeCategory.InitialQuotePunctuation;
    }

    /// <summary>
    /// 通过语言文化判断当前传入的标点符号是否不能放在行首。语言文化里面只能用来判断符号，是否能放在行首是文本库的判断
    /// </summary>
    /// <param name="codePoint">传入参数之前，确保只有一个字符</param>
    /// <returns></returns>
    private static bool IsPunctuationNotInLineStart(Utf32CodePoint codePoint)
    {
        // 只是判断标点符号而已
        // 反向判断，通过正则辅助判断。只要是标点符号，且不是可以在行首的，那就返回 true 值
        UnicodeCategory unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(codePoint.Value);
        return unicodeCategory is UnicodeCategory.OtherPunctuation
            //or UnicodeCategory.OpenPunctuation 如 （）
            or UnicodeCategory.ClosePunctuation
            or UnicodeCategory.ConnectorPunctuation
            or UnicodeCategory.DashPunctuation
            //or UnicodeCategory.InitialQuotePunctuation 如 “
            or UnicodeCategory.FinalQuotePunctuation;

        //if (RegexPatterns.LeftSurroundInterpunction.Contains(charInNextWord))
        //{
        //    // 先判断是否在行首，这个判断数量比较小，速度快
        //    return false;
        //}

        //return Regex.IsMatch(text, RegexPatterns.Interpunction);

        //Span<char> punctuationNotInLineStartList = stackalloc char[]
        //{
        //    // 英文系列
        //    '.',
        //    ',',
        //    ':',
        //    ';',
        //    '?',
        //    '!',
        //    '\'',
        //    '"',
        //    ')',

        //    // 中文系列 GB/T 15834 规范
        //    '。',
        //    '，',
        //    '、',
        //    '；',
        //    '：',
        //    '？',
        //    '！',
        //    '”',
        //    '）',
        //    '》',
        //    '·', // 间隔号 5.1.7 间隔号标不能出现在一行之首
        //    '/', // 5.1.9 不能在行首也不能在行末

        //    // 其他语言的，看天
        //};

        //return punctuationNotInLineStartList.Contains(charInNextWord);
    }
}