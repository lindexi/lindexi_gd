using LightTextEditorPlus.Core.Document;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout.HitTests;

/// <summary>
/// 基于布局的命中测试提供者
/// </summary>
class LayoutHitTestProvider
{
    public LayoutHitTestProvider(ArrangingLayoutProvider arrangingLayoutProvider)
    {
        LayoutProvider = arrangingLayoutProvider;
        LayoutManager = arrangingLayoutProvider.LayoutManager;
    }

    public LayoutManager LayoutManager { get; }

    public ArrangingLayoutProvider LayoutProvider { get; }
    public TextEditorCore TextEditor => LayoutManager.TextEditor;

    /// <summary>
    /// 命中测试
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    /// 入口方法
    public TextHitTestResult HitTest(in TextPoint point)
    {
        // 不需要通过 GetRenderInfo 方法获取，这是一个比较上层的方法了
        //TextEditor.GetRenderInfo()

        // 先判断是否命中到文档
        // 命中到文档，那就继续判断段落命中
        var documentManager = TextEditor.DocumentManager;
        var paragraphManager = documentManager.ParagraphManager;
        TextRect documentHitBounds = LayoutProvider.GetDocumentHitBounds();

        var contains = documentHitBounds.Contains(point);
        // 如果没有点到文档范围内，则处理超过范围逻辑
        if (!contains)
        {
            return GetOverBoundsHitTestResult(point, documentHitBounds);
        }

        IReadOnlyList<ParagraphData> list = paragraphManager.GetParagraphList();
        // 先进行段落的命中，再执行(xing)行(hang)命中
        var currentDocumentOffset = new DocumentOffset(0);
        for (var paragraphIndex = 0; paragraphIndex < list.Count; paragraphIndex++)
        {
            ParagraphData paragraphData = list[paragraphIndex];

            var context = new ParagraphHitTestContext(point, paragraphData, new ParagraphIndex(paragraphIndex),
                currentDocumentOffset);

            ParagraphHitTestResult result = ParagraphHitTest(in context);
            if (result.Success)
            {
                return result.Result;
            }
            else
            {
                currentDocumentOffset = result.CurrentDocumentOffset;
            }
        }

        {
            var lastParagraphData = list.Last();
            // 任何一个都没命中，那就返回命中到最后
            return new TextHitTestResult(lastParagraphData, false, false, false, documentManager.GetDocumentEndCaretOffset(),
                null, lastParagraphData.Index)
            {
                // 没有命中到字符
                IsHitSpace = true,
            };
        }

        // todo 命中测试处理竖排文本
    }

    /// <summary>
    /// 获取超过文档范围的命中测试结果
    /// </summary>
    /// <param name="point"></param>
    /// <param name="documentHitBounds"></param>
    /// <returns></returns>
    private TextHitTestResult GetOverBoundsHitTestResult(TextPoint point, TextRect documentHitBounds)
    {
        var documentManager = TextEditor.DocumentManager;
        var paragraphManager = documentManager.ParagraphManager;

        // 是否超过文本字符范围了
        const bool isOutOfTextCharacterBounds = true;
        // 是否在文档末尾
        const bool isEndOfTextCharacterBounds = false;
        // 是否在段落最后一行上
        const bool isInLastLineBounds = false;

        // 对于水平布局，顶端对齐来说，应该是只判断上下
        var isInTop = point.Y < documentHitBounds.Top;
        ParagraphData paragraphData;
        CaretOffset hitCaretOffset;
        if (isInTop)
        {
            // 在文档的上方，则取首个字符
            var firstParagraphData = paragraphManager.GetParagraphList().First();
            paragraphData = firstParagraphData;
            hitCaretOffset = documentManager.GetDocumentStartCaretOffset();
        }
        else
        {
            // 无论是在左边还是在右边，都设置为文档最后
            var lastParagraphData = paragraphManager.GetParagraphList().Last();
            paragraphData = lastParagraphData;
            hitCaretOffset = documentManager.GetDocumentEndCaretOffset();
        }

        return new TextHitTestResult(paragraphData, isOutOfTextCharacterBounds, isEndOfTextCharacterBounds, isInLastLineBounds,
            hitCaretOffset, HitCharData: null, paragraphData.Index)
        {
            // 没在文档内，那一定是命中到空白
            IsHitSpace = true,
        };
    }

