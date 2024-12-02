using System.Diagnostics;
using System.Linq;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 实际的布局提供器
/// </summary>
abstract class ArrangingLayoutProvider
{
    protected ArrangingLayoutProvider(LayoutManager layoutManager)
    {
        LayoutManager = layoutManager;
    }

    /// <summary>
    /// 布局方式
    /// </summary>
    public abstract ArrangingType ArrangingType { get; }

    /// <summary>
    /// 布局管理器
    /// </summary>
    public LayoutManager LayoutManager { get; }

    public TextEditorCore TextEditor => LayoutManager.TextEditor;

    #region 命中测试

    public TextHitTestResult HitTest(in TextPoint point)
    {
        // 不需要通过 GetRenderInfo 方法获取，这是一个比较上层的方法了
        //TextEditor.GetRenderInfo()
        // 先判断是否命中到文档
        // 命中到文档，那就继续判断段落命中
        var documentManager = TextEditor.DocumentManager;
        var paragraphManager = documentManager.ParagraphManager;
        var documentBounds = LayoutManager.DocumentRenderData.DocumentBounds;
        var contains = documentBounds.Contains(point);
        // 如果没有点到文档范围内，则处理超过范围逻辑
        if (!contains)
        {
            // 是否超过文本字符范围了
            const bool isOutOfTextCharacterBounds = true;
            // 是否在文档末尾
            const bool isEndOfTextCharacterBounds = false;
            // 是否在段落最后一行上
            const bool isInLastLineBounds = false;

            // 对于水平布局，顶端对齐来说，应该是只判断上下
            var isInTop = point.Y < documentBounds.Top;
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

            return new TextHitTestResult(isOutOfTextCharacterBounds, isEndOfTextCharacterBounds, isInLastLineBounds,
                hitCaretOffset, HitCharData: null, paragraphData.Index)
            {
                HitParagraphData = paragraphData,
                // 没在文档内，那一定是命中到空白
                IsHitSpace = true,
            };
        }

        var list = paragraphManager.GetParagraphList();
        // 先进行段落的命中，再执行(xing)行(hang)命中
        var currentCharIndex = 0;
        for (var paragraphIndex = 0; paragraphIndex < list.Count; paragraphIndex++)
        {
            var paragraphData = list[paragraphIndex];
            var paragraphBounds = paragraphData.ParagraphLayoutData.GetBounds();
            if (paragraphBounds.Contains(point))
            {
                for (var lineIndex = 0; lineIndex < paragraphData.LineLayoutDataList.Count; lineIndex++)
                {
                    LineLayoutData lineLayoutData = paragraphData.LineLayoutDataList[lineIndex];
                    var lineBounds = lineLayoutData.GetLineBounds();
                    if (lineBounds.Contains(point))
                    {
                        var charList = lineLayoutData.GetCharList();
                        for (var charIndex = 0; charIndex < charList.Count; charIndex++)
                        {
                            var charData = charList[charIndex];
                            var charBounds = charData.GetBounds();
                            if (charBounds.Contains(point))
                            {
                                // 横排的话，需要判断命中在字符的前后，也就是前半部分还是后半部分
                                var center = charBounds.Center;
                                CaretOffset hitCaretOffset;
                                if (point.X <= center.X)
                                {
                                    // 在前面
                                    hitCaretOffset = new CaretOffset(currentCharIndex, isAtLineStart: charIndex == 0);
                                }
                                else
                                {
                                    hitCaretOffset = new CaretOffset(currentCharIndex + 1);
                                }

                                return new TextHitTestResult(false, false, false, hitCaretOffset, charData,
                                    paragraphIndex)
                                {
                                    HitParagraphData = paragraphData
                                };
                            }

                            currentCharIndex++;
                        }
                        // 行内没有命中到字符，视为命中到最后一个字符
                        return new TextHitTestResult(false, false, false, new CaretOffset(currentCharIndex), charList.LastOrDefault(),
                            paragraphIndex)
                        {
                            HitParagraphData = paragraphData,
                            // 行内没有命中到字符
                            IsHitSpace = true,
                        };
                    }
                    else
                    {
                        // 一行里面没有命中，可能是这是一个居中对齐的文本，此时需要根据排版规则，修改一行的尺寸，重新计算是否命中到某行
                        // 这是横排的算法
                        var unionLineBounds = new TextRect(paragraphBounds.X, lineBounds.Y, paragraphBounds.Width,
                            lineBounds.Height);
                        if (unionLineBounds.Contains(point))
                        {
                            // 命中到了，需要判断是行首还是行末
                            var isLineStart = point.X <= lineBounds.X;
                            CaretOffset hitCaretOffset;
                            if (isLineStart)
                            {
                                hitCaretOffset = new CaretOffset(currentCharIndex, isAtLineStart: true);
                            }
                            else
                            {
                                hitCaretOffset = new CaretOffset(currentCharIndex + lineLayoutData.CharCount);
                            }

                            return new TextHitTestResult(false, false, true, hitCaretOffset, null, paragraphIndex)
                            {
                                HitParagraphData = paragraphData,
                                // 命中到了字符
                                IsHitSpace = false,
                            };
                        }
                    }

                    currentCharIndex += lineLayoutData.CharCount;
                }
            }

            currentCharIndex += paragraphData.CharCount + ParagraphData.DelimiterLength;
        }

        {
            var lastParagraphData = list.Last();
            // 任何一个都没命中，那就返回命中到最后
            return new TextHitTestResult(false, false, false, documentManager.GetDocumentEndCaretOffset(),
                null, lastParagraphData.Index)
            {
                HitParagraphData = lastParagraphData,

                // 没有命中到字符
                IsHitSpace = true,
            };
        }

        // todo 命中测试处理竖排文本
    }

