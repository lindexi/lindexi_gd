using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;
using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 布局管理器
/// </summary>
/// 布局和渲染是拆开的，先进行布局再进行渲染
/// 布局的
// todo 文本公式混排 文本图片混排 文本和其他元素的混排多选 文本和其他可交互元素混排的光标策略
class LayoutManager
{
    public LayoutManager(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditor TextEditor { get; }
    public event EventHandler? InternalLayoutCompleted;

    public void UpdateLayout()
    {
        if (ArrangingLayoutProvider?.ArrangingType != TextEditor.ArrangingType)
        {
            ArrangingLayoutProvider = TextEditor.ArrangingType switch
            {
                ArrangingType.Horizontal => new HorizontalArrangingLayoutProvider(TextEditor),
                // todo 支持竖排文本
                _ => throw new NotSupportedException()
            };
        }

        var result = ArrangingLayoutProvider.UpdateLayout();
        DocumentRenderData.DocumentBounds = result.DocumentBounds;
        //DocumentRenderData.IsDirty = false;

        InternalLayoutCompleted?.Invoke(this, EventArgs.Empty);
    }

    private ArrangingLayoutProvider? ArrangingLayoutProvider { set; get; }

    public DocumentRenderData DocumentRenderData { get; } = new DocumentRenderData()
    {
        //IsDirty = true,
    };
}

/// <summary>
/// 水平方向布局的提供器
/// </summary>
class HorizontalArrangingLayoutProvider : ArrangingLayoutProvider
{
    public HorizontalArrangingLayoutProvider(TextEditorCore textEditor) : base(textEditor)
    {
    }

    public override ArrangingType ArrangingType => ArrangingType.Horizontal;

    /// <summary>
    /// 布局段落的核心逻辑
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="startParagraphOffset">开始布局的字符，刚好是一行的开始</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <remarks>
    /// 逻辑上是：
    /// 布局按照： 文本-段落-行-Run-字符
    /// 布局整个文本
    /// 布局文本的每个段落 <see cref="LayoutParagraphCore"/>
    /// 段落里面，需要对每一行进行布局 <see cref="LayoutWholeLine"/>
    /// 每一行里面，需要对每个 Char 字符进行布局 <see cref="LayoutSingleCharInLine"/>
    /// 每个字符需要调用平台的测量 <see cref="ArrangingLayoutProvider.MeasureCharInfo"/>
    /// </remarks>
    protected override ParagraphLayoutResult LayoutParagraphCore(ParagraphLayoutArgument argument,
        ParagraphCharOffset startParagraphOffset)
    {
        // 先更新非脏的行的坐标
        // 布局左上角坐标
        var currentStartPoint = argument.CurrentStartPoint;
        ParagraphData paragraph = argument.ParagraphData;

        foreach (LineLayoutData lineVisualData in argument.ParagraphData.LineVisualDataList)
        {
            UpdateLineVisualDataStartPoint(lineVisualData, currentStartPoint);

            currentStartPoint = GetNextLineStartPoint(currentStartPoint, lineVisualData);
        }

        //// 当前行的 RunList 列表，看起来设计不对，没有加上在段落的坐标
        //var currentLineRunList = new List<IImmutableRun>();

        // 获取最大宽度信息
        double lineMaxWidth = TextEditor.SizeToContent switch
        {
            SizeToContent.Manual => TextEditor.DocumentManager.DocumentWidth,
            SizeToContent.Width => double.PositiveInfinity,
            SizeToContent.Height => TextEditor.DocumentManager.DocumentHeight,
            SizeToContent.WidthAndHeight => double.PositiveInfinity,
            _ => throw new ArgumentOutOfRangeException()
        };

        var wholeRunLineLayouter = TextEditor.PlatformProvider.GetWholeRunLineLayouter();

        for (var i = startParagraphOffset.Offset; i < paragraph.CharCount;)
        {
            // 开始行布局
            // 第一个 Run 就是行的开始
            ReadOnlyListSpan<CharData> charDataList = paragraph.ToReadOnlyListSpan(i);

            if (TextEditor.IsInDebugMode)
            {
                // 这是调试代码，判断是否在布局过程，漏掉某个字符
                foreach (var charData in charDataList)
                {
                    charData.IsSetStartPointInDebugMode = false;
                }
            }

            WholeLineLayoutResult result;
            var wholeRunLineLayoutArgument = new WholeLineLayoutArgument(paragraph.ParagraphProperty, charDataList,
                lineMaxWidth, currentStartPoint);
            if (wholeRunLineLayouter != null)
            {
                result = wholeRunLineLayouter.LayoutWholeLine(wholeRunLineLayoutArgument);
            }
            else
            {
                // 继续往下执行，如果没有注入自定义的行布局层的话
                result = LayoutWholeLine(wholeRunLineLayoutArgument);
            }

            // 当前的行布局信息
            var currentLineLayoutData = new LineLayoutData(paragraph)
            {
                CharStartParagraphIndex = i,
                CharEndParagraphIndex = i + result.CharCount,
                Size = result.Size,
                StartPoint = currentStartPoint,
            };
            // 更新字符信息
            Debug.Assert(result.CharCount <= charDataList.Count, "所获取的行的字符数量不能超过可提供布局的行的字符数量");
            for (var index = 0; index < result.CharCount; index++)
            {
                var charData = charDataList[index];

                if (TextEditor.IsInDebugMode)
                {
                    if (charData.IsSetStartPointInDebugMode == false)
                    {
                        throw new TextEditorDebugException($"存在某个字符没有在布局时设置坐标",
                            (charData, currentLineLayoutData, i + index));
                    }
                }

                charData.CharLayoutData!.CharIndex = new ParagraphCharOffset(i + index);
                charData.CharLayoutData.CurrentLine = currentLineLayoutData;
                charData.CharLayoutData.UpdateVersion();
            }

            paragraph.LineVisualDataList.Add(currentLineLayoutData);

            i += result.CharCount;

            if (result.CharCount == 0)
            {
                if (TextEditor.IsInDebugMode)
                {
                    throw new TextEditorDebugException($"某一行在布局时，只采用了零个字符");
                }

                // todo 理论上不可能，表示行布局出错了
                TextEditor.Logger.LogWarning($"某一行在布局时，只采用了零个字符");
                throw new NotImplementedException();
            }

            currentStartPoint = GetNextLineStartPoint(currentStartPoint, currentLineLayoutData);
        }

        // todo 考虑行复用，例如刚好添加的内容是一行。或者在一行内做文本替换等
        // 这个没有啥优先级。测试了 SublimeText 和 NotePad 工具，都没有做此复用，预计有坑

        argument.ParagraphData.ParagraphLayoutData.StartPoint = argument.ParagraphData.LineVisualDataList[0].StartPoint;
        argument.ParagraphData.ParagraphLayoutData.Size = BuildParagraphSize(argument);

        // 设置当前段落已经布局完成
        paragraph.SetFinishLayout();

        return new ParagraphLayoutResult(currentStartPoint);
    }

