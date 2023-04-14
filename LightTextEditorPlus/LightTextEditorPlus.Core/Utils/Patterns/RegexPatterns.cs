using System;

namespace LightTextEditorPlus.Core.Utils.Patterns;

/// <summary>
/// 正则表达式静态管理类
/// </summary>
/// todo 改名为 TextPatterns 类
public static class RegexPatterns
{
    /// <summary>
    /// 藏文
    /// </summary>
    public const string Tibetan = @"[\u0f00-\u0ff0]";

    private const char TibetanMinChar = '\u0f00';
    private const char TibetanMaxChar = '\u0ff0';

    /// <summary>
    /// 藏文匹配
    /// </summary>
    public static readonly TextRangePattern TibetanPattern = new TextRangePattern(TibetanMinChar, TibetanMaxChar);

    /// <summary>
    /// 蒙古文字符
    /// </summary>
    public const string Mongolian = @"[\u1800-\u18AF]";

    /// <summary>
    /// 蒙古文匹配
    /// </summary>
    public static readonly TextRangePattern MongolianPattern = new TextRangePattern('\u1800', '\u18AF');

    /// <summary>
    /// 阿拉伯数字
    /// </summary>
    public const string Number = "[0-9]";

    /// <summary>
    /// 阿拉伯数字匹配
    /// </summary>
    public static readonly TextRangePattern NumberPattern = new TextRangePattern('0', '9');

    /// <summary>
    /// 中文标点符号
    /// </summary>
    public const string ChineseSymbol = @"[\u2014-\u2026]|[\u3000-\u303F]|[\uff01-\uff0f]|[\uff1a-\uff20]|[\uff3b-\uff40]|[\uff5b-\uff5e]";

    /// <summary>
    /// 除了汉字、空格、扩展字符外的连续词组
    /// </summary>
    public static readonly string ContinuousPhrase = $"[^\u4e00-\u9fbb\\s{TextContext.UnknownChar}{TextContext.NotChar}]+";

    /// <summary>
    /// 汉字、空格、扩展字符组成的独立布局的连续词组
    /// </summary>
    public static readonly string IndependentPhrase = $"[\u4e00-\u9fbb\\s{TextContext.UnknownChar}]+";

    /// <summary>
    /// 左包围的标点符号（可以出现在行首的标点符号）
    /// </summary>
    public static readonly char[] LeftSurroundInterpunction = { '"', '\'', '“', '‘', '《', '[', '【' };

    /// <summary>
    /// 匹配标点符号
    /// </summary>
    public const string Interpunction = "[\\p{P}]+";

    /// <summary>
    /// 完整匹配一个Emoji字符
    /// </summary>
    public const string Utf16Char = @"([\ud800-\udbff][\udc00-\udfff])";

    /// <summary>
    /// 由两个Unicode码组成的Emoji字符的Unicode码
    /// </summary>
    public const string Utf16Surrogates = @"[\ud800-\udfff]";

    /// <summary>
    /// 由两个Unicode码组成的Emoji字符的Unicode码
    /// 这是 UTF-16 的高半区 对应 Unicode 字符平面映射的 High-half zone of UTF-16 字符
    /// 而 UTF-16的低半区 范围是 DC00-DFFF 的字符
    /// </summary>
    public static readonly TextRangePattern Utf16SurrogatesPattern = new TextRangePattern('\ud800', '\udfff');

    /// <summary>
    ///  由一个Unicode码组成的Emoji字符的Unicode码
    ///  对应 Unicode 字符平面映射的 Miscellaneous Symbols 杂项符号
    /// </summary>
    public const string OneCodeEmojiParts = @"[\u2600-\u27ff]";

    /// <summary>
    /// 中文标点符号中需要旋转的符号（比如：【】（）《》等）
    /// <para>使用这种正则匹配存在一些问题，不同字体下可能公用同一个Unicode码，</para>
    /// <para>不同字体下有不同的字符需要做竖排的旋转处理，需要针对每种字体（或语言）处理</para>
    /// <para>目前暂时只处理汉字</para>
    /// </summary>
    public const string ChineseSymbolNeedRotation = @"[\u0028-\u0029]|[\u005b]|[\u2026]|[\u005d]|[\u007b]|[\u2014]|[\u007d]|[\u0025-\u0026]|[\uff08-\uff09]|[\u3008-\u3011]|[\u3014-\u301b]|[\ufe59-\ufe5e]";

    /// <summary>
    /// 需要旋转的项目符号字符
    /// </summary>
    public const string MarkerStringNeedRotation = @"[▶]|[\u221a]|[\u0030-\u0039]|[\u0041-\u005A]|[\u0061-\u007A]|[\u2460-\u249b]|[\u3251-\u3289]|[\u32b1-\u32cb]|[\u2160-\u217b]";

