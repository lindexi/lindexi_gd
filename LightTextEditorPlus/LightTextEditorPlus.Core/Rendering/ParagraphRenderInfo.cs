using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 段落渲染信息
/// </summary>
public readonly struct ParagraphRenderInfo
{
    internal ParagraphRenderInfo(ParagraphIndex index, ParagraphData paragraphData, RenderInfoProvider renderInfoProvider)
    {
        Index = index;
        _paragraphData = paragraphData;
        _renderInfoProvider = renderInfoProvider;
    }

    /// <summary>
    /// 段落序号，这是文档里的第几段，从0开始
    /// </summary>
    public ParagraphIndex Index { get; }
    private readonly ParagraphData _paragraphData;
    private readonly RenderInfoProvider _renderInfoProvider;

    /// <summary>
    /// 段落的布局数据
    /// </summary>
    public IParagraphLayoutData ParagraphLayoutData => _paragraphData.ParagraphLayoutData;

    /// <summary>
    /// 段落属性
    /// </summary>
    public ParagraphProperty ParagraphProperty => _paragraphData.ParagraphProperty;

    /// <summary>
    /// 段落
    /// </summary>
    public ITextParagraph Paragraph => _paragraphData;

    /// <summary>
    /// 获取此段落内的行的渲染信息
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ParagraphLineRenderInfo> GetLineRenderInfoList()
    {
        for (var i = 0; i < _paragraphData.LineLayoutDataList.Count; i++)
        {
            LineLayoutData lineLayoutData = _paragraphData.LineLayoutDataList[i];

            var argument = lineLayoutData.GetLineDrawingArgument();

            _renderInfoProvider.VerifyNotDirty();

            yield return new ParagraphLineRenderInfo(lineIndex: i, paragraphIndex: Index, argument, lineLayoutData, _paragraphData.ParagraphStartRunProperty, _renderInfoProvider);
        }
    }
}
