using System;
using System.Collections.Generic;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Utils;

/// <summary>
/// 只读的字符列表扩展方法
/// </summary>
public static class ReadOnlyCharDataListExtension
{
    /// <summary>
    /// 将传入的字符列表，根据判断条件，分割为一个个相邻的字符列表
    /// </summary>
    /// <param name="charDataList"></param>
    /// <param name="predicate">判断给定两个 <see cref="CharData"/> 是否符合条件</param>
    /// <returns></returns>
    public static IEnumerable<TextReadOnlyListSpan<CharData>> SplitContinuousCharData(this TextReadOnlyListSpan<CharData> charDataList,
        Func<CharData, CharData, bool> predicate)
    {
        CharData lastCharData = null!;
        int length = 0;
        for (var i = 0; i < charDataList.Count; i++)
        {
            var charData = charDataList[i];

            if (i == 0)
            {
                length++;
            }
            else
            {
                var result = predicate(lastCharData, charData);
                if (result)
                {
                    // 如果是相同的
                    length++;
                }
                else
                {
                    // 如果是不相同的，那就将当前的列表返回
                    var startIndex = i - length;

                    yield return charDataList.Slice(startIndex, length);

                    // 当前列表返回了，那就剩下当前这个一个
                    // 例子，如只有两个元素，且两个元素不相同。那第一个元素返回之后，还剩下一个元素
                    length = 1;
                }
            }

            if (i == charDataList.Count - 1)
            {
                if (length != 0)
                {
                    var startIndex = i - length + 1;

                    yield return charDataList.Slice(startIndex, length);
                }
            }

            lastCharData = charData;
        }
    }
}