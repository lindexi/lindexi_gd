using System.Collections.Generic;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Document;

// todo 考虑自动项目的重新开始应该如何表示

static class MarkerRuntimeCalculator
{
    /// <summary>
    /// 更新段落的项目符号信息
    /// </summary>
    /// <param name="paragraphList"></param>
    public static void UpdateParagraphMarkerRuntimeInfo(IReadOnlyList<ParagraphData> paragraphList)
    {
        for (var i = 0; i < paragraphList.Count; i++)
        {
            ParagraphData paragraphData = paragraphList[i];
            ParagraphProperty paragraphProperty = paragraphData.ParagraphProperty;

            if (paragraphProperty.Marker is { } marker)
            {
                if (marker is BulletMarker bulletMarker)
                {
                    // 无序项目符号
                    string? markerText = bulletMarker.MarkerText;

                }
                else if (marker is NumberMarker numberMarker)
                {
                    // 有序项目符号
                }
            }
        }
    }
}