    #endregion

    public DocumentLayoutResult UpdateLayout()
    {
        // todo 项目符号的段落，如果在段落上方新建段落，那需要项目符号更新
        // 这个逻辑准备给项目符号逻辑更新，逻辑是，假如现在有两段，分别采用 `1. 2.` 作为项目符号
        // 在 `1.` 后面新建一个段落，需要自动将原本的 `2.` 修改为 `3.` 的内容，这个逻辑准备交给项目符号模块自己编辑实现

        // 布局逻辑：
        // - 获取需要更新布局段落的逻辑
        // - 进入段落布局
        //   - 进入行布局
        // - 获取文档整个的布局信息
        //   - 获取文档的布局尺寸

        // 首行出现变脏的序号
        var firstDirtyParagraphIndex = -1;
        // 首个脏段的起始 也就是横排左上角的点。等于非脏段的下一个行起点
        TextPoint firstStartPoint = default;

        var paragraphList = TextEditor.DocumentManager.ParagraphManager.GetParagraphList();

        Debug.Assert(paragraphList.Count > 0, "获取到的段落，即使空文本也会存在一段");

        for (var index = 0; index < paragraphList.Count; index++)
        {
            ParagraphData paragraphData = paragraphList[index];
            if (paragraphData.IsDirty())
            {
                firstDirtyParagraphIndex = index;

                if (index == 0)
                {
                    // 从首段落开始
                    firstStartPoint = new TextPoint(0, 0);
                }
                else
                {
                    // 非首段开始，需要进行不同的排版计算。例如横排和竖排的规则不相同
                    var lastParagraph = paragraphList[index - 1];
                    firstStartPoint = GetNextParagraphLineStartPoint(lastParagraph);
                }

                break;
            }
        }

        if (firstDirtyParagraphIndex == -1)
        {
            throw new TextEditorInnerException($"进入布局时，没有任何一段需要布局");
        }

        //// 进入各个段落的段落之间和行之间的布局

        // 进入段落内布局
        var currentStartPoint = firstStartPoint;
        for (var index = firstDirtyParagraphIndex; index < paragraphList.Count; index++)
        {
            ParagraphData paragraphData = paragraphList[index];

            var argument = new ParagraphLayoutArgument(index, currentStartPoint, paragraphData, paragraphList);

            ParagraphLayoutResult result = LayoutParagraph(argument);
            currentStartPoint = result.NextLineStartPoint;
        }

        var documentBounds = TextRect.Zero;
        foreach (var paragraphData in paragraphList)
        {
            var bounds = paragraphData.ParagraphLayoutData.GetBounds();
            documentBounds = documentBounds.Union(bounds);
        }

        Debug.Assert(TextEditor.DocumentManager.ParagraphManager.GetParagraphList()
            .All(t => t.IsDirty() == false));

        return new DocumentLayoutResult(documentBounds);
    }

    /// <summary>
    /// 更新段落的左上角坐标
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected abstract ParagraphLayoutResult UpdateParagraphStartPoint(in ParagraphLayoutArgument argument);

