using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 提供用于渲染时用到的信息
/// </summary>
public class RenderInfoProvider
{
    internal RenderInfoProvider(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    /// <summary>
    /// 文本编辑器
    /// </summary>
    public TextEditorCore TextEditor { get; }

    /// <summary>
    /// 当前渲染信息是否脏的。如果是脏的就不能使用
    /// </summary>
    public bool IsDirty { internal set; get; }

    /// <inheritdoc cref="LightTextEditorPlus.Core.TextEditorCore.GetDocumentLayoutBounds"/>
    public TextRect GetDocumentLayoutBounds() => TextEditor.GetDocumentLayoutBounds();

    /// <summary>
    /// 获取选择对应的范围。一般是一行一个 Rect 对象
    /// </summary>
    /// <param name="selection"></param>
    /// <returns></returns>
    public IReadOnlyList<TextRect> GetSelectionBoundsList(in Selection selection)
    {
        if (selection.IsEmpty)
        {
#pragma warning disable IDE0300 // 简化集合初始化。因为几乎不可能进入此分支，所以不简化
            return new TextRect[0];
#pragma warning restore IDE0300 // 简化集合初始化
        }

        var result = new List<TextRect>();
        LineLayoutData? currentLineLayoutData = null;
        TextRect currentBounds = default;
        CharData? lastCharData = null;
        foreach (var charData in TextEditor.DocumentManager.GetCharDataRange(selection))
        {
            if (ReferenceEquals(charData.CharObject, LineBreakCharObject.Instance))
            {
                // 这是换段了
                continue;
            }

            var sameLine = currentLineLayoutData is not null && ReferenceEquals(charData.CharLayoutData?.CurrentLine, currentLineLayoutData);

            if (sameLine)
            {
                // 在相同的一行里面，先不加入计算
            }
            else
            {
                if (currentLineLayoutData is not null)
                {
                    var lastBounds = lastCharData!.GetBounds();
                    result.Add(currentBounds.Union(lastBounds));
                }

                currentLineLayoutData = charData.CharLayoutData?.CurrentLine;
                currentBounds = charData.GetBounds();
            }

            lastCharData = charData;
        }

        if (lastCharData is not null)
        {
            var lastBounds = lastCharData!.GetBounds();
            result.Add(currentBounds.Union(lastBounds));
        }

        return result;
    }

    /// <summary>
    /// 获取当前文本的光标的光标渲染信息
    /// </summary>
    /// <returns></returns>
    public CaretRenderInfo GetCurrentCaretRenderInfo()
    {
        var textEditor = TextEditor;
        CaretOffset currentCaretOffset = textEditor.CurrentCaretOffset;
        return GetCaretRenderInfo(currentCaretOffset);
    }

    /// <summary>
    /// 获取给定光标坐标的光标渲染信息
    /// </summary>
    /// <param name="caretOffset"></param>
    /// <param name="isTestingLineStart">测试是否属于行的开始光标。不会影响任何行为，只影响调试输出</param>
    /// <returns></returns>
    /// <exception cref="HitCaretOffsetOutOfRangeException"></exception>
    /// <exception cref="TextEditorInnerException"></exception>
    public CaretRenderInfo GetCaretRenderInfo(CaretOffset caretOffset, bool isTestingLineStart = false)
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
        ParagraphCaretOffset hitParagraphCaretOffset = hitParagraphDataResult.HitOffset;
        // 是否段首，如果是段首，那一定就是行首
        var isParagraphStart = hitParagraphCaretOffset.Offset == 0;

        if (isParagraphStart)
        {
            // 段首，短路代码，特殊执行，不需要走循环
            var lineIndex = 0;
            var lineLayoutData = paragraphData.LineLayoutDataList[0];
            // 段落起始下，取段落首个字符，如可能取到 \n 将拿不到 CharData 对象
            // 如“|abc”的情况，应该取字符 a 作为命中的字符
            var hitLineCharOffset = new LineCharOffset(0);
            var hitLineCaretOffset = new LineCaretOffset(0);
            return new CaretRenderInfo(TextEditor, lineIndex, hitLineCharOffset, hitLineCaretOffset, hitParagraphCaretOffset, caretOffset, lineLayoutData);
        }
        else if (hitParagraphCaretOffset.Offset == paragraphData.CharCount)
        {
            // 短路代码，如果命中到段末。这个逻辑可以快速判断，不需要走循环
            var lineIndex = paragraphData.LineLayoutDataList.Count - 1;
            var lineLayoutData = paragraphData.LineLayoutDataList[lineIndex];

            // 以下代码的 -1 的逻辑是：
            // 段落起始下，取段落首个字符，如可能取到 \n 将拿不到 CharData 对象
            // 非段落起始，取光标前一个字符
            // 如“abc|”的情况，应该取字符 c 作为命中的字符
            var hitCharOffset = hitParagraphCaretOffset.Offset - 1;

            var hitLineCharOffset = new LineCharOffset(hitCharOffset - lineLayoutData.CharStartParagraphIndex);
            var hitLineCaretOffset =
                new LineCaretOffset(hitParagraphCaretOffset.Offset - lineLayoutData.CharStartParagraphIndex);

            return new CaretRenderInfo(TextEditor, lineIndex, hitLineCharOffset, hitLineCaretOffset, hitParagraphCaretOffset, caretOffset, lineLayoutData);
        }

        for (var lineIndex = 0; lineIndex < paragraphData.LineLayoutDataList.Count; lineIndex++)
        {
            var lineLayoutData = paragraphData.LineLayoutDataList[lineIndex];

            if
            (
                // 如果行超过命中范围，符合预期，这就是表示命中到行内
                lineLayoutData.CharEndParagraphIndex > hitParagraphCaretOffset.Offset
                // 如果行刚好等于光标，那就是需要判断光标是否设置在行首，如果非行首情况下，那就是命中到当前行。是行首的情况下，等下次循环进入时，判断行超过命中范围
                // 只有在命中到行末情况下，才判断光标是否在行首，如此可以解决传入参数将一个非行首的光标骗在行首
                || (lineLayoutData.CharEndParagraphIndex == hitParagraphCaretOffset.Offset &&
                    !caretOffset.IsAtLineStart)
            )
            {
                // 命中到行末，但是此时光标设置非行首情况
                var hitLineCaretOffset =
                    new LineCaretOffset(hitParagraphCaretOffset.Offset - lineLayoutData.CharStartParagraphIndex);
                LineCharOffset hitLineCharOffset;
                if (hitLineCaretOffset.Offset == 0)
                {
                    // 命中到行首的情况
                    Debug.Assert(caretOffset.IsAtLineStart, "命中到行首时，应该传入光标才是真的行首");
                    hitLineCharOffset = new LineCharOffset(0);
                }
                else
                {
                    if (caretOffset.IsAtLineStart && !isTestingLineStart)
                    {
                        // 光标参数在骗人，毕竟框架能处理，那就记录日志吧
                        TextEditor.Logger.LogDebug("不是命中到行首时，不应该设置光标为行首");
                    }

                    // 以下代码的 -1 的逻辑是：
                    // 取光标前一个字符
                    // 如“a|bc”的情况，应该取字符 a 作为命中的字符
                    hitLineCharOffset = new LineCharOffset(hitLineCaretOffset.Offset - 1);
                }

                return new CaretRenderInfo(TextEditor, lineIndex, hitLineCharOffset, hitLineCaretOffset, hitParagraphCaretOffset, caretOffset, lineLayoutData);
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
            yield return new ParagraphRenderInfo(new ParagraphIndex(index), paragraphData, this);
        }
    }

    /// <summary>
    /// 确保当前渲染信息不是脏的
    /// </summary>
    /// <exception cref="TextEditorRenderInfoDirtyException"></exception>
    [EditorBrowsable(EditorBrowsableState.Never)] // 表示不要提示，因为这个方法是内部使用的，基本上业务开发者也用不着
    public void VerifyNotDirty()
    {
        // 这里不能用 TextEditor.VerifyNotDirty 方法，因为调用的 IsDirty 不是一个属性
        if (IsDirty)
        {
            throw new TextEditorRenderInfoDirtyException(TextEditor);
        }
    }
}
