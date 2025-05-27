using System.Collections.Generic;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;

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
    public static IEnumerable<DecorationSplitResult> SplitContinuousTextDecorationCharData(TextReadOnlyListSpan<CharData> charList)
    {
        var textDecorationCharDataList = SkipEmptyTextDecoration(charList);

        while (textDecorationCharDataList.Count > 0)
        {
            // 获取到装饰
            CharData firstCharData = textDecorationCharDataList[0];
            RunProperty runProperty = firstCharData.RunProperty.AsRunProperty();

            for (var i = 0; i < runProperty.DecorationCollection.Count; i++)
            {
                TextEditorDecoration textEditorDecoration = runProperty.DecorationCollection[i];

                bool Predicate(CharData a, CharData b)
                {
                    RunProperty aRunProperty = a.RunProperty.AsRunProperty();
                    RunProperty bRunProperty = b.RunProperty.AsRunProperty();

                    return textEditorDecoration.AreSameRunProperty(aRunProperty,
                        bRunProperty);
                }

                IEnumerable<TextReadOnlyListSpan<CharData>> splitList = textDecorationCharDataList.SplitContinuousCharData(Predicate);

                foreach (TextReadOnlyListSpan<CharData> textReadOnlyListSpan in splitList)
                {
                    yield return new DecorationSplitResult(textEditorDecoration, runProperty, textReadOnlyListSpan);
                }
            }

            textDecorationCharDataList = SkipEmptyTextDecoration(textDecorationCharDataList);
        }
    }

    private static TextReadOnlyListSpan<CharData> SkipEmptyTextDecoration(TextReadOnlyListSpan<CharData> charList)
    {
        for (var i = 0; i < charList.Count; i++)
        {
            RunProperty runProperty = charList[i].RunProperty.AsRunProperty();
            if (runProperty.DecorationCollection.IsEmpty)
            {
                // 跳过
            }
            else
            {
                return charList.Slice(i);
            }
        }

        return new TextReadOnlyListSpan<CharData>();
    }
}
