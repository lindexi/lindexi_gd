using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 段落布局参数
/// </summary>
/// <param name="ParagraphIndex">段落序号</param>
/// <param name="CurrentStartPoint">段落的起点布局坐标。对于正横排布局，那就是左上角坐标。对于正竖排，也是左上角坐标。对于蒙文竖排，从右到左的竖排布局，是右上角坐标</param>
/// <param name="ParagraphData">段落数据</param>
/// <param name="ParagraphList">文档的所有段落</param>
/// <param name="UpdateLayoutContext"></param>
readonly record struct ParagraphLayoutArgument(ParagraphIndex ParagraphIndex, TextPointInDocumentContentCoordinateSystem CurrentStartPoint, ParagraphData ParagraphData, IReadOnlyList<ParagraphData> ParagraphList, UpdateLayoutContext UpdateLayoutContext)
{
    /// <summary>
    /// 是否首段
    /// </summary>
    public bool IsFirstParagraph => ParagraphIndex == 0;

    /// <summary>
    /// 是否末段
    /// </summary>
    public bool IsLastParagraph => ParagraphIndex == ParagraphList.Count - 1;
}