    private WholeLineLayoutResult LayoutWholeLine(WholeLineLayoutArgument argument)
    {
        var (paragraphProperty, charDataList, lineMaxWidth, currentStartPoint) = argument;

        var singleRunLineLayouter = TextEditor.PlatformProvider.GetSingleRunLineLayouter();

        // RunLineMeasurer
        // 行还剩余的空闲宽度
        double lineRemainingWidth = lineMaxWidth;

        int currentIndex = 0;
        var currentSize = Size.Zero;

        while (currentIndex < charDataList.Count)
        {
            // 一行里面需要逐个字符进行布局
            var arguments = new SingleCharInLineLayoutArgument(charDataList, currentIndex, lineRemainingWidth,
                paragraphProperty);

            SingleCharInLineLayoutResult result;
            if (singleRunLineLayouter is not null)
            {
                result = singleRunLineLayouter.LayoutSingleCharInLine(arguments);
            }
            else
            {
                result = LayoutSingleCharInLine(arguments);
            }

            if (result.CanTake)
            {
                currentSize = currentSize.HorizontalUnion(result.TotalSize);

                if (result.TakeCount == 1)
                {
                    var charData = charDataList[currentIndex];
                    charData.Size ??= result.TotalSize;
                }
                else
                {
                    for (int i = currentIndex; i < currentIndex + result.TakeCount; i++)
                    {
                        var charData = charDataList[i];
                        //charData.CharRenderData ??=
                        //    new CharRenderData(charData, paragraph);
                        charData.Size ??= result.CharSizeList![i - currentIndex];
                    }
                }

                currentIndex += result.TakeCount;

                // 计算此行剩余的宽度
                lineRemainingWidth -= result.TotalSize.Width;
            }

            if (result.ShouldBreakLine)
            {
                // 换行，这一行布局完成
                break;
            }
        }

        // todo 这里可以支持两端对齐
        // 整行的字符布局
        // CorrectCharBounds AdaptBaseLine
        var wholeCharCount = currentIndex;
        // todo 这里需要处理行距和段距
        var lineTop = currentStartPoint.Y;
        var currentX = 0d;
        var lineHeight = currentSize.Height;
        for (var i = 0; i < wholeCharCount; i++)
        {
            // 简单版本的 AdaptBaseLine 方法，正确的做法是：
            // 1. 求出最大字符的 Baseline 出来
            // 2. 求出当前字符的 Baseline 出来
            // 3. 求出 最大字符的 Baseline 和 当前字符的 Baseline 的差，此结果叠加 lineTop 就是偏移量了
            // 这里只使用简单的方法，求尺寸和行高的差，让字符底部对齐
            var charData = charDataList[i];
            Debug.Assert(charData.Size != null, "charData.Size != null");
            var charDataSize = charData.Size!.Value;

            var yOffset = (lineHeight - charDataSize.Height) + lineTop;
            charData.SetStartPoint(new Point(currentX, yOffset));

            currentX += charDataSize.Width;
        }

        return new WholeLineLayoutResult(currentSize, wholeCharCount);
    }