    /// <summary>
    /// 英文标点符号
    /// </summary>
    public const string EnglishSymbol = "[-,.?:;'\"!`]|(-{2})|(/.{3})|(/(/))|(/[/])|({})";

    /// <summary>
    /// 汉字
    /// 对应 Unicode 字符平面映射的 CJK Unified Ideographs 中日韩字符
    /// </summary>
    public const string Hanzi = @"[\u4e00-\u9fbb]";

    /// <summary>
    /// 汉字匹配
    /// </summary>
    public static readonly TextRangePattern HanziPattern = new TextRangePattern('\u4e00', '\u9fbb');

    /// <summary>
    /// 阿拉伯语匹配
    /// https://zh.wikipedia.org/wiki/Unicode%E5%AD%97%E7%AC%A6%E5%B9%B3%E9%9D%A2%E6%98%A0%E5%B0%84
    /// </summary>
    public static readonly TextRangePattern ArabicPattern = new TextRangePattern('\u0600', '\u06ff');

    /// <summary>
    /// 阿拉伯语补充匹配
    /// https://zh.wikipedia.org/wiki/Unicode%E5%AD%97%E7%AC%A6%E5%B9%B3%E9%9D%A2%E6%98%A0%E5%B0%84
    /// </summary>
    public static readonly TextRangePattern ArabicSupplementPattern = new TextRangePattern('\u0750', '\u077f');

    /// <summary>
    /// 阿拉伯文扩展-A匹配
    /// https://zh.wikipedia.org/wiki/Unicode%E5%AD%97%E7%AC%A6%E5%B9%B3%E9%9D%A2%E6%98%A0%E5%B0%84
    /// </summary>
    public static readonly TextRangePattern ArabicExtendedAPattern = new TextRangePattern('\u08a0', '\u08ff');

    /// <summary>
    /// 希伯来语匹配
    /// https://zh.wikipedia.org/wiki/Unicode%E5%AD%97%E7%AC%A6%E5%B9%B3%E9%9D%A2%E6%98%A0%E5%B0%84
    /// </summary>
    public static readonly TextRangePattern HebrewPattern = new TextRangePattern('\u0590', '\u05ff');

    /// <summary>
    /// ASCII 符号
    /// https://zh.wikipedia.org/wiki/ASCII
    /// </summary>
    public const string AsciiPunctuation = "[\x21-\x2f\x3a-\x40\x5b-\x60\x7b-\x7e]";

    ///// <summary>
    ///// ASCII 符号
    ///// https://zh.wikipedia.org/wiki/ASCII
    ///// </summary>
    ///// !-/ :-@ [-` {-~
    //internal static readonly MultiTextRangePattern AsciiPunctuationPattern =
    //    new MultiTextRangePattern
    //    (
    //        /* !-/ */ (char) 0x21, (char) 0x2f,
    //        /* :-@ */ (char) 0x3a, (char) 0x40,
    //        /* [-` */ (char) 0x5b, (char) 0x60,
    //        /* {-~ */ (char) 0x7b, (char) 0x7e
    //    );

    /// <summary>
    /// 所有ASCII字符
    /// </summary>
    public const string Ascii = "[\x00-\xff]";

    internal static readonly AsciiPattern AsciiPattern = new AsciiPattern();

    /// <summary>
    /// 英文字符
    /// </summary>
    public const string Letters = "[a-zA-Z]";

    /// <summary>
    /// 匹配 Letter 字符，包含中文字符哦
    /// </summary>
    public static readonly IPattern LetterPattern = new LetterPattern();

    /// <summary>
    /// 匹配英文字符
    /// </summary>
    public static readonly IPattern EnglishLetterPattern = new EnglishLetterPattern();

    /// <summary>
    /// 小写字符
    /// </summary>
    public const string LowerLetters = "[a-z]";

    /// <summary>
    /// 小写字符匹配
    /// </summary>
    public static readonly TextRangePattern LowerLettersPattern = new TextRangePattern('a', 'z');

    /// <summary>
    /// 大写字符
    /// </summary>
    public const string UpperLetters = "[A-Z]";

    /// <summary>
    /// 大写字符匹配
    /// </summary>
    public static readonly TextRangePattern UpperLettersPattern = new TextRangePattern('A', 'Z');

    /// <summary>
    /// 换行符
    /// </summary>
    public const string NewLine = TextContext.NewLine;

    /// <summary>
    /// 空白字符 \s
    /// </summary>
    public const string Blank = @"\s";

    /// <summary>
    /// 空格 @" "
    /// </summary>
    public const string BlankSpace = @" ";

    /// <summary>
    /// 空格字符 ' '
    /// </summary>
    public const char BlankSpaceChar = ' ';
}