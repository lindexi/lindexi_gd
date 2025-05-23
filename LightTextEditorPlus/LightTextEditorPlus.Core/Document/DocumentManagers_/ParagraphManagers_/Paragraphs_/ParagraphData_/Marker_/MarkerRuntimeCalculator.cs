using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.Document;

static class MarkerRuntimeCalculator
{
    /// <summary>
    /// 更新段落的项目符号信息
    /// </summary>
    /// <param name="paragraphList"></param>
    /// <param name="textEditor"></param>
    public static void UpdateParagraphMarkerRuntimeInfo(IReadOnlyList<ParagraphData> paragraphList, TextEditorCore textEditor)
    {
        if (paragraphList.All(t => t.ParagraphProperty.Marker is null))
        {
            // 短路代码，没有任何一个项目符号的情况
            return;
        }

        IPlatformRunPropertyCreator platformRunPropertyCreator = textEditor.PlatformProvider.GetPlatformRunPropertyCreator();

        foreach (MarkerTextInfo markerTextInfo in GetMarkerTextInfoList(paragraphList))
        {
            ParagraphData paragraphData = markerTextInfo.ParagraphData;
            if (markerTextInfo.MarkerText is null)
            {
                continue;
            }

            ParagraphProperty paragraphProperty = paragraphData.ParagraphProperty;

            if (paragraphProperty.Marker is { } marker)
            {
                IReadOnlyRunProperty? markerRunProperty = marker.RunProperty;

                if (markerRunProperty is null || marker.ShouldFollowParagraphFirstCharRunProperty)
                {
                    IReadOnlyRunProperty styleRunProperty;
                    if (paragraphData.IsEmptyParagraph)
                    {
                        styleRunProperty = paragraphData.ParagraphStartRunProperty;
                    }
                    else
                    {
                        CharData firstCharData = paragraphData.GetCharData(new ParagraphCharOffset(0));
                        styleRunProperty = firstCharData.RunProperty;
                    }

                    markerRunProperty = platformRunPropertyCreator.UpdateMarkerRunProperty(markerRunProperty, styleRunProperty);
                }

                if (paragraphData.MarkerRuntimeInfo is {} markerRuntimeInfo)
                {
                    if (string.Equals(markerRuntimeInfo.Text, markerTextInfo.MarkerText, StringComparison.InvariantCulture) && markerRuntimeInfo.RunProperty.Equals(markerRunProperty))
                    {
                        // 没有改变
                        continue;
                    }
                }

                paragraphData.MarkerRuntimeInfo = new MarkerRuntimeInfo(markerTextInfo.MarkerText, markerRunProperty);
            }
            else
            {
                Debug.Fail("如果能拿到 MarkerText 则 ParagraphProperty.Marker 必定存在");
            }
        }
    }

    readonly record struct MarkerTextInfo(string? MarkerText, ParagraphData ParagraphData);

    private static List<MarkerTextInfo> GetMarkerTextInfoList(IReadOnlyList<ParagraphData> paragraphList)
    {
        var list = new List<MarkerTextInfo>(paragraphList.Count);

        //AutoNumberType lastAutoNumberType = default;
        //int lastAutoNumberIndex = -1; // 人类友好，从 1 开始
        var dictionary = new Dictionary<NumberMarkerGroupId, uint>();

        for (var i = 0; i < paragraphList.Count; i++)
        {
            ParagraphData paragraphData = paragraphList[i];
            ParagraphProperty paragraphProperty = paragraphData.ParagraphProperty;
            string? markerText = null;
            if (paragraphProperty.Marker is { } marker)
            {
                if (marker is BulletMarker bulletMarker)
                {
                    // 无序项目符号
                    markerText = bulletMarker.MarkerText;
                }
                else if (marker is NumberMarker numberMarker)
                {
                    // 有序项目符号
                    var currentIndex = dictionary.GetValueOrDefault(numberMarker.GroupId, 0u);
                    if (currentIndex == 0)
                    {
                        currentIndex = numberMarker.StartAt;
                    }
                    else
                    {
                        if (paragraphData.IsEmptyParagraph)
                        {
                            // 如果是空段落，则不增加编号，保持 currentIndex 不变
                            // 忽略 CS1717 建议
                            currentIndex = currentIndex;
                        }
                        else
                        {
                            currentIndex++;
                        }
                    }

                    dictionary[numberMarker.GroupId] = currentIndex;

                    markerText = numberMarker.GetMarkerText(currentIndex);
                }
            }

            list.Add(new MarkerTextInfo(markerText, paragraphData));
        }

        return list;
    }
}