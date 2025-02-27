using LightTextEditorPlus.Core.Document;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.Maths;

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
        var logContext = new TextEditorDebugLogContext(TextEditor);
        logContext.RecordDebugMessage($"Start HitTest Point={point.ToMathPointFormat()}");

        var result = HitTestInner(in point, in logContext);

        logContext.RecordDebugMessage($"Finish HitTest Result={result}, HitPoint={point.ToMathPointFormat()}");

        TextEditor.Logger.Log(new HitTestLogInfo(point, result, logContext));

        return result;

        // todo 命中测试处理竖排文本
    }

    private TextHitTestResult HitTestInner(in TextPoint point, in TextEditorDebugLogContext logContext)
    {
        // 先判断是否命中到文档
        // 命中到文档，那就继续判断段落命中
        var documentManager = TextEditor.DocumentManager;
        var paragraphManager = documentManager.ParagraphManager;
        TextRect documentHitBounds = LayoutProvider.GetDocumentHitBounds();

        logContext.RecordDebugMessage($"开始测试文档范围是否命中");

        var contains = documentHitBounds.Contains(point);
        // 如果没有点到文档范围内，则处理超过范围逻辑
        if (!contains)
        {
            logContext.RecordDebugMessage($"没有命中到文档范围。文档范围 {documentHitBounds}");
            return GetOverBoundsHitTestResult(point, documentHitBounds);
        }

        logContext.RecordDebugMessage($"命中到文档范围。文档范围 {documentHitBounds}，继续进行段落命中");
        logContext.RecordDebugMessage($"开始段落命中测试");

        IReadOnlyList<ParagraphData> list = paragraphManager.GetParagraphList();
        // 先进行段落的命中，再执行(xing)行(hang)命中
        var currentDocumentOffset = new DocumentOffset(0);
        for (var paragraphIndex = 0; paragraphIndex < list.Count; paragraphIndex++)
        {
            logContext.RecordDebugMessage($"第 {paragraphIndex} 段命中测试");

            ParagraphData paragraphData = list[paragraphIndex];

            var context = new ParagraphHitTestContext(point, paragraphData, new ParagraphIndex(paragraphIndex),
                currentDocumentOffset, logContext);

            ParagraphHitTestResult result = ParagraphHitTest(in context);
            if (result.Success)
            {
                logContext.RecordDebugMessage($"第 {paragraphIndex} 段成功命中。命中测试结束");

                return result.Result;
            }
            else
            {
                currentDocumentOffset = result.CurrentDocumentOffset;

                logContext.RecordDebugMessage($"第 {paragraphIndex} 段没有命中");
            }
        }

        logContext.RecordDebugMessage($"没有命中到任意一段。回滚。取最后一段作为命中的段落");

        var lastParagraphData = list.Last();
        // 任何一个都没命中，那就返回命中到最后
        return new TextHitTestResult(lastParagraphData, false, false, false, documentManager.GetDocumentEndCaretOffset(),
            null, lastParagraphData.Index)
        {
            // 没有命中到字符
            IsHitSpace = true,
        };
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
        var logContext = context.LogContext;
        ParagraphData paragraphData = context.ParagraphData;
        TextPoint point = context.HitPoint;
        // 当前的文档偏移量。为什么不能用段落的偏移量？因为段落里面的偏移量也是一个获取的时候才进行计算的属性。为了提升性能，这里直接传递，就不用每个段落重新计算了
        var currentCharIndex = context.StartDocumentOffset;
        // 理论上，段落的偏移量应该和当前的文档偏移量相同。如果不同，那就是框架实现错误
        if (TextEditor.IsInDebugMode)
        {
            DocumentOffset paragraphStartOffset = paragraphData.GetParagraphStartOffset();
            if (currentCharIndex != paragraphStartOffset)
            {
                throw new TextEditorInnerDebugException($"命中测试传递的当前文档偏移量和段落的偏移量不相同 CurrentCharIndex={currentCharIndex.Offset} ParagraphStartOffset={paragraphStartOffset.Offset}");
            }
        }

        IParagraphLayoutData paragraphLayoutData = paragraphData.ParagraphLayoutData;

        Debug.Assert(paragraphData.LineLayoutDataList.Count > 0, "经过布局之后的段落必定至少有一行，即使空段也有一行");

        var paragraphBounds = paragraphLayoutData.OutlineBounds;
        if (paragraphBounds.Contains(point))
        {
            logContext.RecordDebugMessage($"段落范围包含命中点 ParagraphOutlineBounds={paragraphBounds} HitPoint={point.ToMathPointFormat()}");

            TextRect textContentBounds = paragraphLayoutData.TextContentBounds;
            if (!textContentBounds.Contains(point))
            {
                logContext.RecordDebugMessage($"段落文本范围不包含命中点，命中到段落空白部分 ParagraphTextContentBounds={textContentBounds}");

                // 快速分支，命中到段落，但预计命中到了段落的空白部分。如段落前后的空白
                // 这里当成横排文本处理
                var isLeft = point.X <= textContentBounds.Left;
                var isRight = point.X >= textContentBounds.Right;
                var isTop = point.Y <= textContentBounds.Top;
                var isBottom = point.Y >= textContentBounds.Bottom;

                // 如果是在段落前的情况，取首行命中，否则取末行命中
                LineLayoutData lineLayoutData;
                if (isLeft || isTop)
                {
                    lineLayoutData = paragraphData.LineLayoutDataList.First();
                }
                else
                {
                    Debug.Assert(isRight || isBottom);
                    lineLayoutData = paragraphData.LineLayoutDataList.Last();
                }

                TextRect lineContentBounds = lineLayoutData.GetLineContentBounds();

                double x = point.X.CoerceValue(lineContentBounds.Left + TextContext.Epsilon,
                    lineContentBounds.Right - TextContext.Epsilon);
                double y = point.Y.CoerceValue(lineContentBounds.Top + TextContext.Epsilon,
                    lineContentBounds.Bottom - TextContext.Epsilon);
                var lineHitTestContext = new LineHitTestContext(lineLayoutData, currentCharIndex, context with
                {
                    HitPoint = new TextPoint(x, y)
                });
                ParagraphHitTestResult result = LineHitTest(in lineHitTestContext).ParagraphHitTestResult;
                Debug.Assert(result.Success, "经过了约束的点，必定命中成功");
                return result with
                {
                    Result = result.Result with
                    {
                        // 这是命中到段落的空白部分
                        IsHitSpace = true,
                        // 超过文字字符范围
                        IsOutOfTextCharacterBounds = true,
                    }
                };
            }

            logContext.RecordDebugMessage($"段落文本范围包含命中点，命中到段落文本部分。开始进入行命中测试 ParagraphTextContentBounds={textContentBounds}");

            for (var lineIndex = 0; lineIndex < paragraphData.LineLayoutDataList.Count; lineIndex++)
            {
                logContext.RecordDebugMessage($"开始测试第 {context.ParagraphIndex.Index} 段，第 {lineIndex} 行命中测试");

                LineLayoutData lineLayoutData = paragraphData.LineLayoutDataList[lineIndex];

                var lineHitTestContext = new LineHitTestContext(lineLayoutData, currentCharIndex, context);
                ParagraphHitTestResult result = LineHitTest(in lineHitTestContext).ParagraphHitTestResult;
                if (result.Success)
                {
                    logContext.RecordDebugMessage($"第 {context.ParagraphIndex.Index} 段，第 {lineIndex} 行命中测试成功");
                    return result;
                }
                else
                {
                    currentCharIndex = result.CurrentDocumentOffset;
                    logContext.RecordDebugMessage($"第 {context.ParagraphIndex.Index} 段，第 {lineIndex} 行没有命中");
                }
            }

            Debug.Fail($"命中到一段，但是在一段里面没有命中一行");
        }

        logContext.RecordDebugMessage($"段落范围不包含命中点，段落没有命中 ParagraphOutlineBounds={paragraphBounds} HitPoint={point.ToMathPointFormat()}");

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
                    bool isEmptyLine = lineLayoutData.CharCount == 0;
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
                    LineLayoutData = lineLayoutData,
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