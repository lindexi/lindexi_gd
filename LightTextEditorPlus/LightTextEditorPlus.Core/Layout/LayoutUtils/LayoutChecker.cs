using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils;

internal static class LayoutChecker
{
    /// <summary>
    /// 调试下判断布局是否正确
    /// </summary>
    public static void DebugCheckLayoutBeCorrected(UpdateLayoutContext updateLayoutContext)
    {
        EnsureNextStartPoint(updateLayoutContext);
        EnsureParagraphIndent(updateLayoutContext);
        EnsureMarker(updateLayoutContext);
    }

    private static void EnsureNextStartPoint(UpdateLayoutContext updateLayoutContext)
    {
        IReadOnlyList<ParagraphData> paragraphList = updateLayoutContext.InternalParagraphList;

        // 调试逻辑，理论上下一段的起始点就是等于本段最低点
        var firstParagraph = paragraphList[0];
        var currentExceptedStartPoint = GetExpectedNextStartPoint(firstParagraph);
        var lastParagraphLayoutData = firstParagraph.ParagraphLayoutData;

        for (var paragraphIndex = 1;
             paragraphIndex < paragraphList.Count;
             paragraphIndex++)
        {
            // 当前段落的起始点就等于上一段的最低点
            ParagraphData paragraphData = paragraphList[paragraphIndex];
            TextPointInDocumentContentCoordinateSystem startPoint = paragraphData.ParagraphLayoutData.StartPointInDocumentContentCoordinateSystem;

            if (!startPoint.NearlyEquals(currentExceptedStartPoint))
            {
                // 如果不相等，则证明计算不正确
                throw new TextEditorInnerDebugException($"文本段落计算之间存在空隙。当前第 {paragraphIndex} 段。上一段范围： {lastParagraphLayoutData.StartPointInDocumentContentCoordinateSystem}  {lastParagraphLayoutData.OutlineSize.ToDebugText()}，当前段的起始点 {startPoint}");
            }

            lastParagraphLayoutData = paragraphData.ParagraphLayoutData;
            currentExceptedStartPoint = GetExpectedNextStartPoint(paragraphData);
        }

        // 获取期望的下一个段的起始点
        TextPointInDocumentContentCoordinateSystem GetExpectedNextStartPoint(ParagraphData paragraph)
        {
            var layoutData = paragraph.ParagraphLayoutData;
            TextPointInDocumentContentCoordinateSystem startPoint = layoutData.StartPointInDocumentContentCoordinateSystem;
            TextSize outlineSize = layoutData.OutlineSize;
            // 当前段落的起始点就等于上一段的最低点
            return startPoint.Offset(0, outlineSize.Height);
        }
    }

    private static void EnsureParagraphIndent(UpdateLayoutContext updateLayoutContext)
    {
        IReadOnlyList<ParagraphData> paragraphList = updateLayoutContext.InternalParagraphList;
        foreach (ParagraphData paragraphData in paragraphList)
        {
            ParagraphLayoutIndentInfo indentInfo = paragraphData.ParagraphLayoutData.IndentInfo;

            indentInfo.DebugVerifyParagraphPropertyIndentInfo(paragraphData.ParagraphProperty);

            double markerIndentation = paragraphData.MarkerRuntimeInfo?.MarkerIndentation ?? 0;
            TextEditorInnerDebugAsset.AreEquals(markerIndentation, indentInfo.MarkerIndentation, "MarkerIndentation");
        }
    }

    /// <summary>
    /// 确保项目符号布局
    /// </summary>
    /// <param name="updateLayoutContext"></param>
    private static void EnsureMarker(UpdateLayoutContext updateLayoutContext)
    {
        foreach (ParagraphData paragraphData in updateLayoutContext.InternalParagraphList)
        {
            var marker = paragraphData.MarkerRuntimeInfo;

            if (marker is null)
            {
                // 没有项目符号
                continue;
            }

            TextReadOnlyListSpan<CharData> charDataList = marker.CharDataList;

            if (charDataList.Count == 0)
            {
                throw new TextEditorInnerDebugException($"包含项目符号下，必定存在项目符号字符");
            }

            foreach (CharData charData in charDataList)
            {
                if (charData.IsInvalidCharDataInfo)
                {
                    throw new TextEditorInnerDebugException($"存在项目符号字符没有在布局时计算尺寸");
                }

                if (!charData.IsSetStartPointInDebugMode)
                {
                    throw new TextEditorInnerDebugException($"存在项目符号字符没有在布局时设置坐标");
                }

                if (charData.CharLayoutData!.IsInvalidVersion())
                {
                    throw new TextEditorInnerDebugException($"存在项目符号字符缓存版本错误");
                }
            }
        }
    }
}
