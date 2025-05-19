using System;
using System.ComponentModel;

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
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    RomanLowerCharacterParenBoth,

    /// <summary>
    /// 带双括号的大写罗马数字，如 (I)、 (II)、 (III)
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    RomanUpperCharacterParenBoth,

    /// <summary>
    /// 带右括号的小写罗马数字，如 i)、 ii)、 iii)
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    RomanLowerCharacterParenR,

    /// <summary>
    /// 带右括号的大写罗马数字，如 I)、 II)、 III)
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    RomanUpperCharacterParenR,

    /// <summary>
    /// 带点号的小写罗马数字，如 i.、 ii.、 iii.
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    RomanLowerCharacterPeriod,

    /// <summary>
    /// 带点号的大写罗马数字，如 I.、 II.、 III.
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    RomanUpperCharacterPeriod,

    /// <summary>
    /// 带圈的双字节阿拉伯数字，如 ①、 ②、 ③
    /// </summary>
    CircleNumberDoubleBytePlain,

    /// <summary>
    /// 黑底的圈的双字节阿拉伯数字，如 ①、 ②、 ③
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    CircleNumberWingdingsBlackPlain,

    /// <summary>
    /// 白底的圈的双字节阿拉伯数字，如 ①、 ②、 ③
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    CircleNumberWingdingsWhitePlain,

    /// <summary>
    /// 带点号的双字节阿拉伯数字，如 1.、 2.、 3.
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    ArabicDoubleBytePeriod,

    /// <summary>
    /// 不带任何符号的双字节阿拉伯数字，如 1、 2、 3
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    ArabicDoubleBytePlain,

    /// <summary>
    /// 带点的简体中文数字，如 一.、 二.、 三.
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    EastAsianSimplifiedChinesePeriod,

    /// <summary>
    /// 不带任何符号的简体中文数字，如 一、 二、 三
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    EastAsianSimplifiedChinesePlain,

    /// <summary>
    /// 带点的繁体中文数字，如 一.、 二.、 三.
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    EastAsianTraditionalChinesePeriod,

    /// <summary>
    /// 不带任何符号的繁体中文数字，如 一、 二、 三
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    EastAsianTraditionalChinesePlain,

    /// <summary>
    /// 带点的日文双字节数字，如 一.、 二.、 三.
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    EastAsianJapaneseDoubleBytePeriod,

    /// <summary>
    /// 不带任何符号的日韩数字，如 一、 二、 三
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    EastAsianJapaneseKoreanPlain,

    /// <summary>
    /// 带点的日韩数字，如 一.、 二.、 三.
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    EastAsianJapaneseKoreanPeriod,

    /// <summary>
    /// 阿拉伯语1
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    Arabic1Minus,

    /// <summary>
    /// 阿拉伯语2
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    Arabic2Minus,

    /// <summary>
    /// 希伯来语2
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    Hebrew2Minus,

    /// <summary>
    /// 带点的泰语
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    ThaiAlphaPeriod,

    /// <summary>
    /// 带右括号的泰语
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    ThaiAlphaParenthesisRight,

    /// <summary>
    /// 带双括号的泰语
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    ThaiAlphaParenthesisBoth,

    /// <summary>
    /// 带点的泰语数字
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    ThaiNumberPeriod,

    /// <summary>
    /// 带右括号的泰语数字
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    ThaiNumberParenthesisRight,

    /// <summary>
    /// 带双括号的泰语数字
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    ThaiNumberParenthesisBoth,

    /// <summary>
    /// 带点的印地语字母
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    HindiAlphaPeriod,

    /// <summary>
    /// 带点的印地语数字
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    HindiNumPeriod,

    /// <summary>
    /// 带右括号的印地语数字
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    HindiNumberParenthesisRight,

    /// <summary>
    /// 带点的印地语
    /// </summary>
    [Obsolete("NotImplemented", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    HindiAlpha1Period,

    /// <summary>
    /// 自定义的
    /// </summary>
    Custom,
}