    private SingleCharInLineLayoutResult LayoutSingleCharInLine(SingleCharInLineLayoutArgument argument)
    {
        var charInfoMeasurer = TextEditor.PlatformProvider.GetCharInfoMeasurer();

        var charData = argument.CurrentCharData;

        // 字符可能自己缓存有了自己的尺寸，如果有缓存，那是可以重复使用
        var cacheSize = charData.Size;

        Size size;
        if (cacheSize == null)
        {
            var charInfo = new CharInfo(charData.CharObject, charData.RunProperty);
            CharInfoMeasureResult charInfoMeasureResult;
            if (charInfoMeasurer != null)
            {
                charInfoMeasureResult = charInfoMeasurer.MeasureCharInfo(charInfo);
            }
            else
            {
                charInfoMeasureResult = MeasureCharInfo(charInfo);
            }

            size = charInfoMeasureResult.Bounds.Size;
        }
        else
        {
            size = cacheSize.Value;
        }

        if (argument.LineRemainingWidth > size.Width)
        {
            return new SingleCharInLineLayoutResult(takeCount: 1, size, charSizeList: default);
        }
        else
        {
            // 如果尺寸不足，也就是一个都拿不到
            return new SingleCharInLineLayoutResult(takeCount: 0, default, charSizeList: default);
        }
    }

    #region 辅助方法

    /// <summary>
    /// 获取下一行的起始点
    /// </summary>
    /// 对于横排布局来说，只是更新 Y 值即可
    /// <param name="currentStartPoint"></param>
    /// <param name="currentLineLayoutData"></param>
    /// <returns></returns>
    private static Point GetNextLineStartPoint(Point currentStartPoint, LineLayoutData currentLineLayoutData)
    {
        currentStartPoint = new Point(currentStartPoint.X, currentStartPoint.Y + currentLineLayoutData.Size.Height);
        return currentStartPoint;
    }

    /// <summary>
    /// 重新更新每一行的起始点。例如现在第一段的文本加了一行，那第二段的所有文本都需要更新每一行的起始点，而不需要重新布局第二段
    /// </summary>
    /// <param name="lineLayoutData"></param>
    /// <param name="startPoint"></param>
    static void UpdateLineVisualDataStartPoint(LineLayoutData lineLayoutData, Point startPoint)
    {
        // 更新包括两个方面：
        // 1. 此行的起点
        // 2. 此行内的所有字符的起点坐标
        var currentStartPoint = startPoint;
        lineLayoutData.StartPoint = currentStartPoint;
        // 更新行内所有字符的坐标
        var lineTop = currentStartPoint.Y;
        var list = lineLayoutData.GetCharList();

        for (var index = 0; index < list.Count; index++)
        {
            var charData = list[index];

            Debug.Assert(charData.CharLayoutData is not null);

            charData.CharLayoutData!.StartPoint = new Point(charData.CharLayoutData.StartPoint.X, lineTop);
            charData.CharLayoutData.UpdateVersion();
        }

        lineLayoutData.UpdateVersion();
    }

    private static Size BuildParagraphSize(in ParagraphLayoutArgument argument)
    {
        var paragraphSize = new Size(0, 0);
        foreach (var lineVisualData in argument.ParagraphData.LineVisualDataList)
        {
            var width = Math.Max(paragraphSize.Width, lineVisualData.Size.Width);
            var height = paragraphSize.Height + lineVisualData.Size.Height;

            paragraphSize = new Size(width, height);
        }

        return paragraphSize;
    }

    #endregion

