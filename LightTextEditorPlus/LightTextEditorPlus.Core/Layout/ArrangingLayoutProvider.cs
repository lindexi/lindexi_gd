using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 实际的布局提供器
/// </summary>
/// 段落排版关系
/// ------------------------------------------
/// |段起始点
/// | // 段前间距 ParagraphBefore
/// | --------------------------------------
/// | |行起始点  
/// | |    // 行
/// | |         
/// | --------------------------------------
/// |
/// | // 段后间距 ParagraphAfter
/// -------------------------------------------
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

    /// <summary>
    /// 是否进入调试模式
    /// </summary>
    protected bool IsInDebugMode => TextEditor.IsInDebugMode;

    /// <summary>
    /// 更新布局
    /// </summary>
    /// <returns></returns>
    /// 入口方法
    public DocumentLayoutResult UpdateLayout()
    {
        // 布局逻辑：
        // - 01 获取需要更新布局段落的逻辑
        // - 02 预布局阶段
        //   - 进入段落布局
        //     - 进入行布局
        // - 03 回溯最终布局阶段
        //   - 获取文档整个的布局信息
        //     - 获取文档的布局尺寸
        //   - 段落回溯布局
        //     - 行回溯布局
        //     - 水平对齐的右对齐、居中对齐等
        //     - 垂直对齐的下对齐、居中对齐等
        var updateLayoutContext = new UpdateLayoutContext(this);

        updateLayoutContext.RecordDebugLayoutInfo($"开始布局");

        IReadOnlyList<ParagraphData> paragraphList = updateLayoutContext.InternalParagraphList;
        Debug.Assert(paragraphList.Count > 0, "获取到的段落，即使空文本也会存在一段");

        // 01 获取需要更新布局段落的逻辑
        FirstDirtyParagraphInfo firstDirtyParagraphInfo = GetFirstDirtyParagraph(paragraphList, updateLayoutContext);

        // 02 预布局阶段
        PreUpdateDocumentLayoutResult preUpdateDocumentLayoutResult =
            PreUpdateDocumentLayout(paragraphList, updateLayoutContext, firstDirtyParagraphInfo);

        // 03 回溯最终布局阶段
        FinalUpdateDocumentLayout(preUpdateDocumentLayoutResult, updateLayoutContext);

        if (IsInDebugMode)
        {
            // 进入一些校验逻辑
            EnsureFinishLayoutCompletedInDebugMode();
        }

        // 这是多余的判断，仅仅用在 DEBUG 过程中，没开 IsInDebugMode 的逻辑
        Debug.Assert(TextEditor.DocumentManager.ParagraphManager.GetParagraphList()
            .All(t => t.IsDirty() == false));

        updateLayoutContext.SetLayoutCompleted();

        return new DocumentLayoutResult(preUpdateDocumentLayoutResult.DocumentBounds, updateLayoutContext);
    }

    /// <summary>
    /// 调试模式下确保布局完成
    /// </summary>
    /// <exception cref="TextEditorInnerDebugException"></exception>
    private void EnsureFinishLayoutCompletedInDebugMode()
    {
        foreach (ParagraphData paragraphData in TextEditor.DocumentManager.ParagraphManager.GetParagraphList())
        {
            if (paragraphData.IsDirty())
            {
                throw new TextEditorInnerDebugException($"完成布局之后，段落还是脏的");
            }

            IParagraphLayoutData paragraphLayoutData = paragraphData.ParagraphLayoutData;
            if (paragraphLayoutData.StartPoint ==
                TextContext.InvalidStartPoint)
            {
                throw new TextEditorInnerDebugException($"完成布局之后，没有更新段落的文本起始点布局信息");
            }
            if (paragraphLayoutData.TextSize == TextSize.Invalid)
            {
                throw new TextEditorInnerDebugException($"完成布局之后，没有更新段落的文本尺寸布局信息");
            }
            if (paragraphLayoutData.OutlineSize == TextSize.Invalid)
            {
                throw new TextEditorInnerDebugException($"完成布局之后，没有更新段落的文本外接尺寸布局信息");
            }
        }
    }

    #region 01 获取需要更新布局段落的逻辑

    /// <summary>
    /// 获取首个变脏的段落
    /// </summary>
    /// <param name="paragraphList"></param>
    /// <param name="updateLayoutContext"></param>
    private FirstDirtyParagraphInfo GetFirstDirtyParagraph(IReadOnlyList<ParagraphData> paragraphList,
        UpdateLayoutContext updateLayoutContext)
    {
        // todo 项目符号的段落，如果在段落上方新建段落，那需要项目符号更新
        // 这个逻辑准备给项目符号逻辑更新，逻辑是，假如现在有两段，分别采用 `1. 2.` 作为项目符号
        // 在 `1.` 后面新建一个段落，需要自动将原本的 `2.` 修改为 `3.` 的内容，这个逻辑准备交给项目符号模块自己编辑实现

        updateLayoutContext.RecordDebugLayoutInfo($"开始寻找首个变脏段落序号");

        // 首行出现变脏的序号
        var firstDirtyParagraphIndex = -1;
        // 首个脏段的起始 也就是横排左上角的点。等于非脏段的下一个行起点
        TextPoint firstStartPoint = default;

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

        updateLayoutContext.RecordDebugLayoutInfo(
            $"完成寻找首个变脏段落序号。首个变脏的段落序号是： {firstDirtyParagraphIndex}；首个脏段的起始点：{firstStartPoint}");

        return new FirstDirtyParagraphInfo(new ParagraphIndex(firstDirtyParagraphIndex), firstStartPoint);
    }

    /// <summary>
    /// 获取首个变脏的段落的序号和起始点
    /// </summary>
    /// <param name="FirstDirtyParagraphIndex">首个出现变脏的序号</param>
    /// <param name="FirstStartPoint">首个脏段的起始 也就是横排左上角的点。等于非脏段的下一个行起点</param>
    private readonly record struct FirstDirtyParagraphInfo(
        ParagraphIndex FirstDirtyParagraphIndex,
        TextPoint FirstStartPoint);

    #endregion 获取需要更新布局段落的逻辑

    #region 02 预布局阶段

    /// <summary>
    /// 预布局文档
    /// </summary>
    /// <param name="paragraphList"></param>
    /// <param name="updateLayoutContext"></param>
    /// <param name="firstDirtyParagraphInfo"></param>
    /// <returns></returns>
    /// 等价于 BuildRenderData 阶段。只是文本库将布局和渲染分开了，所以这里只是获取布局信息
    private PreUpdateDocumentLayoutResult PreUpdateDocumentLayout(IReadOnlyList<ParagraphData> paragraphList,
        UpdateLayoutContext updateLayoutContext, in FirstDirtyParagraphInfo firstDirtyParagraphInfo)
    {
        // firstDirtyParagraphIndex - 首行出现变脏的序号
        // firstStartPoint - 首个脏段的起始 也就是横排左上角的点。等于非脏段的下一个行起点
        (ParagraphIndex firstDirtyParagraphIndex, TextPoint firstStartPoint) = firstDirtyParagraphInfo;

        // 进入段落内布局
        var currentStartPoint = firstStartPoint;
        for (var index = firstDirtyParagraphIndex.Index; index < paragraphList.Count; index++)
        {
            updateLayoutContext.RecordDebugLayoutInfo($"开始预布局第 {index} 段");
            ParagraphData paragraphData = paragraphList[index];

            var argument = new ParagraphLayoutArgument(new ParagraphIndex(index), currentStartPoint, paragraphData,
                paragraphList, updateLayoutContext);

            ParagraphLayoutResult result = UpdateParagraphLayout(argument);
            var nextParagraphStartPoint = result.NextParagraphStartPoint;
            // 预布局过程中，没有获取其 Outline 的值。 于是 OutlineBounds={paragraphData.ParagraphLayoutData.OutlineBounds}; 将在无缓存时，为 {X=0 Y=0 Width=0 Height=0} 的值
            updateLayoutContext.RecordDebugLayoutInfo($"完成预布局第 {index} 段TextBounds={paragraphData.ParagraphLayoutData.TextContentBounds};NextParagraphStartPoint={nextParagraphStartPoint}");
            currentStartPoint = nextParagraphStartPoint;

            if (IsInDebugMode)
            {
                // 预期此时完成了起始点和文本尺寸的布局了，即已经有值了
                if (paragraphData.ParagraphLayoutData.TextSize == TextSize.Invalid)
                {
                    throw new TextEditorInnerDebugException($"完成预布局第 {index} 段之后，没有更新段落的文本尺寸布局信息");
                }

                if (paragraphData.ParagraphLayoutData.StartPoint ==
                    TextContext.InvalidStartPoint)
                {
                    throw new TextEditorInnerDebugException($"完成预布局第 {index} 段之后，没有更新段落的文本起始点布局信息");
                }

                var nextY = nextParagraphStartPoint.Y;
                var exceptedY = argument.CurrentStartPoint.Y + argument.GetParagraphBefore() +
                                paragraphData.ParagraphLayoutData.TextSize.Height + argument.GetParagraphAfter();
                if (!Nearly.Equals(nextY, exceptedY))
                {
                    // 预期下一个段落的起始点是当前段落的起始点 + 当前段落的段前间距 + 当前段落的文本高度 + 当前段落的段后间距
                }
            }
        }

        TextRect documentBounds = TextRect.Zero;
        foreach (var paragraphData in paragraphList)
        {
            var bounds = paragraphData.ParagraphLayoutData.TextContentBounds;
            documentBounds = documentBounds.Union(bounds);
        }

        return new PreUpdateDocumentLayoutResult(documentBounds);
    }

    /// <summary>
    /// 预布局文档的结果
    /// </summary>
    /// <param name="DocumentBounds">文档的范围</param>
    protected readonly record struct PreUpdateDocumentLayoutResult(TextRect DocumentBounds);

    /// <summary>
    /// 段落内布局
    /// </summary>
    private ParagraphLayoutResult UpdateParagraphLayout(in ParagraphLayoutArgument argument)
    {
        // 如果段落本身是没有脏的，可能是当前段落的前面段落变更，导致需要更新段落的左上角坐标点而已
        // 这里执行快速的短路代码，提升性能
        UpdateLayoutContext context = argument.UpdateLayoutContext;
        if (!argument.ParagraphData.IsDirty())
        {
            context.RecordDebugLayoutInfo($"段落本身没有脏，进入快速分支，只需更新段落起始点坐标");
            return UpdateParagraphStartPoint(argument);
        }
        else
        {
            // 继续执行段落内布局
        }

        context.RecordDebugLayoutInfo($"段落是脏的，执行段落内布局");
        argument.ParagraphData.SetLayoutDirty(exceptTextSize: false/*应该是连文本尺寸都是脏的*/);

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

        context.RecordDebugLayoutInfo($"段内第 {lastIndex} 行是脏的，从此行开始布局");

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

        var result = UpdateParagraphLayoutCore(argument, startParagraphOffset);

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
    protected abstract ParagraphLayoutResult UpdateParagraphLayoutCore(in ParagraphLayoutArgument paragraph,
        in ParagraphCharOffset startParagraphOffset);

    /// <summary>
    /// 更新段落的左上角坐标
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected abstract ParagraphLayoutResult UpdateParagraphStartPoint(in ParagraphLayoutArgument argument);

    /// <summary>
    /// 测量空段高度。空段的文本行高度包括行距，不包括段前和段后间距
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected EmptyParagraphLineHeightMeasureResult MeasureEmptyParagraphLineHeight(
        in EmptyParagraphLineHeightMeasureArgument argument)
    {
        var emptyParagraphLineHeightMeasurer = TextEditor.PlatformProvider.GetEmptyParagraphLineHeightMeasurer();
        if (emptyParagraphLineHeightMeasurer != null)
        {
            return emptyParagraphLineHeightMeasurer.MeasureEmptyParagraphLineHeight(argument);
        }
        else
        {
            var paragraphProperty = argument.ParagraphProperty;

            var runProperty = argument.ParagraphStartRunProperty;

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
        ITextLineSpacing textLineSpacing = paragraphProperty.LineSpacing;

        double lineHeight;
        if (textLineSpacing is MultipleTextLineSpace multipleTextLineSpace)
        {
            // 倍数行距逻辑
            var lineSpacing = multipleTextLineSpace.LineSpacing;

            var needNotCalculateLineSpacing =
                // 处理首行不展开，文档的首段首行不加上行距
                // 也就是不需要处理 lineHeight 的值
                TextEditor.LineSpacingStrategy == LineSpacingStrategy.FirstLineShrink
                && argument.ParagraphIndex == 0
                && argument.LineIndex == 0;

            if (needNotCalculateLineSpacing)
            {
                // 如果不需要计算行距，那就随意了
                return new LineSpacingCalculateResult(true, double.NaN, LineSpacing: 0);
            }
            else
            {
                lineHeight =
                    LineSpacingCalculator.CalculateLineHeightWithLineSpacing(TextEditor,
                        argument.MaxFontSizeCharRunProperty,
                        lineSpacing);
            }
        }
        else if (textLineSpacing is ExactlyTextLineSpace exactlyTextLineSpace)
        {
            // 如果定义了固定行距，那就使用固定行距
            lineHeight = exactlyTextLineSpace.ExactlyLineHeight;
        }
        else
        {
            throw new NotSupportedException(
                $"传入的行距为 {textLineSpacing?.GetType()} 类型，无法在文本框框架内处理。可重写 {nameof(ILineSpacingCalculator)} 处理器自行处理此行距类型");
        }

        return new LineSpacingCalculateResult(ShouldUseCharLineHeight: false, lineHeight, lineHeight);
    }

    #endregion

    #endregion 02 预布局阶段

    #region 03 回溯最终布局阶段

    /// <summary>
    /// 回溯文档布局排版。例如右对齐、居中对齐等
    /// </summary>
    /// Rewind Polished Document Layout 回溯也是抛光的过程，抛光是指对文档的最后一次布局调整
    protected abstract void FinalUpdateDocumentLayout(PreUpdateDocumentLayoutResult preUpdateDocumentLayoutResult,
        UpdateLayoutContext updateLayoutContext);

    #endregion 03 回溯最终布局阶段

    #region 通用辅助方法

    /// <summary>
    /// 通用的测量字符信息的方法，直接就是设置宽度高度为字号大小
    /// </summary>
    /// <param name="charInfo"></param>
    /// <returns></returns>
    protected CharInfoMeasureResult MeasureCharInfo(CharInfo charInfo)
    {
        double fontSize = charInfo.RunProperty.FontSize;
        var bounds = new TextRect(0, 0, fontSize, fontSize);
        // 设置基线为字号大小的向上一点点
        const double
            testBaselineRatio = 4d / 5; // 这是一个测试值，确保无 UI 框架下，都采用相同的基线值，方便调试计算。这个值是如何获取的？通过在 PPT 里面进行测量微软雅黑字体的基线的
        double baseline = fontSize * testBaselineRatio;
        return new CharInfoMeasureResult(bounds, baseline);
    }

    /// <summary>
    /// 获取文档的命中范围
    /// </summary>
    /// <returns></returns>
    public TextRect GetDocumentHitBounds()
    {
        var documentBounds = LayoutManager.DocumentRenderData.DocumentBounds;
        return CalculateHitBounds(in documentBounds);
    }

    /// <summary>
    /// 根据传入的文档范围计算命中范围
    /// </summary>
    /// <param name="documentBounds"></param>
    /// <returns></returns>
    protected abstract TextRect CalculateHitBounds(in TextRect documentBounds);

    #endregion 通用辅助方法
}
