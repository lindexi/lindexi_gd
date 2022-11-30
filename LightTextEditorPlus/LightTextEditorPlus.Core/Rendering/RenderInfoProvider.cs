using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Rendering;

public class RenderInfoProvider
{
    public RenderInfoProvider(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditor TextEditor { get; }

    public bool IsDirty { internal set; get; }

    public IEnumerable<ParagraphRenderInfo> GetParagraphRenderInfoList()
    {
        var paragraphManager = TextEditor.DocumentManager.DocumentRunEditProvider.ParagraphManager;
        var list = paragraphManager.GetParagraphList();
        for (var index = 0; index < list.Count; index++)
        {
            VerifyNotDirty();
            var paragraphData = list[index];
            yield return new ParagraphRenderInfo(index, paragraphData, this);
        }
    }

    internal void VerifyNotDirty()
    {
        if (IsDirty)
        {
            throw new TextEditorRenderInfoDirtyException();
        }
    }
}

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

    public IEnumerable<ParagraphLineRenderInfo> GetLineRenderInfoList()
    {
        for (var i = 0; i < _paragraphData.LineVisualDataList.Count; i++)
        {
            LineVisualData lineVisualData = _paragraphData.LineVisualDataList[i];

            var argument = lineVisualData.GetLineDrawingArgument();

            _renderInfoProvider.VerifyNotDirty();

            yield return new ParagraphLineRenderInfo(i, argument)
            {
                LineVisualData = lineVisualData
            };
        }
    }
}

/// <summary>
/// 段落的行渲染信息
/// </summary>
/// <param name="LineIndex">这一行是段落的第几行，从0开始</param>
/// <param name="Argument">行渲染参数</param>
public readonly record struct ParagraphLineRenderInfo(int LineIndex, LineDrawingArgument Argument)
{
    /// <summary>
    /// 内部使用的行信息
    /// </summary>
    /// 由于需要修改访问权限，修改为属性
    internal LineVisualData LineVisualData { init; get; } = null!;

    public void SetDrawnResult(in LineDrawnResult lineDrawnResult)
    {
        LineVisualData.SetDrawn(lineDrawnResult);
    }
}