using System.Diagnostics;
using System.Globalization;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

/// <summary>
/// 默认的分词器
/// </summary>
internal class DefaultWordDivider
{
    public DefaultWordDivider(TextEditorCore textEditor, IInternalCharDataSizeMeasurer charDataSizeMeasurer)
    {
        _textEditor = textEditor;
        _charDataSizeMeasurer = charDataSizeMeasurer;
    }

    private readonly TextEditorCore _textEditor;
    private readonly IInternalCharDataSizeMeasurer _charDataSizeMeasurer;
    //private WordRange _currentWord;

    /// <summary>
    /// 根据语言文化的简单分词算法，将字符排版在一行内
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    /// <remarks>不使用 Knuth-Plass 断行算法，详细请参阅 `行为定义.md` 文档</remarks>
    public SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArgument argument)
    {
        // 判断是否在单词内
        var charData = argument.CurrentCharData;
        Debug.Assert(charData.Size!=null,"进入此方法之前，已经确保完成了字符的尺寸测量");
        TextSize textSize = GetCharSize(charData);

        // 思路：
        // 先判断字符所在的语言文化，是 Latin 的还是中文的，还是合写字的蒙文或藏文的
        // 接着读取这个单词到结束。再判断其是否能在一行放下。如果不能在一行放下，需要判断 IsTakeEmpty 属性
        // 如果是空一行，那就强行能放下多少是多少。先无视缩进导致一行都无法放下一个字符的情况
        // 再读取后面一个字符看是不是标点符号，是标点符号的话，看能不能放下。如果能放下，那就此结束。如果不能放下
        // 那么判断是否允许符号溢出边界，或者考虑将当前单词放入到下一行里面。需要考虑 IsTakeEmpty 属性

        var charCount = WordCharHelper.ReadWordCharCount(argument.RunList, argument.CurrentIndex);

        // 读取一个单词的宽度，看看是否太长了
        Debug.Assert(argument.CurrentIndex + charCount <= argument.RunList.Count, "读取的单词一定在行内");
        var totalWidth = textSize.Width;
        for (int i = argument.CurrentIndex + 1; i < argument.CurrentIndex + charCount; i++)
        {
            CharData currentCharData = argument.RunList[i];
            TextSize currentTextSize = GetCharSize(currentCharData);
            totalWidth += currentTextSize.Width;
        }

        // 如果剩余的宽度大于此当前能够获取的，那还需要判断下一个字符是否标点符号，且标点符号是不能放在下一行行首的
        bool canTakeCurrentWord;
        if (totalWidth <= argument.LineRemainingWidth)
        {
            // 单词的下一个字符的序号。序号是从 0 开始的，因此加上 charCount 即可表示下一个字符的序号
            var wordNextCharIndex = argument.CurrentIndex + charCount;
            if (wordNextCharIndex < argument.RunList.Count)
            {
                // 先判断能不能放下这个字符，能放下就无视其他规则
                CharData charDataInNextWord = argument.RunList[wordNextCharIndex];
                TextSize currentTextSize = GetCharSize(charDataInNextWord);

                // 获取测试宽度，再次判断测试行剩余宽度是否足够
                var testWidth = totalWidth + currentTextSize.Width;

                if (testWidth <= argument.LineRemainingWidth)
                {
                    // 证明能放下这个单词后的下一个字符，能放下就无视其他规则
                    canTakeCurrentWord = true;
                }
                else
                {
                    // 不能放下的话，需要额外考虑一下，判断一下这个单词后的下一个字符是否属于不能放在行首的符号
                    // punctuation
                    // 不能放在行首的符号
                    Utf32CodePoint codePoint = charDataInNextWord.CharObject.CodePoint;
                    bool allowHangingPunctuation = argument.ParagraphProperty.AllowHangingPunctuation;
                    (bool isOverflow, bool shouldTakeNextChar) = ShouldTakeNextPunctuationChar(codePoint, allowHangingPunctuation);

                    if (isOverflow)
                    {
                        canTakeCurrentWord = false;
                    }
                    else
                    {
                        canTakeCurrentWord = true;
                        if (shouldTakeNextChar)
                        {
                            // 获取下一个字符
                            charCount++;
                            totalWidth = testWidth;
                        }
                    }
                }
            }
            else
            {
                // 后续没有了，那就返回当前吧
                canTakeCurrentWord = true;
            }
        }
        else
        {
            canTakeCurrentWord = false;
        }

        if (canTakeCurrentWord)
        {
            return new SingleCharInLineLayoutResult(takeCount: charCount, new TextSize(totalWidth, textSize.Height));
        }
        else
        {
            if (argument.IsTakeEmpty)
            {
                // 空行强行换行，否则下一行说不定也不够放
                return LayoutCharWithoutCulture(argument);
            }
            else
            {
                // 这一行有内容了，那就放在下一行吧
                return new SingleCharInLineLayoutResult(takeCount: 0, default);
            }
        }

        // todo 合写字的测量
        // todo 中文考虑支持 GB/T 15834 规范
        // 额外考虑 《 和 （ 不能出现在行末
        // 5.1.4 破折号不能中间断开分为两行

        // Word 规则，不能用于行首： !%),.:;>?]}¢¨°·ˇˉ―‖’”…‰′″›℃∶、。〃〉》」』】〕〗〞︶︺︾﹀﹄﹚﹜﹞！＂％＇），．：；？］｀｜｝～￠
        // 不能用于行末：$([{£¥·‘“〈《「『【〔〖〝﹙﹛﹝＄（．［｛￡￥

        //// 单个字符直接布局，无视语言文化。快，但是诡异
        //if (argument.LineRemainingWidth > size.Width)
        //{
        //    return new SingleCharInLineLayoutResult(takeCount: 1, size);
        //}
        //else
        //{
        //    // 如果尺寸不足，也就是一个都拿不到
        //    return new SingleCharInLineLayoutResult(takeCount: 0, default);
        //}
    }

    /// <summary>
    /// 无视语言文化的获取字符
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    private SingleCharInLineLayoutResult LayoutCharWithoutCulture(in SingleCharInLineLayoutArgument argument)
    {
        var totalWidth = TextSize.Zero;
        var i = argument.CurrentIndex;
        for (; i < argument.RunList.Count; i++)
        {
            CharData charData = argument.RunList[i];
            TextSize textSize = GetCharSize(charData);
            var currentSize = totalWidth.HorizontalUnion(textSize);
            if (currentSize.Width > argument.LineRemainingWidth)
            {
                // 超过了，那就不能获取了
                break;
            }
            else
            {
                totalWidth = currentSize;
            }
        }

        // 因为数量和序号，刚好是差 1 的值。因此刚刚好减去即可
        var takeCount = i - argument.CurrentIndex;
        return new SingleCharInLineLayoutResult(takeCount, totalWidth);
    }

    /// <summary>
    /// 是否需要获取下一个字符，只有在符号不能存在行末，且允许符号溢出
    /// </summary>
    /// <param name="codePoint"></param>
    /// <param name="allowHangingPunctuation"></param>
    /// <returns></returns>
    private static (bool isOverflow, bool shouldTakeNextChar) ShouldTakeNextPunctuationChar(Utf32CodePoint codePoint, bool allowHangingPunctuation)
    {
        if (IsPunctuationNotInLineStart(codePoint))
        {
            if (allowHangingPunctuation)
            {
                // 允许溢出符号的情况下，再多取一个符号
                // todo 判断是否多符号连续
                // 例如 !!! 的情况
                return (isOverflow: false, shouldTakeNextChar: true);
            }
            else
            {
                // 不能作为行末的符号，且不允许溢出，凉凉
                return (isOverflow: true, shouldTakeNextChar: false);
            }
        }
        // 既然不是符号，那就不存在溢出
        return (isOverflow: false, shouldTakeNextChar: false);
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

    [DebuggerStepThrough] // 别跳太多层
    private TextSize GetCharSize(CharData charData) => _charDataSizeMeasurer.GetCharSize(charData);

    //readonly record struct WordRange(ReadOnlyListSpan<CharData> RunList, int StartIndex, int EndIndex);
}