    private ParagraphHitTestResult ParagraphHitTest(in ParagraphHitTestContext context)
    {
        ParagraphData paragraphData = context.ParagraphData;
        TextPoint point = context.HitPoint;
        var currentCharIndex = context.StartDocumentOffset;

        IParagraphLayoutData paragraphLayoutData = paragraphData.ParagraphLayoutData;

        var paragraphBounds = paragraphLayoutData.OutlineBounds;
        if (paragraphBounds.Contains(point))
        {
            if (!paragraphLayoutData.TextContentBounds.Contains(point))
            {
                // 快速分支，命中到段落，但预计命中到了段落的空白部分。如段落前后的空白

            }

            for (var lineIndex = 0; lineIndex < paragraphData.LineLayoutDataList.Count; lineIndex++)
            {
                LineLayoutData lineLayoutData = paragraphData.LineLayoutDataList[lineIndex];

                var lineHitTestContext = new LineHitTestContext(lineLayoutData, currentCharIndex, context);
                ParagraphHitTestResult result = LineHitTest(in lineHitTestContext).ParagraphHitTestResult;
                if (result.Success)
                {
                    return result;
                }
                else
                {
                    currentCharIndex = result.CurrentDocumentOffset;
                }
            }

            Debug.Fail($"命中到一段，但是在一段里面没有命中一行");
        }

        currentCharIndex += paragraphData.CharCount + ParagraphData.DelimiterLength;
        return ParagraphHitTestResult.OnFail(new DocumentOffset(currentCharIndex));
    }