    /// <summary>
    /// 测量空段的行高
    /// </summary>
    /// <returns></returns>
    protected override EmptyParagraphLineHeightMeasureResult MeasureEmptyParagraphLineHeightCore(
        in CharInfoMeasureResult charInfoMeasureResult)
    {
        // 下一行的开始就是这一行的行高
        var height = charInfoMeasureResult.Bounds.Height;
        var nextLineStartPoint = new Point(0, height);
        // 段落没有宽度，只有高度
        var paragraphBounds = new Rect(0, 0, 0, height);
        return new EmptyParagraphLineHeightMeasureResult(paragraphBounds, nextLineStartPoint);
    }

    protected override Point GetNextParagraphLineStartPoint(ParagraphData paragraphData)
    {
        const double x = 0;
        var layoutData = paragraphData.ParagraphLayoutData;
        var y = layoutData.StartPoint.Y + layoutData.Size.Height;
        return new Point(x, y);

        // 以下是通过最后一行的值进行计算的。不足的是需要判断空段，因此不如使用段落偏移加上段落高度进行计算
        //if (paragraphData.LineVisualDataList.Count == 0)
        //{
        //    // 这一段没有包含任何的行。这一段里面可能没有任何文本，只是一个 \r\n 的空段
        //    Debug.Assert(paragraphData.CharCount == 0,"只有空段才没有包含行");

        //    const double x = 0;
        //    var layoutData = paragraphData.ParagraphLayoutData;
        //    var y = layoutData.StartPoint.Y + layoutData.Size.Height;
        //    return new Point(x, y);
        //}
        //else
        //{
        //    var lineVisualData = paragraphData.LineVisualDataList.Last();
        //    const double x = 0;
        //    var y = lineVisualData.StartPoint.Y + lineVisualData.Size.Height;
        //    return new Point(x, y);
        //}
    }
}

/// <summary>
/// 实际的布局提供器
/// </summary>
abstract class ArrangingLayoutProvider
{
    protected ArrangingLayoutProvider(TextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    public abstract ArrangingType ArrangingType { get; }
    public TextEditor TextEditor { get; }

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
        Point firstStartPoint = default;

        var paragraphList = TextEditor.DocumentManager.DocumentRunEditProvider.ParagraphManager.GetParagraphList();
        Debug.Assert(paragraphList.Count > 0);
        for (var index = 0; index < paragraphList.Count; index++)
        {
            ParagraphData paragraphData = paragraphList[index];
            if (paragraphData.IsDirty())
            {
                firstDirtyParagraphIndex = index;

                if (index == 0)
                {
                    // 从首段落开始
                    firstStartPoint = new Point(0, 0);
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
            throw new NotImplementedException($"进入布局时，没有任何一段需要布局");
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

        var documentBounds = Rect.Zero;
        foreach (var paragraphData in paragraphList)
        {
            var bounds = paragraphData.ParagraphLayoutData.GetBounds();
            documentBounds = documentBounds.Union(bounds);
        }

        Debug.Assert(TextEditor.DocumentManager.DocumentRunEditProvider.ParagraphManager.GetParagraphList()
            .All(t => t.IsDirty() == false));

        return new DocumentLayoutResult(documentBounds);
    }

    /// <summary>
    /// 段落内布局
    /// </summary>
    private ParagraphLayoutResult LayoutParagraph(ParagraphLayoutArgument argument)
    {
        // 先找到首个需要更新的坐标点，这里的坐标是段坐标
        var dirtyParagraphOffset = 0;
        // 首个是脏的行的序号
        var lastIndex = -1;
        var paragraph = argument.ParagraphData;
        for (var index = 0; index < paragraph.LineVisualDataList.Count; index++)
        {
            LineLayoutData lineLayoutData = paragraph.LineVisualDataList[index];
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
            foreach (var lineLayoutData in paragraph.LineVisualDataList)
            {
                lineLayoutData.Dispose();
            }

            paragraph.LineVisualDataList.Clear();
        }
        else if (lastIndex > 0)
        {
            for (var i = lastIndex; i < paragraph.LineVisualDataList.Count; i++)
            {
                var lineVisualData = paragraph.LineVisualDataList[i];
                lineVisualData.Dispose();
            }

            paragraph.LineVisualDataList.RemoveRange(lastIndex, paragraph.LineVisualDataList.Count - lastIndex);
        }
        else
        {
            // 这一段一个脏的行都没有。那可能是直接在空段追加，或者是首次布局
            Debug.Assert(paragraph.LineVisualDataList.Count == 0);
        }

        // 不需要通过如此复杂的逻辑获取有哪些，因为存在的坑在于后续分拆 IImmutableRun 逻辑将会复杂
        //paragraph.GetRunRange(dirtyParagraphOffset);

        // 段落布局规则：
        // 1. 先判断是不是空段，空段的意思是这一段里没有任何字符。如果是空段，则进行空段高度测量计算
        // 2. 非空段情况下，进入具体的排版测量，如横排和竖排文本的段落测量方法进行测量排版
        if (paragraph.CharCount == 0)
        {
            // 考虑 paragraph.TextRunList 数量为空的情况，只有一个换行的情况
            // 使用空行测量器，测量空行高度
            var emptyParagraphLineHeightMeasureArgument =
                new EmptyParagraphLineHeightMeasureArgument(argument.ParagraphData.ParagraphProperty);

            EmptyParagraphLineHeightMeasureResult result;

            var emptyParagraphLineHeightMeasurer = TextEditor.PlatformProvider.GetEmptyParagraphLineHeightMeasurer();
            if (emptyParagraphLineHeightMeasurer != null)
            {
                // 有具体平台的测量，那就采用具体平台的测量
                result = emptyParagraphLineHeightMeasurer.MeasureEmptyParagraphLineHeight(
                    emptyParagraphLineHeightMeasureArgument);
            }
            else
            {
                result = MeasureEmptyParagraphLineHeight(emptyParagraphLineHeightMeasureArgument);
            }

            argument.ParagraphData.ParagraphLayoutData.StartPoint = argument.CurrentStartPoint;
            argument.ParagraphData.ParagraphLayoutData.Size = result.ParagraphBounds.Size;

            // 设置当前段落已经布局完成
            paragraph.SetFinishLayout();

            return new ParagraphLayoutResult(result.NextLineStartPoint);
        }
        else
        {
            var startParagraphOffset = new ParagraphCharOffset(dirtyParagraphOffset);

            var result = LayoutParagraphCore(argument, startParagraphOffset);

#if DEBUG
            // 排版的结果如何？通过段落里面的每一行的信息，可以了解
            var lineVisualDataList = paragraph.LineVisualDataList;
            foreach (var lineLayoutData in lineVisualDataList)
            {
                // 每一行有多少个字符，字符的坐标
                var charList = lineLayoutData.GetCharList();
                foreach (var charData in charList)
                {
                    // 字符的坐标是多少
                    var startPoint = charData.GetStartPoint();
                }
            }
#endif

            return result;
        }
    }

    /// <summary>
    /// 排版和测量布局段落，处理段落内布局
    /// </summary>
    /// 这是一段一段进行排版和测量布局
    /// <param name="paragraph"></param>
    /// <param name="startParagraphOffset"></param>
    /// <returns></returns>
    protected abstract ParagraphLayoutResult LayoutParagraphCore(ParagraphLayoutArgument paragraph,
        ParagraphCharOffset startParagraphOffset);

    /// <summary>
    /// 测量空段的行高
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    private EmptyParagraphLineHeightMeasureResult MeasureEmptyParagraphLineHeight(
        in EmptyParagraphLineHeightMeasureArgument argument)
    {
        var paragraphProperty = argument.ParagraphProperty;

        var runProperty = paragraphProperty.ParagraphStartRunProperty;
        runProperty ??= TextEditor.DocumentManager.CurrentRunProperty;
        var singleCharObject = new SingleCharObject(TextContext.DefaultChar);

        var charInfo = new CharInfo(singleCharObject, runProperty);
        var charInfoMeasurer = TextEditor.PlatformProvider.GetCharInfoMeasurer();

        CharInfoMeasureResult charInfoMeasureResult;
        if (charInfoMeasurer != null)
        {
            charInfoMeasureResult = charInfoMeasurer.MeasureCharInfo(charInfo);
        }
        else
        {
            charInfoMeasureResult = MeasureCharInfo(charInfo);
        }

        return MeasureEmptyParagraphLineHeightCore(charInfoMeasureResult);
    }

    /// <summary>
    /// 测量空段的行高
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    protected abstract EmptyParagraphLineHeightMeasureResult MeasureEmptyParagraphLineHeightCore(
        in CharInfoMeasureResult argument);

    /// <summary>
    /// 获取下一段的首行起始点
    /// </summary>
    /// <param name="paragraphData"></param>
    /// <returns></returns>
    protected abstract Point GetNextParagraphLineStartPoint(ParagraphData paragraphData);

    #region 通用辅助方法

    protected CharInfoMeasureResult MeasureCharInfo(CharInfo charInfo)
    {
        var bounds = new Rect(0, 0, charInfo.RunProperty.FontSize, charInfo.RunProperty.FontSize);
        return new CharInfoMeasureResult(bounds);
    }

    #endregion
}