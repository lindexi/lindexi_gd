using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

static class GetCaretWordHelper
{
    /// <summary>
    /// 无语言文化的分词查找
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    /// 行为是找到被符号包围的单词，或者连续的符号。对于英文来说，自然每个单词之间都是被空格或标点符号包围的。对于中文来说，就没有按照空格来分词的习惯了，只好把连续的汉字作为一个单词来处理了。对于汉字的处理来说，只要没有上语言文化分词，那就没有更好的处理方法了
    public static GetCaretWordResult GetCaretCurrentWord(in GetCaretWordArgument argument)
    {
        // 无语言文化的分词查找
        // 先找段落，再从段落里面找字符
        var textEditor = argument.TextEditor;
        var caretOffset = argument.CaretOffset;

        ITextParagraph textParagraph = textEditor.DocumentManager.GetParagraph(in caretOffset);
        if (textParagraph.IsEmptyParagraph)
        {
            // 空段，啥都没有命中
            return new GetCaretWordResult()
            {
                WordSelection = caretOffset.ToSelection()
            };
        }
        TextReadOnlyListSpan<CharData> charDataList = textParagraph.GetParagraphCharDataList();

        DocumentOffset paragraphStartOffset = textParagraph.GetParagraphStartOffset();

        // 这里的是相对于段落的光标位置
        var currentCharCaret = caretOffset.Offset - paragraphStartOffset.Offset;
        // 找当前光标的单词
        Debug.Assert(currentCharCaret >= 0);

        ReadWordCountResult wordResult = ReadWordCount(currentCharCaret, charDataList, IsNotPunctuation);
        Selection wordSelection = wordResult.ToSelection(paragraphStartOffset);
        if (!wordSelection.IsEmpty)
        {
            return new GetCaretWordResult()
            {
                WordSelection = wordSelection
            };
        }

        // 没有命中到任何单词，那就选中一些符号
        // 当前是一些符号，连续的符号就一起选择，如连续的空格
        var currentCharIndex = currentCharCaret;
        if (currentCharIndex > 0)
        {
            // 光标在字符后面，应该找前一个字符
            // 如 "a|b"，光标在 a 后面，应该找 a 字符
            // 如 "|ab"，光标在 a 前面，应该找前一个字符，前面没有字符就不动
            currentCharIndex -= 1;
        }
        var currentCharData = charDataList[currentCharIndex];
        Utf32CodePoint currentCharDataCodePoint = currentCharData.CharObject.CodePoint;
        ReadWordCountResult punctuationResult = ReadWordCount(currentCharCaret, charDataList,
            charData => charData.CharObject.CodePoint == currentCharDataCodePoint);
        return new GetCaretWordResult()
        {
            WordSelection = punctuationResult.ToSelection(paragraphStartOffset),
            HitPunctuationOrSpace = true
        };

        static bool IsNotPunctuation(CharData charData)
        {
            return !IsPunctuation(charData);
        }
    }

    /// <summary>
    /// 读取单词的字符数量
    /// </summary>
    /// <param name="currentCharCaret">当前的字符光标，相对于 <paramref name="charDataList"/> 的坐标</param>
    /// <param name="charDataList">字符数据列表</param>
    /// <param name="predicate">判断字符是否符合条件的谓词</param>
    /// <returns>读取单词的字符数量结果</returns>
    private static ReadWordCountResult ReadWordCount
        (int currentCharCaret, TextReadOnlyListSpan<CharData> charDataList, Predicate<CharData> predicate)
    {
        int rightWordCharCount = 0;
        for (int i = currentCharCaret; i < charDataList.Count; i++)
        {
            if (predicate(charDataList[i]))
            {
                rightWordCharCount++;
            }
            else
            {
                break;
            }
        }

        int leftWordCharCount = 0;
        for (int i = currentCharCaret - 1; i >= 0; i--)
        {
            if (predicate(charDataList[i]))
            {
                leftWordCharCount++;
            }
            else
            {
                break;
            }
        }

        return new ReadWordCountResult(leftWordCharCount, rightWordCharCount, currentCharCaret);
    }

    /// <summary>
    /// 是否符号。这里的符号包括空格
    /// </summary>
    /// <param name="charData"></param>
    /// <returns></returns>
    public static bool IsPunctuation(CharData charData)
    {
        Utf32CodePoint codePoint = charData.CharObject.CodePoint;
        if (codePoint.Value == ' ')
        {
            return true;
        }

        Span<char> buffer = stackalloc char[2];
        int length = codePoint.Rune.EncodeToUtf16(buffer);
        if (length == 1)
        {
            // 如果是单字符的，直接用 char 的方法判断
            return char.IsPunctuation(buffer[0]);
        }
        else
        {
            // 多字符的，就用 UnicodeCategory 判断
            UnicodeCategory unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(codePoint.Value);
            return ((int) unicodeCategory & 0b0001_0000) > 0;
        }
    }

    /// <summary>
    /// 读取单词的字符数量结果
    /// </summary>
    /// <param name="LeftCount">光标的左边有多少个字符</param>
    /// <param name="RightCount">光标的右边有多少个字符</param>
    /// <param name="CurrentCharIndex">光标的当前索引位置</param>
    readonly record struct ReadWordCountResult(int LeftCount, int RightCount, int CurrentCharIndex)
    {
        /// <summary>
        /// 总字符数量
        /// </summary>
        public int Length => LeftCount + RightCount;

        /// <summary>
        /// 转换为选择范围
        /// </summary>
        /// <param name="paragraphStartOffset"></param>
        /// <returns></returns>
        public Selection ToSelection(DocumentOffset paragraphStartOffset)
        {
            int startIndex = CurrentCharIndex - LeftCount;
            return new Selection(new CaretOffset(paragraphStartOffset.Offset + startIndex), Length);
        }
    }
}

