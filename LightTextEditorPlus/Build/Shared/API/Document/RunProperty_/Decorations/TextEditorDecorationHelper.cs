#if DirectTextEditorDefinition

using System;
using System.Collections.Generic;
using System.Diagnostics;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;

#if USE_SKIA
using RunProperty = LightTextEditorPlus.Document.SkiaTextRunProperty;
#endif

namespace LightTextEditorPlus.Document.Decorations;

/// <summary>
/// 文本装饰的分割结果
/// </summary>
/// <param name="Decoration"></param>
/// <param name="RunProperty"></param>
/// <param name="CharList"></param>
readonly record struct DecorationSplitResult(TextEditorDecoration Decoration, RunProperty RunProperty, TextReadOnlyListSpan<CharData> CharList);

static class TextEditorDecorationHelper
{
    /// <summary>
    /// 按照文本装饰进行分割字符列表
    /// </summary>
    /// <param name="charList"></param>
    /// <returns></returns>
    public static IEnumerable<DecorationSplitResult> SplitContinuousTextDecorationCharData(
        TextReadOnlyListSpan<CharData> charList)
    {
        var offset = SkipEmptyTextEditorDecoration(in charList, 0);
        if (offset == charList.Count)
        {
            // 短路分支，如果没有装饰则快速返回
            yield break;
        }

        // 这个字典用于处理快慢装饰层的问题
        // 字符：1 2 3 4 5 6
        // 装饰：-----   ---
        // 装饰：---   ---
        // 如上所示，有两个装饰，两个装饰覆盖的范围不相同。通过此字典记录进行处理
        Dictionary<TextEditorDecoration, int /*Offset*/> dictionary = [];

        for (; offset < charList.Count;)
        {
            offset = SkipEmptyTextEditorDecoration(in charList, offset);
            if (offset == charList.Count)
            {
                yield break;
            }
            Debug.Assert(offset < charList.Count);

            // 获取到装饰
            CharData firstCharData = charList[offset];
            RunProperty runProperty = firstCharData.RunProperty.AsRunProperty();
            var minCount = 1;

            for (var decorationIndex = 0; decorationIndex < runProperty.DecorationCollection.Count; decorationIndex++)
            {
                TextEditorDecoration textEditorDecoration = runProperty.DecorationCollection[decorationIndex];
                int textEditorDecorationOffset = dictionary.GetValueOrDefault(textEditorDecoration, -1);
                if (textEditorDecorationOffset > offset)
                {
                    continue;
                }

                bool Predicate(CharData a, CharData b)
                {
                    RunProperty aRunProperty = a.RunProperty.AsRunProperty();
                    RunProperty bRunProperty = b.RunProperty.AsRunProperty();

                    return textEditorDecoration.AreSameRunProperty(aRunProperty,
                        bRunProperty);
                }

                var textDecorationCharDataList = charList.Slice(offset);
                TextReadOnlyListSpan<CharData> firstCharSpanContinuous = textDecorationCharDataList.GetFirstCharSpanContinuous(Predicate);

                yield return new DecorationSplitResult(textEditorDecoration, runProperty, firstCharSpanContinuous);

                minCount = Math.Min(minCount, firstCharSpanContinuous.Count);
                dictionary[textEditorDecoration] = offset + firstCharSpanContinuous.Count;
            }

            offset += minCount;
        }
    }

    private static int SkipEmptyTextEditorDecoration(in TextReadOnlyListSpan<CharData> charList, int currentIndex)
    {
        for (var i = currentIndex; i < charList.Count; i++)
        {
            RunProperty runProperty = charList[i].RunProperty.AsRunProperty();
            if (runProperty.DecorationCollection.IsEmpty)
            {
                // 跳过
            }
            else
            {
                return currentIndex;
            }
        }

        return charList.Count; // 返回结束位置
    }
}

#if USE_SKIA
file static class RunPropertyExtension
{
    public static RunProperty AsRunProperty(this IReadOnlyRunProperty runProperty) => runProperty.AsSkiaRunProperty();
}
#endif

#endif