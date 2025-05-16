namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 编号项目符号类型
/// </summary>
/// 类型定义完全参考 OpenXML 的定义，具体参考 《编号项目符号整理大全.pptx》 文档
public enum AutoNumberType
{
    /// <summary>
    /// 带双括号的小写字母，如 (a)、 (b)、 (c)
    /// </summary>
    AlphaLowerCharacterParenBoth,

    /// <summary>
    /// 带双括号的大写字母，如 (A)、 (B)、 (C)
    /// </summary>
    AlphaUpperCharacterParenBoth,

    /// <summary>
    /// 带右括号的小写字母，如 a)、 b)、 c)
    /// </summary>
    AlphaLowerCharacterParenR,

    /// <summary>
    /// 带右括号的大写字母，如 A)、 B)、 C)
    /// </summary>
    AlphaUpperCharacterParenR,

    /// <summary>
    /// 带点号的小写字母，如 a.、 b.、 c.
    /// </summary>
    AlphaLowerCharacterPeriod,

    /// <summary>
    /// 带点号的大写字母，如 A.、 B.、 C.
    /// </summary>
    AlphaUpperCharacterPeriod,

    /// <summary>
    /// 带双括号的阿拉伯数字，如 (1)、 (2)、 (3)
    /// </summary>
    ArabicParenBoth,

    /// <summary>
    /// 带右括号的阿拉伯数字，如 1)、 2)、 3)
    /// </summary>
    ArabicParenR,

    /// <summary>
    /// 带点号的阿拉伯数字，如 1.、 2.、 3.
    /// </summary>
    ArabicPeriod,

    /// <summary>
    /// 不带任何符号的阿拉伯数字，如 1、 2、 3
    /// </summary>
    ArabicPlain,

    /// <summary>
    /// 带双括号的小写罗马数字，如 (i)、 (ii)、 (iii)
    /// </summary>
    RomanLowerCharacterParenBoth,

    /// <summary>
    /// 带双括号的大写罗马数字，如 (I)、 (II)、 (III)
    /// </summary>
    RomanUpperCharacterParenBoth,

    /// <summary>
    /// 带右括号的小写罗马数字，如 i)、 ii)、 iii)
    /// </summary>
    RomanLowerCharacterParenR,

    /// <summary>
    /// 带右括号的大写罗马数字，如 I)、 II)、 III)
    /// </summary>
    RomanUpperCharacterParenR,

    /// <summary>
    /// 带点号的小写罗马数字，如 i.、 ii.、 iii.
    /// </summary>
    RomanLowerCharacterPeriod,

    /// <summary>
    /// 带点号的大写罗马数字，如 I.、 II.、 III.
    /// </summary>
    RomanUpperCharacterPeriod,

    /// <summary>
    /// 带圈的双字节阿拉伯数字，如 ①、 ②、 ③
    /// </summary>
    CircleNumberDoubleBytePlain,

    /// <summary>
    /// 黑底的圈的双字节阿拉伯数字，如 ①、 ②、 ③
    /// </summary>
    CircleNumberWingdingsBlackPlain,

    /// <summary>
    /// 白底的圈的双字节阿拉伯数字，如 ①、 ②、 ③
    /// </summary>
    CircleNumberWingdingsWhitePlain,

    /// <summary>
    /// 带点号的双字节阿拉伯数字，如 1.、 2.、 3.
    /// </summary>
    ArabicDoubleBytePeriod,

    /// <summary>
    /// 不带任何符号的双字节阿拉伯数字，如 1、 2、 3
    /// </summary>
    ArabicDoubleBytePlain,

    /// <summary>
    /// 带点的简体中文数字，如 一.、 二.、 三.
    /// </summary>
    EastAsianSimplifiedChinesePeriod,

    /// <summary>
    /// 不带任何符号的简体中文数字，如 一、 二、 三
    /// </summary>
    EastAsianSimplifiedChinesePlain,

    /// <summary>
    /// 带点的繁体中文数字，如 一.、 二.、 三.
    /// </summary>
    EastAsianTraditionalChinesePeriod,

    /// <summary>
    /// 不带任何符号的繁体中文数字，如 一、 二、 三
    /// </summary>
    EastAsianTraditionalChinesePlain,

    /// <summary>
    /// 带点的日文双字节数字，如 一.、 二.、 三.
    /// </summary>
    EastAsianJapaneseDoubleBytePeriod,

    /// <summary>
    /// 不带任何符号的日韩数字，如 一、 二、 三
    /// </summary>
    EastAsianJapaneseKoreanPlain,

    /// <summary>
    /// 带点的日韩数字，如 一.、 二.、 三.
    /// </summary>
    EastAsianJapaneseKoreanPeriod,

    /// <summary>
    /// 阿拉伯语1
    /// </summary>
    Arabic1Minus,

    /// <summary>
    /// 阿拉伯语2
    /// </summary>
    Arabic2Minus,

    /// <summary>
    /// 希伯来语2
    /// </summary>
    Hebrew2Minus,

    /// <summary>
    /// 带点的泰语
    /// </summary>
    ThaiAlphaPeriod,

    /// <summary>
    /// 带右括号的泰语
    /// </summary>
    ThaiAlphaParenthesisRight,

    /// <summary>
    /// 带双括号的泰语
    /// </summary>
    ThaiAlphaParenthesisBoth,

    /// <summary>
    /// 带点的泰语数字
    /// </summary>
    ThaiNumberPeriod,

    /// <summary>
    /// 带右括号的泰语数字
    /// </summary>
    ThaiNumberParenthesisRight,

    /// <summary>
    /// 带双括号的泰语数字
    /// </summary>
    ThaiNumberParenthesisBoth,

    /// <summary>
    /// 带点的印地语字母
    /// </summary>
    HindiAlphaPeriod,

    /// <summary>
    /// 带点的印地语数字
    /// </summary>
    HindiNumPeriod,

    /// <summary>
    /// 带右括号的印地语数字
    /// </summary>
    HindiNumberParenthesisRight,

    /// <summary>
    /// 带点的印地语
    /// </summary>
    HindiAlpha1Period,
}