using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 段落渲染信息
/// </summary>
public readonly struct ParagraphRenderInfo
{
    internal ParagraphRenderInfo(int index, ParagraphData paragraphData, RenderInfoProvider renderInfoProvider)
    {
        Index = index;
        _paragraphData = paragraphData;
        _renderInfoProvider = renderInfoProvider;
    }

    /// <summary>
    /// 段落序号，这是文档里的第几段，从0开始
    /// </summary>
    public int Index { get; }
    private readonly ParagraphData _paragraphData;
    private readonly RenderInfoProvider _renderInfoProvider;

    /// <summary>
    /// 段落的布局数据
    /// </summary>
    public IParagraphLayoutData ParagraphLayoutData => _paragraphData.ParagraphLayoutData;

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

            yield return new ParagraphLineRenderInfo(i, argument)
            {
                LineLayoutData = lineLayoutData
            };
        }
    }
}