using LightTextEditorPlus.Core.Document;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils.Patterns;

namespace LightTextEditorPlus.Core.Layout;

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
    private IInternalCharDataSizeMeasurer _charDataSizeMeasurer;
    private WordRange _currentWord;

    public SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArgument argument)
    {
        // todo 考虑拆分为多个文件，每个语言文化独立文件

        // 判断是否在单词内
        var charData = argument.CurrentCharData;
        Size size = GetCharSize(charData);

        string currentCharText = charData.CharObject.ToText();
        if (currentCharText == RegexPatterns.BlankSpace)
        {
            // 如果当前是空格的话，那就判断是否可以返回啦，需要额外判断空格是否允许溢出行
            // 以下是无视此规则
            if (argument.ParagraphProperty.AllowHangingSpace)
            {
                throw new NotImplementedException("需要支持空格溢出行");
            }
            else
            {
                if (argument.LineRemainingWidth > size.Width)
                {
                    return new SingleCharInLineLayoutResult(takeCount: 1, size);
                }
                else
                {
                    // 如果尺寸不足，也就是一个都拿不到
                    return new SingleCharInLineLayoutResult(takeCount: 0, default);
                }
            }
        }

        // 思路：
        // 先判断字符所在的语言文化，是 Latin 的还是中文的，还是合写字的蒙文或藏文的
        // 接着读取这个单词到结束。再判断其是否能在一行放下。如果不能在一行放下，需要判断 IsTakeEmpty 属性
        // 如果是空一行，那就强行能放下多少是多少。先无视缩进导致一行都无法放下一个字符的情况
        // 再读取后面一个字符看是不是标点符号，是标点符号的话，看能不能放下。如果能放下，那就此结束。如果不能放下
        // 那么判断是否允许符号溢出边界，或者考虑将当前单词放入到下一行里面。需要考虑 IsTakeEmpty 属性

        var charCount = 1;

        bool isLatin = RegexPatterns.LetterPattern.AreAllInRange(currentCharText);
        if (isLatin)
        {
            // 已知当前字符的类型了，继续读取下一个字符
            int index = argument.CurrentIndex + 1;
            for (; index < argument.RunList.Count; index++)
            {
                CharData currentCharData = argument.RunList[index];
                string text = currentCharData.CharObject.ToText();
                if (!RegexPatterns.LetterPattern.AreAllInRange(text))
                {
                    // 读取到不是英文字符的
                    break;
                }
            }

            charCount = index - argument.CurrentIndex;
        }

        // 读取一个单词的宽度，看看是否太长了
        Debug.Assert(argument.CurrentIndex + charCount <= argument.RunList.Count, "读取的单词一定在行内");
        var totalWidth = size.Width;
        for (int i = argument.CurrentIndex + 1; i < argument.CurrentIndex + charCount; i++)
        {
            CharData currentCharData = argument.RunList[i];
            Size currentSize = GetCharSize(currentCharData);
            totalWidth += currentSize.Width;
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
                Size currentSize = GetCharSize(charDataInNextWord);

                // 获取测试宽度，再次判断测试行剩余宽度是否足够
                var testWidth = totalWidth + currentSize.Width;

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
                    string text = charDataInNextWord.CharObject.ToText();
                    bool allowHangingPunctuation = argument.ParagraphProperty.AllowHangingPunctuation;
                    (bool isOverflow, bool shouldTakeNextChar) = ShouldTakeNextPunctuationChar(text, allowHangingPunctuation);

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
            return new SingleCharInLineLayoutResult(takeCount: charCount, new Size(totalWidth, size.Height));
        }
        else
        {
            if (argument.IsTakeEmpty)
            {
                // todo 空行强行换行
            }
            else
            {
                // 这一行有内容了，那就放在下一行吧
                return new SingleCharInLineLayoutResult(takeCount: 0, default);
            }
        }

        // todo 数字也需要考虑小数点

        // todo 中文考虑支持 GB/T 15834 规范
        // 额外考虑 《 和 （ 不能出现在行末
        // 5.1.4 破折号不能中间断开分为两行

        // 单个字符直接布局，无视语言文化。快，但是诡异
        if (argument.LineRemainingWidth > size.Width)
        {
            return new SingleCharInLineLayoutResult(takeCount: 1, size);
        }
        else
        {
            // 如果尺寸不足，也就是一个都拿不到
            return new SingleCharInLineLayoutResult(takeCount: 0, default);
        }
    }

    /// <summary>
    /// 是否需要获取下一个字符，只有在符号不能存在行末，且允许符号溢出
    /// </summary>
    /// <param name="text"></param>
    /// <param name="allowHangingPunctuation"></param>
    /// <returns></returns>
    private static (bool isOverflow, bool shouldTakeNextChar) ShouldTakeNextPunctuationChar(string text, bool allowHangingPunctuation)
    {
        if (text.Length == 1)
        {
            if (IsPunctuationNotInLineStart(text[0]))
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
        }
        // 既然不是符号，那就不存在溢出
        return (isOverflow: false, shouldTakeNextChar: false);
    }

    static bool IsPunctuationNotInLineStart(char charInNextWord)
    {
        Span<char> punctuationNotInLineStartList = stackalloc char[]
        {
            // 英文系列
            '.',
            ',',
            '\'',
            '"',
            '!',
            '?',

            // 中文系列 GB/T 15834 规范
            '。',
            '，',
            '、',
            '；',
            '：',
            '？',
            '！',
            '”',
            '）',
            '》',
            '·', // 间隔号 5.1.7 间隔号标不能出现在一行之首
            '/', // 5.1.9 不能在行首也不能在行末

            // 其他语言的，看天
        };

        return punctuationNotInLineStartList.Contains(charInNextWord);
    }

    [DebuggerStepThrough]
    private Size GetCharSize(CharData charData) => _charDataSizeMeasurer.GetCharSize(charData);

    readonly record struct WordRange(ReadOnlyListSpan<CharData> RunList, int StartIndex, int EndIndex);
}