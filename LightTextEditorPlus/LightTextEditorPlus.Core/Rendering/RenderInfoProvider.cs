using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        var documentCharCount = textEditor.DocumentManager.CharCount;
        if (caretOffset.Offset > documentCharCount)
        {
            // 超过文档的字符数量
            throw new HitCaretOffsetOutOfRangeException(textEditor, caretOffset, documentCharCount,
                nameof(caretOffset));
        }

        var paragraphManager = textEditor.DocumentManager.DocumentRunEditProvider.ParagraphManager;
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

                if (lineLayoutData.CharCount == 0)
                {
                    // 这是一个空行
                    // 如果遇到空行，那应该只有是空段才能创建空行
                    Debug.Assert(paragraphData.CharCount==0, "只有空段才能创建空行");
                    Debug.Assert(hitLineOffset == 0, "对于一个空行，难道还能计算出多个字符");
                    return new CaretRenderInfo(lineIndex, hitLineOffset, null, paragraphData, hitOffset, caretOffset,
                        lineLayoutData.GetLineBounds());
                }
                else
                {
                    var charData = lineLayoutData.GetCharList()[hitLineOffset];

                    // 预期是能找到的，如果找不到，那就是炸
                    return new CaretRenderInfo(lineIndex, hitLineOffset, charData, paragraphData, hitOffset, caretOffset, lineLayoutData.GetLineBounds());
                }
            }
        }

        // 理论上不可能进入此分支
        throw new TextEditorInnerException("无法命中光标对应的字符");
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