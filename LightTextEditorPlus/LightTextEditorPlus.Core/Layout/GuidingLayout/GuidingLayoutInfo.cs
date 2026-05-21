using System.Collections.Generic;
using System.Linq;

using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 提供一次完成布局之后的指导布局快照信息。
/// </summary>
public sealed record GuidingLayoutInfo(
    ArrangingType ArrangingType,
    IReadOnlyList<GuidingParagraphLayoutInfo> ParagraphList)
{
    /// <summary>
    /// 段落数量。
    /// </summary>
    public int ParagraphCount => ParagraphList.Count;

    /// <summary>
    /// 总行数。
    /// </summary>
    public int LineCount => ParagraphList.Sum(t => t.LineCount);

    /// <summary>
    /// 所有行包含的字符总数。
    /// </summary>
    public int CharCount => ParagraphList.Sum(t => t.CharCount);

    internal static GuidingLayoutInfo Create(RenderInfoProvider renderInfoProvider)
    {
        GuidingParagraphLayoutInfo[] paragraphList = renderInfoProvider.GetParagraphRenderInfoList()
            .Select(paragraphRenderInfo =>
            {
                GuidingLineLayoutInfo[] lineList = paragraphRenderInfo.GetLineRenderInfoList()
                    .Select(lineRenderInfo => new GuidingLineLayoutInfo(
                        lineRenderInfo.ParagraphIndex,
                        lineRenderInfo.LineIndex,
                        lineRenderInfo.CharList.Count,
                        lineRenderInfo.ContentBounds,
                        lineRenderInfo.OutlineBounds,
                        lineRenderInfo.LineLayoutData.CharStartPoint.ToCurrentArrangingTypePoint(),
                        lineRenderInfo.LineLayoutData.LineContentStartPoint.ToCurrentArrangingTypePoint()))
                    .ToArray();

                return new GuidingParagraphLayoutInfo(
                    paragraphRenderInfo.Index,
                    paragraphRenderInfo.ParagraphLayoutData.TextContentBounds,
                    paragraphRenderInfo.ParagraphLayoutData.OutlineBounds,
                    lineList);
            })
            .ToArray();

        return new GuidingLayoutInfo(
            renderInfoProvider.TextEditor.ArrangingType,
            paragraphList);
    }
}

/// <summary>
/// 提供段落级别的指导布局信息。
/// </summary>
public sealed record GuidingParagraphLayoutInfo(
    ParagraphIndex ParagraphIndex,
    TextRect ContentBounds,
    TextRect OutlineBounds,
    IReadOnlyList<GuidingLineLayoutInfo> LineList)
{
    /// <summary>
    /// 段落行数。
    /// </summary>
    public int LineCount => LineList.Count;

    /// <summary>
    /// 段落内所有行的字符数总和。
    /// </summary>
    public int CharCount => LineList.Sum(t => t.CharCount);
}

/// <summary>
/// 提供行级别的指导布局信息。
/// </summary>
public sealed record GuidingLineLayoutInfo(
    ParagraphIndex ParagraphIndex,
    int LineIndex,
    int CharCount,
    TextRect ContentBounds,
    TextRect OutlineBounds,
    TextPoint StartPoint,
    TextPoint ContentStartPoint);
