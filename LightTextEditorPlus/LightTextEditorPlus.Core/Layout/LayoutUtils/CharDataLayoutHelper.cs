using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive.Collections;

using System.Collections.Generic;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils;

static class CharDataLayoutHelper
{
    /// <summary>
    /// 获取给定行的最大字号的字符属性。这个属性就是这一行的代表属性
    /// </summary>
    /// <param name="charDataList"></param>
    /// <returns></returns>
    public static CharData GetMaxFontSizeCharData(in TextReadOnlyListSpan<CharData> charDataList)
    {
        CharData firstCharData = charDataList[0];
        var maxFontSizeCharData = firstCharData;
        // 遍历这一行的所有字符，找到最大字符的字符属性
        for (var i = 1; i < charDataList.Count; i++)
        {
            var charData = charDataList[i];
            if (charData.RunProperty.FontSize > maxFontSizeCharData.RunProperty.FontSize)
            {
                maxFontSizeCharData = charData;
            }
        }

        return maxFontSizeCharData;
    }

    /// <summary>
    /// 清理所有的字符数据
    /// </summary>
    /// <param name="paragraphList"></param>
    /// <param name="context"></param>
    public static void ClearAllCharDataInfo(IReadOnlyList<ParagraphData> paragraphList, UpdateLayoutContext context)
    {
        foreach (ParagraphData paragraphData in paragraphList)
        {
            if (context.IsInDebugMode)
            {
                if (!paragraphData.IsDirty())
                {
                    throw new TextEditorInnerDebugException("清空全部的字符数据时，必然是段落脏的");
                }
            }

            foreach (CharData charData in paragraphData.GetParagraphCharDataList())
            {
                charData.ClearCharDataInfo();
            }
        }
    }
}