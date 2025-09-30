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
    public static GetCaretWordResult GetCaretWord(in GetCaretWordArgument argument)
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

        var currentCharIndex = caretOffset.Offset - paragraphStartOffset.Offset;
        // 找当前光标的单词
        Debug.Assert(currentCharIndex >= 0);

        ReadWordCountResult wordResult = ReadWordCount(currentCharIndex, charDataList, IsNotPunctuation);
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
        var currentCharData = charDataList[currentCharIndex];
        Utf32CodePoint currentCharDataCodePoint = currentCharData.CharObject.CodePoint;
        ReadWordCountResult punctuationResult = ReadWordCount(currentCharIndex, charDataList,
            charData => charData.CharObject.CodePoint == currentCharDataCodePoint);
        return new GetCaretWordResult()
        {
            WordSelection = punctuationResult.ToSelection(paragraphStartOffset),
            HitPunctuation = true
        };

        static bool IsNotPunctuation(CharData charData)
        {
            return !IsPunctuation(charData);
        }

        static bool IsPunctuation(CharData charData)
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
    }

    private static ReadWordCountResult ReadWordCount
        (int currentCharIndex, TextReadOnlyListSpan<CharData> charDataList, Predicate<CharData> predicate)
    {
        int rightWordCharCount = 0;
        for (int i = currentCharIndex; i < charDataList.Count; i++)
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
        for (int i = currentCharIndex - 1; i >= 0; i--)
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

        return new ReadWordCountResult(leftWordCharCount, rightWordCharCount, currentCharIndex);
    }

    readonly record struct ReadWordCountResult(int LeftCount, int RightCount, int CurrentCharIndex)
    {
        public int Length => LeftCount + RightCount;

        public Selection ToSelection(DocumentOffset paragraphStartOffset)
        {
            int startIndex = CurrentCharIndex - LeftCount;
            return new Selection(new CaretOffset(paragraphStartOffset.Offset + startIndex), Length);
        }
    }
}