    /// <summary>
    /// 段落内布局
    /// </summary>
    private ParagraphLayoutResult LayoutParagraph(in ParagraphLayoutArgument argument)
    {
        // 如果段落本身是没有脏的，可能是当前段落的前面段落变更，导致需要更新段落的左上角坐标点而已
        // 这里执行快速的短路代码，提升性能
        if (!argument.ParagraphData.IsDirty())
        {
            return UpdateParagraphStartPoint(argument);
        }

        // 先找到首个需要更新的坐标点，这里的坐标是段坐标
        var dirtyParagraphOffset = 0;
        // 首个是脏的行的序号
        var lastIndex = -1;
        var paragraph = argument.ParagraphData;
        for (var index = 0; index < paragraph.LineLayoutDataList.Count; index++)
        {
            LineLayoutData lineLayoutData = paragraph.LineLayoutDataList[index];
            if (lineLayoutData.IsDirty == false)
            {
                dirtyParagraphOffset += lineLayoutData.CharCount;
            }
            else
            {
                lastIndex = index;
                break;
            }
        }

        // 将脏的行移除掉，然后重新添加新的行
        // 例如在一段里面，首行就是脏的，那么此时应该就是从 0 开始，将后续所有行都移除掉
        // 例如在一段里面，有三行，首行不是脏的，第一行是脏的，那就需要删除第一行和第二行
        if (lastIndex == 0)
        {
            // 一段的首行是脏的，将后续全部删掉
            foreach (var lineLayoutData in paragraph.LineLayoutDataList)
            {
                lineLayoutData.Dispose();
            }

            paragraph.LineLayoutDataList.Clear();
        }
        else if (lastIndex > 0)
        {
            for (var i = lastIndex; i < paragraph.LineLayoutDataList.Count; i++)
            {
                var lineVisualData = paragraph.LineLayoutDataList[i];
                lineVisualData.Dispose();
            }

            paragraph.LineLayoutDataList.RemoveRange(lastIndex, paragraph.LineLayoutDataList.Count - lastIndex);
        }
        else
        {
            // 这一段一个脏的行都没有。那可能是直接在空段追加，或者是首次布局
            Debug.Assert(paragraph.LineLayoutDataList.Count == 0);
        }

        // 不需要通过如此复杂的逻辑获取有哪些，因为存在的坑在于后续分拆 IImmutableRun 逻辑将会复杂
        //paragraph.GetRunRange(dirtyParagraphOffset);
        
        var startParagraphOffset = new ParagraphCharOffset(dirtyParagraphOffset);

        var result = LayoutParagraphCore(argument, startParagraphOffset);

#if DEBUG
        // 排版的结果如何？通过段落里面的每一行的信息，可以了解
        var lineVisualDataList = paragraph.LineLayoutDataList;
        foreach (var lineLayoutData in lineVisualDataList)
        {
            // 每一行有多少个字符，字符的坐标
            var charList = lineLayoutData.GetCharList();
            foreach (var charData in charList)
            {
                // 字符的坐标是多少
                var startPoint = charData.GetStartPoint();
                _ = startPoint;
            }
        }
#endif

        return result;
    }

    /// <summary>
    /// 排版和测量布局段落，处理段落内布局
    /// </summary>
    /// 这是一段一段进行排版和测量布局
    /// <param name="paragraph"></param>
    /// <param name="startParagraphOffset"></param>
    /// <returns></returns>
    protected abstract ParagraphLayoutResult LayoutParagraphCore(in ParagraphLayoutArgument paragraph,
        in ParagraphCharOffset startParagraphOffset);

