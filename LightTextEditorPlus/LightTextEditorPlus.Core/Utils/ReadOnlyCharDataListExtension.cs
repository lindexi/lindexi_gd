using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Utils;

public static class ReadOnlyCharDataListExtension
{
    /// <summary>
    /// 将传入的字符列表，根据判断条件，分割为一个个相邻的字符列表
    /// </summary>
    /// <param name="charDataList"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static IEnumerable<ReadOnlyListSpan<CharData>> SplitContinuousCharData(this ReadOnlyListSpan<CharData> charDataList,
        Func<CharData, CharData, bool> predicate)
    {
        CharData lastCharData = null!;
        int startIndex = 0;
        for (var i = 0; i < charDataList.Count; i++)
        {
            var charData = charDataList[i];

            if (i == 0)
            {
                lastCharData = charData;
            }
            else
            {
               var result = predicate(lastCharData,charData);
               if (result)
               {
                   // 如果是相同的，那就继续 i++ 读取下一个字符
               }
               else
               {
                   var length = i - startIndex + 1;

                   yield return charDataList.Slice(i, length);

                   startIndex = i;
               }
            }

            if (i == charDataList.Count - 1)
            {
                if (startIndex != i)
                {
                    var length = i - startIndex + 1;

                    yield return charDataList.Slice(i, length);
                }
            }
        }
    }
}