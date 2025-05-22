using System.Collections.Generic;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Document;

// todo 考虑自动项目的重新开始应该如何表示

static class MarkerRuntimeCalculator
{
    public static MarkerRuntimeInfo CalculateMarkerRuntimeInfo(in CalculateParagraphIndentArgument argument)
    {
        ParagraphData paragraphData = argument.CurrentParagraphData;
        ParagraphProperty paragraphProperty = paragraphData.ParagraphProperty;
        ParagraphIndex paragraphIndex = argument.ParagraphIndex;

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

        return default;
    }

    /// <summary>
    /// 更新段落的项目符号信息
    /// </summary>
    /// <param name="paragraphList"></param>
    public static void UpdateParagraphMarkerRuntimeInfo(IReadOnlyList<ParagraphData> paragraphList)
    {
        
    }
}