    /// <summary>
    /// 测量空段高度。空段的文本行高度包括行距，不包括段前和段后距离
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected EmptyParagraphLineHeightMeasureResult MeasureEmptyParagraphLineHeight(in EmptyParagraphLineHeightMeasureArgument argument)
    {
        var emptyParagraphLineHeightMeasurer = TextEditor.PlatformProvider.GetEmptyParagraphLineHeightMeasurer();
        if (emptyParagraphLineHeightMeasurer != null)
        {
            return emptyParagraphLineHeightMeasurer.MeasureEmptyParagraphLineHeight(argument);
        }
        else
        {
            var paragraphProperty = argument.ParagraphProperty;

            var runProperty = paragraphProperty.ParagraphStartRunProperty;
            runProperty ??= TextEditor.DocumentManager.CurrentRunProperty;

            var lineSpacingCalculateArgument =
                new LineSpacingCalculateArgument(argument.ParagraphIndex, 0, paragraphProperty, runProperty);
            var lineSpacingCalculateResult = CalculateLineSpacing(lineSpacingCalculateArgument);
            double lineHeight = lineSpacingCalculateResult.TotalLineHeight;
            if (lineSpacingCalculateResult.ShouldUseCharLineHeight)
            {
                // 如果需要使用文本高度，那么进行
                // 测量空行文本
                CharInfoMeasureResult charInfoMeasureResult = MeasureEmptyParagraphLineSize(runProperty);

                lineHeight = charInfoMeasureResult.Bounds.Height;
            }

            return new EmptyParagraphLineHeightMeasureResult(lineHeight);
        }
    }

    /// <summary>
    /// 测量使用 <paramref name="runProperty"/> 的空段文本行的字符高度
    /// </summary>
    /// <param name="runProperty"></param>
    /// <returns></returns>
    private CharInfoMeasureResult MeasureEmptyParagraphLineSize(IReadOnlyRunProperty runProperty)
    {
        var singleCharObject = new SingleCharObject(TextContext.DefaultChar);
        var charInfo = new CharInfo(singleCharObject, runProperty);

        var charInfoMeasurer = TextEditor.PlatformProvider.GetCharInfoMeasurer();

        CharInfoMeasureResult charInfoMeasureResult;
        if (charInfoMeasurer != null)
        {
            // 测量空行高度
            charInfoMeasureResult = charInfoMeasurer.MeasureCharInfo(charInfo);
        }
        else
        {
            charInfoMeasureResult = MeasureCharInfo(charInfo);
        }

        return charInfoMeasureResult;
    }

    /// <summary>
    /// 获取下一段的首行起始点
    /// </summary>
    /// <param name="paragraphData"></param>
    /// <returns></returns>
    /// 对于横排来说，是往下排。对于竖排来说，也许是往左也许是往右排
    protected abstract TextPoint GetNextParagraphLineStartPoint(ParagraphData paragraphData);

    #region 行距

    /// <summary>
    /// 计算行距
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected LineSpacingCalculateResult CalculateLineSpacing(in LineSpacingCalculateArgument argument)
    {
        var lineSpacingCalculator = TextEditor.PlatformProvider.GetLineSpacingCalculator();

        if (lineSpacingCalculator != null)
        {
            return lineSpacingCalculator.CalculateLineSpacing(argument);
        }

        // 没有注入平台相关的行距计算器的情况下，以下是默认逻辑

        ParagraphProperty paragraphProperty = argument.ParagraphProperty;

        double lineHeight;
        if (double.IsNaN(paragraphProperty.FixedLineSpacing))
        {
            // 倍数行距逻辑
            var lineSpacing = paragraphProperty.LineSpacing;

            var needNotCalculateLineSpacing =
                // 处理首行不展开，文档的首段首行不加上行距
                // 也就是不需要处理 lineHeight 的值
                TextEditor.LineSpacingStrategy == LineSpacingStrategy.FirstLineShrink
                && argument.ParagraphIndex == 0
                && argument.LineIndex == 0;

            if (needNotCalculateLineSpacing)
            {
                // 如果不需要计算行距，那就随意了
                return new LineSpacingCalculateResult(true, double.NaN);
            }
            else
            {
                lineHeight =
                    LineSpacingCalculator.CalculateLineHeightWithLineSpacing(TextEditor,
                        argument.MaxFontSizeCharRunProperty,
                        lineSpacing);
            }
        }
        else
        {
            // 如果定义了固定行距，那就使用固定行距
            lineHeight = paragraphProperty.FixedLineSpacing;
        }

        return new LineSpacingCalculateResult(ShouldUseCharLineHeight: false, lineHeight);
    }

    #endregion

    #region 通用辅助方法

    /// <summary>
    /// 通用的测量字符信息的方法，直接就是设置宽度高度为字号大小
    /// </summary>
    /// <param name="charInfo"></param>
    /// <returns></returns>
    protected CharInfoMeasureResult MeasureCharInfo(CharInfo charInfo)
    {
        var bounds = new TextRect(0, 0, charInfo.RunProperty.FontSize, charInfo.RunProperty.FontSize);
        return new CharInfoMeasureResult(bounds);
    }

    #endregion
}