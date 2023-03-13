using System;
using System.Collections.Generic;
using System.Diagnostics;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 提供用于渲染时用到的信息
/// </summary>
public class RenderInfoProvider
{
    internal RenderInfoProvider(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    internal TextEditor TextEditor { get; }

    /// <summary>
    /// 当前渲染信息是否脏的。如果是脏的就不能使用
    /// </summary>
    public bool IsDirty { internal set; get; }

    /// <summary>
    /// 获取选择对应的范围。一般是一行一个 Rect 对象
    /// </summary>
    /// <param name="selection"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public IList<Rect> GetSelectionBoundsList(in Selection selection)
    {
        if (selection.IsEmpty)
        {
            return new Rect[0];
        }

        // 一行内
        // 相邻行
        // 跨多行
        var caretStartRenderInfo = GetCaretRenderInfo(selection.FrontOffset);

        var caretEndRenderInfo = GetCaretRenderInfo(selection.BehindOffset);

        // 是否单行
        if (ReferenceEquals(caretStartRenderInfo.LineLayoutData, caretEndRenderInfo.LineLayoutData))
        {
            Rect startBounds;
            if (caretStartRenderInfo.CaretOffset.IsAtLineStart)
            {
                startBounds = caretStartRenderInfo.CharData!.GetBounds();
            }
            else
            {
                // 首个是在光标之后
                var hitCharData = caretStartRenderInfo.GetCharDataAfterCaretOffset()!;
                startBounds = hitCharData.GetBounds();
            }

            var endBounds = caretEndRenderInfo.CharData!.GetBounds();

            var bounds = startBounds.Union(endBounds);
            return new Rect[] { bounds };
        }

        throw new NotSupportedException();
    }

    /// <summary>
    /// 获取给定光标坐标的光标渲染信息
    /// </summary>
    /// <param name="caretOffset"></param>
    /// <returns></returns>
    /// <exception cref="HitCaretOffsetOutOfRangeException"></exception>
    /// <exception cref="TextEditorInnerException"></exception>
    public CaretRenderInfo GetCaretRenderInfo(CaretOffset caretOffset)
    {
        var textEditor = TextEditor;
        var documentCharCount = textEditor.DocumentManager.CharCount;
        if (caretOffset.Offset > documentCharCount)
        {
            // 超过文档的字符数量
            throw new HitCaretOffsetOutOfRangeException(textEditor, caretOffset, documentCharCount,
                nameof(caretOffset));
        }

        var paragraphManager = textEditor.DocumentManager.ParagraphManager;
        var hitParagraphDataResult = paragraphManager.GetHitParagraphData(caretOffset);
        var paragraphData = hitParagraphDataResult.ParagraphData;
        var hitOffset = hitParagraphDataResult.HitOffset;
        // 是否段首，如果是段首，那一定就是行首
        var isParagraphStart = hitOffset.Offset == 0;

        if (!caretOffset.IsAtLineStart && !isParagraphStart)
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

                return new CaretRenderInfo(lineIndex, hitLineOffset, hitOffset, caretOffset, lineLayoutData);
            }
        }

        // 理论上不可能进入此分支
        throw new TextEditorInnerException("无法命中光标对应的字符");
    }

    //public Rect GetCharLayoutInfo(DocumentOffset documentOffset)
    //{
    //}

    /// <summary>
    /// 获取段落的渲染信息
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ParagraphRenderInfo> GetParagraphRenderInfoList()
    {
        var paragraphManager = TextEditor.DocumentManager.ParagraphManager;
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