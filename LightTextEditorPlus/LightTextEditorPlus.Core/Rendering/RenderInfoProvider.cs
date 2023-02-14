using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 光标下的渲染信息
/// </summary>
public class CaretRenderInfo
{
    internal CaretRenderInfo(RenderInfoProvider renderInfoProvider, CaretOffset caretOffset)
    {
        RenderInfoProvider = renderInfoProvider;
        CaretOffset = caretOffset;

        var textEditor = renderInfoProvider.TextEditor;
        var paragraphManager = textEditor.DocumentManager.DocumentRunEditProvider.ParagraphManager;
        var hitParagraphDataResult = paragraphManager.GetHitParagraphData(caretOffset);
        ParagraphData = hitParagraphDataResult.ParagraphData;
        HitOffset = hitParagraphDataResult.HitOffset;

        for (var lineIndex = 0; lineIndex < ParagraphData.LineLayoutDataList.Count; lineIndex++)
        {
            var lineLayoutData = ParagraphData.LineLayoutDataList[lineIndex];

            if (lineLayoutData.CharEndParagraphIndex > HitOffset.Offset)
            {
                LineIndex = lineIndex;
                HitLineOffset = HitOffset.Offset - lineLayoutData.CharStartParagraphIndex;
                var charData = lineLayoutData.GetCharList()[HitLineOffset];
                CharData = charData;
                return;
            }
        }
    }

    /// <summary>
    /// 行在段落里的序号
    /// </summary>
    public int LineIndex { get; }

    /// <summary>
    /// 命中到行的哪个字符
    /// </summary>
    public int HitLineOffset { get; }

    public CharData CharData { get; }

    internal ParagraphData ParagraphData { get; }
    internal ParagraphCaretOffset HitOffset { get; }

    public RenderInfoProvider RenderInfoProvider { get; }
    public CaretOffset CaretOffset { get; }
}

public class RenderInfoProvider
{
    public RenderInfoProvider(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditor TextEditor { get; }

    public bool IsDirty { internal set; get; }

    public CaretRenderInfo GetCaretRenderInfo(CaretOffset caretOffset)
    {
        return new CaretRenderInfo(this, caretOffset);
    }

    //public Rect GetCharLayoutInfo(DocumentOffset documentOffset)
    //{

    //}

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
    internal LineLayoutData LineLayoutData { init; get; } = null!;

    public void SetDrawnResult(in LineDrawnResult lineDrawnResult)
    {
        LineLayoutData.SetDrawn(lineDrawnResult);
    }
}