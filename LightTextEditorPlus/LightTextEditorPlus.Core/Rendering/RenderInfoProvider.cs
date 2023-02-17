using System;
using System.Collections.Generic;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
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

    public CaretRenderInfo GetCaretRenderInfo(CaretOffset caretOffset)
    {
        var textEditor = TextEditor;
        if (caretOffset.Offset > textEditor.DocumentManager.CharCount)
        {
            // 超过文档的字符数量
            throw new ArgumentOutOfRangeException(paramName: nameof(caretOffset),
                $"DocumentManagerCharCount={textEditor.DocumentManager.CharCount};CaretOffset={caretOffset.Offset}");
        }

        var paragraphManager = textEditor.DocumentManager.DocumentRunEditProvider.ParagraphManager;
        var hitParagraphDataResult = paragraphManager.GetHitParagraphData(caretOffset);
        var paragraphData = hitParagraphDataResult.ParagraphData;
        var hitOffset = hitParagraphDataResult.HitOffset;

        if (!caretOffset.IsAtLineStart)
        {
            // 非行首情况下，一律取前一个字符
            hitOffset = new ParagraphCaretOffset(hitOffset.Offset - 1);
        }

        for (var lineIndex = 0; lineIndex < paragraphData.LineLayoutDataList.Count; lineIndex++)
        {
            var lineLayoutData = paragraphData.LineLayoutDataList[lineIndex];

            if (lineLayoutData.CharEndParagraphIndex >= hitOffset.Offset)
            {
                var hitLineOffset = hitOffset.Offset - lineLayoutData.CharStartParagraphIndex;
                var charData = lineLayoutData.GetCharList()[hitLineOffset];

                // 预期是能找到的，如果找不到，那就是炸
                return new CaretRenderInfo(lineIndex, hitLineOffset, charData, paragraphData, hitOffset, caretOffset);
            }
        }

        // 理论上不可能进入此分支
        throw new ArgumentException("无法命中光标对应的字符");
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