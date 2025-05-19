using System;
using System.Globalization;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 编号项目符号，又称有序项目符号
/// </summary>
public class NumberMarker : TextMarker
{
    /// <summary>
    /// 表示当前缩进级别的数字项目符号起始编号
    /// </summary>
    public int StartAt { get; init; } = 1;

    /// <summary>
    /// 编号项目符号类型
    /// </summary>
    public AutoNumberType AutoNumberType { get; set; } = AutoNumberType.ArabicPeriod;

    /// <summary>
    /// 获取当前级别的编号文本
    /// </summary>
    /// <param name="levelIndex"></param>
    /// <returns></returns>
    public virtual string GetMarkerText(uint levelIndex)
    {
        switch (AutoNumberType)
        {
            case AutoNumberType.AlphaLowerCharacterParenBoth:
                // 带双括号的小写字母，如 (a)、 (b)、 (c)
                return $"({GetLowerLatinMarkerText(levelIndex)})";
            case AutoNumberType.AlphaUpperCharacterParenBoth:
                // 带双括号的大写字母，如(A)、 (B)、 (C)
                return $"({GetUpperLatinMarkerText(levelIndex)})";
            case AutoNumberType.AlphaLowerCharacterParenR:
                // 带右括号的小写字母，如 a)、 b)、 c)
                return $"{GetLowerLatinMarkerText(levelIndex)})";
            case AutoNumberType.AlphaUpperCharacterParenR:
                // 带右括号的大写字母，如 A)、 B)、 C)
                return $"{GetUpperLatinMarkerText(levelIndex)})";
            case AutoNumberType.AlphaLowerCharacterPeriod:
                // 带点号的小写字母，如 a.、 b.、 c.
                return $"{GetLowerLatinMarkerText(levelIndex)}.";
            case AutoNumberType.AlphaUpperCharacterPeriod:
                // 带点号的大写字母，如 A.、 B.、 C.
                return $"{GetUpperLatinMarkerText(levelIndex)}.";
            case AutoNumberType.ArabicParenBoth:
                // 带双括号的阿拉伯数字，如 (1)、 (2)、 (3)
                return $"({GetArabicNumberMarkerText(levelIndex)})";
            case AutoNumberType.ArabicParenR:
                // 带右括号的阿拉伯数字，如 1)、 2)、 3)
                return $"{GetArabicNumberMarkerText(levelIndex)})";
            case AutoNumberType.ArabicPeriod:
                // 带点号的阿拉伯数字，如 1.、 2.、 3.
                return $"{GetArabicNumberMarkerText(levelIndex)}.";
            case AutoNumberType.ArabicPlain:
                // 不带任何符号的阿拉伯数字，如 1、 2、 3
                return GetArabicNumberMarkerText(levelIndex);
                break;
            //case AutoNumberType.RomanLowerCharacterParenBoth:
            //    break;
            //case AutoNumberType.RomanUpperCharacterParenBoth:
            //    break;
            //case AutoNumberType.RomanLowerCharacterParenR:
            //    break;
            //case AutoNumberType.RomanUpperCharacterParenR:
            //    break;
            //case AutoNumberType.RomanLowerCharacterPeriod:
            //    break;
            //case AutoNumberType.RomanUpperCharacterPeriod:
            //    break;
            case AutoNumberType.CircleNumberDoubleBytePlain:
                // 带圈的双字节阿拉伯数字，如 ①、 ②、 ③
                return GetCircleNumberDoubleBytePlainMarkerText(levelIndex);
            //case AutoNumberType.CircleNumberWingdingsBlackPlain:
            //    break;
            //case AutoNumberType.CircleNumberWingdingsWhitePlain:
            //    break;
            //case AutoNumberType.ArabicDoubleBytePeriod:
            //    break;
            //case AutoNumberType.ArabicDoubleBytePlain:
            //    break;
            //case AutoNumberType.EastAsianSimplifiedChinesePeriod:
            //    break;
            //case AutoNumberType.EastAsianSimplifiedChinesePlain:
            //    break;
            //case AutoNumberType.EastAsianTraditionalChinesePeriod:
            //    break;
            //case AutoNumberType.EastAsianTraditionalChinesePlain:
            //    break;
            //case AutoNumberType.EastAsianJapaneseDoubleBytePeriod:
            //    break;
            //case AutoNumberType.EastAsianJapaneseKoreanPlain:
            //    break;
            //case AutoNumberType.EastAsianJapaneseKoreanPeriod:
            //    break;
            //case AutoNumberType.Arabic1Minus:
            //    break;
            //case AutoNumberType.Arabic2Minus:
            //    break;
            //case AutoNumberType.Hebrew2Minus:
            //    break;
            //case AutoNumberType.ThaiAlphaPeriod:
            //    break;
            //case AutoNumberType.ThaiAlphaParenthesisRight:
            //    break;
            //case AutoNumberType.ThaiAlphaParenthesisBoth:
            //    break;
            //case AutoNumberType.ThaiNumberPeriod:
            //    break;
            //case AutoNumberType.ThaiNumberParenthesisRight:
            //    break;
            //case AutoNumberType.ThaiNumberParenthesisBoth:
            //    break;
            //case AutoNumberType.HindiAlphaPeriod:
            //    break;
            //case AutoNumberType.HindiNumPeriod:
            //    break;
            //case AutoNumberType.HindiNumberParenthesisRight:
            //    break;
            //case AutoNumberType.HindiAlpha1Period:
            //    break;
            case AutoNumberType.Custom:
                throw new InvalidOperationException($"当 AutoNumberType 为 Custom 时，要求重写 GetMarkerText 方法");
                break;
            default:
                throw new NotSupportedException();
        }
    }