    private LineHitTestResult LineHitTest(in LineHitTestContext context)
    {
        ParagraphData paragraphData = context.ParagraphHitTestContext.ParagraphData;
        var point = context.ParagraphHitTestContext.HitPoint;
        var hitParagraphIndex = context.ParagraphHitTestContext.ParagraphIndex;

        //var paragraphBounds = paragraphData.ParagraphLayoutData.OutlineBounds;

        var lineLayoutData = context.LineLayoutData;
        var currentCharIndex = context.StartDocumentOffset.Offset;

        var outlineBounds = lineLayoutData.OutlineBounds;
        if (outlineBounds.Contains(point))
        {
            TextRect lineContentBounds = lineLayoutData.GetLineContentBounds();
            if (!lineContentBounds.Contains(point))
            {
                // 如果行内不包含，则说明命中到了行的空白部分
                // 如居中对齐的空白
                var isLeft = point.X <= lineContentBounds.Left;
                var isRight = point.X >= lineContentBounds.Right;
                var isTop = point.Y <= lineContentBounds.Top;
                var isBottom = point.Y >= lineContentBounds.Bottom;
                CaretOffset hitCaretOffset;
                if (isLeft)
                {
                    // 点到了行的左边，那就是行首
                    var isAtLineStart = true;
                    hitCaretOffset = new CaretOffset(currentCharIndex, isAtLineStart);
                }
                else
                {
                    Debug.Assert(isRight);
                    // 点到了行的右边，那就是行末
                    var isAtLineStart = false;
                    // 但也可能是空行，空行则证明是空段，此时应该在行首
                    bool isEmptyLine = lineLayoutData.CharCount==0;
                    if (isEmptyLine)
                    {
                        Debug.Assert(paragraphData.IsEmptyParagraph, "空行必定在空段内");
                        isAtLineStart = true;
                    }
                    hitCaretOffset = new CaretOffset(currentCharIndex + lineLayoutData.CharCount, isAtLineStart);
                }

                return HitSuccess(isInLineBoundsNotHitChar: true, hitCaretOffset, hitCharData: null, isHitSpace: true);
            }

            var charList = lineLayoutData.GetCharList();
            for (var charIndex = 0; charIndex < charList.Count; charIndex++)
            {
                var charData = charList[charIndex];
                var charBounds = charData.GetBounds();
                var charHitBounds = charBounds with
                {
                    Y = lineContentBounds.Y,
                    // 字符如果是小字号，字符的范围比较小
                    Height = lineContentBounds.Height
                };
                if (charHitBounds.Contains(point))
                {
                    CaretOffset hitCaretOffset;
                    // 横排的话，需要判断命中在字符的前后，也就是前半部分还是后半部分
                    var center = charHitBounds.Center;
                    if (point.X <= center.X)
                    {
                        // 在前面
                        hitCaretOffset = new CaretOffset(currentCharIndex, isAtLineStart: charIndex == 0);
                    }
                    else
                    {
                        hitCaretOffset = new CaretOffset(currentCharIndex + 1);
                    }

                    return HitSuccess(isInLineBoundsNotHitChar: false, hitCaretOffset, charData, isHitSpace: false);
                }

                currentCharIndex++;
            }

            // 行内没有命中到字符，视为命中到最后一个字符
            return HitSuccess(isInLineBoundsNotHitChar: true, // 命中到行内，但没命中到任何字符
                new CaretOffset(currentCharIndex),
                charList.LastOrDefault(), isHitSpace: true);

            ParagraphHitTestResult HitSuccess(bool isInLineBoundsNotHitChar, CaretOffset hitCaretOffset,
                CharData? hitCharData, bool isHitSpace)
            {
                // isInLineBoundsNotHitChar 是否在一行上，尽管没有直接命中到具体的字符。例如居中的两边或行的最后
                // isHitSpace 命中到了空白部分

                // 能进入这里，必然是命中到文档内
                const bool isOutOfTextCharacterBounds = false; // 是否超过文本字符范围了
                const bool isEndOfTextCharacterBounds = false; // >在 IsOutOfTextCharacterBounds 的基础上，是否在文档末尾

                var result = new TextHitTestResult(paragraphData, isOutOfTextCharacterBounds, isEndOfTextCharacterBounds,
                    isInLineBoundsNotHitChar, hitCaretOffset, hitCharData,
                    hitParagraphIndex)
                {
                    // 命中到了空白部分
                    IsHitSpace = isHitSpace,
                };

                return ParagraphHitTestResult.OnSuccess(result);
            }
        }
        //else
        //{
        //    // 一行里面没有命中，可能是这是一个居中对齐的文本，此时需要根据排版规则，修改一行的尺寸，重新计算是否命中到某行
        //    // 这是横排的算法
        //    var unionLineBounds = new TextRect(paragraphBounds.X, outlineBounds.Y, paragraphBounds.Width,
        //        outlineBounds.Height);
        //    if (unionLineBounds.Contains(point))
        //    {
        //        // 命中到了，需要判断是行首还是行末
        //        var isLineStart = point.X <= outlineBounds.X;
        //        CaretOffset hitCaretOffset;
        //        if (isLineStart)
        //        {
        //            hitCaretOffset = new CaretOffset(currentCharIndex, isAtLineStart: true);
        //        }
        //        else
        //        {
        //            hitCaretOffset = new CaretOffset(currentCharIndex + lineLayoutData.CharCount);
        //        }

        //        return ParagraphHitTestResult.OnSuccess(new TextHitTestResult(false, false, true, hitCaretOffset, null,
        //            hitParagraphIndex)
        //        {
        //            HitParagraphData = paragraphData,
        //            // 命中到了字符
        //            IsHitSpace = false,
        //        });
        //    }
        //}

        currentCharIndex += lineLayoutData.CharCount;
        return ParagraphHitTestResult.OnFail(new DocumentOffset(currentCharIndex));
    }
}