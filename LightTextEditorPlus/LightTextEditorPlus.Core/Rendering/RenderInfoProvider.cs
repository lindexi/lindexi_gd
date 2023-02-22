using System;
using System.Collections.Generic;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;

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