using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
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
        else
        {
            var remain = selection.Length;
            bool isFirstLine = true;
            LineLayoutData startLine = caretStartRenderInfo.LineLayoutData;
            var currentLine = startLine;
            var list = new List<Rect>();

            while (remain > 0)
            {
                var startHitLineOffset = 0;
                if (isFirstLine)
                {
                    if (caretStartRenderInfo.CaretOffset.IsAtLineStart)
                    {
                        startHitLineOffset = caretStartRenderInfo.HitLineOffset;
                    }
                    else
                    {
                        startHitLineOffset = caretStartRenderInfo.HitLineOffset + 1;
                    }
                }

                if (startHitLineOffset >= currentLine.CharCount)
                {
                    //// 例如进入段末
                    //// 先只添加段末
                    //var isParagraphEnd = currentLine.CharStartParagraphIndex + startHitLineOffset >
                    //                     currentLine.CurrentParagraph.CharCount;
                    //if (isParagraphEnd)
                    //{
                    //    // 这是段末。段末可选加上一个段末选择内容
                    //    remain -= ParagraphData.DelimiterLength;
                    //}
                }
                else
                {
                    var takeLength = Math.Min(remain, currentLine.CharCount - startHitLineOffset);

                    var charList = currentLine.GetCharList();
                    var startCharData = charList[startHitLineOffset];
                    var endCharData = charList[startHitLineOffset + takeLength];
                    var bounds = startCharData.GetBounds().Union(endCharData.GetBounds());
                    list.Add(bounds);

                    remain -= takeLength;
                }

                isFirstLine = false;

                if (remain > 0)
                {
                    // 切到下一行
                    // 如果需要跨段，自动减去换行字符
                    var lineLayoutDataList = currentLine.CurrentParagraph.LineLayoutDataList;
                    var nextLineIndex = currentLine.LineInParagraphIndex + 1;
                    if (nextLineIndex < lineLayoutDataList.Count)
                    {
                        currentLine = lineLayoutDataList[nextLineIndex];
                    }
                    else
                    {
                        // 需要到下一段了
                        var currentParagraphIndex = currentLine.CurrentParagraph.Index;
                        var paragraphList = currentLine.CurrentParagraph.ParagraphManager.GetParagraphList();

                        var nextParagraphIndex = currentParagraphIndex + 1;
                        if (nextParagraphIndex < paragraphList.Count)
                        {
                            var paragraphData = paragraphList[nextParagraphIndex];
                            currentLine = paragraphData.LineLayoutDataList.First();
                        }
                        else
                        {
                            // 文档结束了
                            break;
                        }

                        //自动减去换行字符
                        remain -= ParagraphData.DelimiterLength;
                    }
                }
            }

            return list;
        }
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