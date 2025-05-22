using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils;

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
}