    internal override void DisableInherit()
    {
    }

    /// <summary>
    /// 根据当前级别获取小写英文字符串
    /// </summary>
    /// <param name="levelIndex"></param>
    /// <returns></returns>
    protected static string GetLowerLatinMarkerText(uint levelIndex)
    {
/*
   其对应关系如下：
   0 a.
   1 b.
   2 c.
   3 d.
   4 e.
   ...
   23 x.
   24 y.
   25 z.
   26 aa.
   27 bb.
   28 cc.
   29 dd.
   30 ee.
 */
        const int startAsciiNum = 'a'; //97
        const int aToZCount = 'z' - 'a' + 1;
        int count = (int) levelIndex / aToZCount;
        int index = (int) levelIndex % aToZCount;

        var word = (char) (startAsciiNum + index);
        return string.Concat(new string(word, count + 1));
    }

    /// <summary>
    /// 根据当前级别获取大写英文字符串
    /// </summary>
    /// <param name="levelIndex"></param>
    /// <returns></returns>
    protected static string GetUpperLatinMarkerText(uint levelIndex)
    {
        const int startAsciiNum = 'A'; //65
        const int aToZCount = 'Z' - 'A' + 1;
        int count = (int) levelIndex / aToZCount;
        int index = (int) levelIndex % aToZCount;

        var word = (char) (startAsciiNum + index);
        return string.Concat(new string(word, count + 1));
    }

    /// <summary>
    /// 根据当前级别获取阿拉伯数字字符串
    /// </summary>
    /// <param name="levelIndex"></param>
    /// <returns></returns>
    protected static string GetArabicNumberMarkerText(uint levelIndex)
    {
        // 从 1 开始的
        return (levelIndex + 1).ToString(CultureInfo.InvariantCulture);
    }

    private string GetCircleNumberDoubleBytePlainMarkerText(uint levelIndex)
    {
        if (levelIndex <= 20)
        {
            // 在 20 以内，采用圆圈的写法，大于等于 21 就用数字
            return ((char) ('①' + levelIndex)).ToString();
        }

        return GetArabicNumberMarkerText(levelIndex);
    }
}