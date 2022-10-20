using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

namespace LightTextEditorPlus.Core.Layout;

// todo 支持输出排版信息，渲染信息。如每一行，每个字符的坐标和尺寸
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

        ArrangingLayoutProvider.UpdateLayout();

        InternalLayoutCompleted?.Invoke(this, EventArgs.Empty);
    }

    private ArrangingLayoutProvider? ArrangingLayoutProvider { set; get; }
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
    /// <param name="paragraph"></param>
    /// <param name="startTextRunIndex"></param>
    /// <param name="startParagraphOffset"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <remarks>
    /// 逻辑上是：
    /// 布局按照： 文本-段落-行-Run-字符
    /// 布局整个文本
    /// 布局文本的每个段落 <see cref="LayoutParagraphCore"/>
    /// 段落里面，需要对每一行进行布局 <see cref="LayoutWholeLine"/>
    /// 每一行里面，需要对每个 Char 字符进行布局 <see cref="LayoutSingleCharInLine"/>
    /// 每个字符需要调用平台的测量 <see cref="MeasureCharInfo"/>
    /// </remarks>
    protected override void LayoutParagraphCore(ParagraphData paragraph, in RunIndexInParagraph startTextRunIndex,
        ParagraphOffset startParagraphOffset)
    {
        //// 当前行的 RunList 列表，看起来设计不对，没有加上在段落的坐标
        //var currentLineRunList = new List<IImmutableRun>();
        // 当前的行渲染信息
        LineVisualData? currentLineVisualData = null;

        // 获取最大宽度信息
        double lineMaxWidth = TextEditor.SizeToContent switch
        {
            SizeToContent.Manual => TextEditor.DocumentManager.DocumentWidth,
            SizeToContent.Width => double.PositiveInfinity,
            SizeToContent.Height => TextEditor.DocumentManager.DocumentWidth,
            SizeToContent.WidthAndHeight => double.PositiveInfinity,
            _ => throw new ArgumentOutOfRangeException()
        };

        var wholeRunLineLayouter = TextEditor.PlatformProvider.GetWholeRunLineLayouter();

        for (var i = startTextRunIndex.ParagraphIndex; i < paragraph.CharCount;)
        {
            // 开始行布局
            // 第一个 Run 就是行的开始
            ReadOnlyListSpan<CharData> charDataList = paragraph.ToReadOnlyListSpan(i);
            WholeRunLineLayoutResult result;
            if (wholeRunLineLayouter != null)
            {
                var argument = new WholeRunLineLayoutArgument(paragraph.ParagraphProperty, charDataList, lineMaxWidth);
                result = wholeRunLineLayouter.LayoutWholeRunLine(argument);
            }
            else
            {
                // 继续往下执行，如果没有注入自定义的行布局层的话
                result = LayoutWholeLine(paragraph, charDataList, lineMaxWidth);
            }

            // 使用字符作为最小单位，也就不需要分割
            //// 先判断是否需要分割
            //if (result.NeedSplitLastRun)
            //{
            //    var lastRunIndex = i + result.RunCount - 1; // todo 这里是否存在 -1 问题
            //    var lastRun = charDataList[result.RunCount - 1];
            //    var (firstRun, secondRun) = lastRun.SplitAt(result.LastRunHitIndex);
            //    paragraph.SplitReplace(lastRunIndex, firstRun, secondRun);
            //}

            currentLineVisualData = new LineVisualData(paragraph)
            {
                StartParagraphIndex = i,
                EndParagraphIndex = i + result.RunCount,
                Size = result.Size,
                //LeftTop = 等待所有行完成了，再赋值
            };
            // 由于修改为使用 Char 为最小单位，不需要再次填充
            //FillRunVisualDataList(currentLineVisualData, result.CharSizeListInRunLine);

            paragraph.LineVisualDataList.Add(currentLineVisualData);

            i += result.RunCount;

            if (result.RunCount == 0)
            {
                // todo 理论上不可能，表示行布局出错了
                TextEditor.Logger.LogWarning($"某一行在布局时，只采用了零个字符");
                return;
            }
        }

        // todo 考虑行复用，例如刚好添加的内容是一行。或者在一行内做文本替换等
        // 这个没有啥优先级。测试了 SublimeText 和 NotePad 工具，都没有做此复用，预计有坑
    }

    ///// <summary>
    ///// 填充 RunVisualDataList 的内容，需要根据字符进行填充
    ///// </summary>
    ///// <param name="currentLineVisualData"></param>
    ///// <param name="charSizeListInRunLine"></param>
    //private void FillRunVisualDataList(LineVisualData currentLineVisualData, IReadOnlyList<Size> charSizeListInRunLine)
    //{
    //    var runVisualDataList = new List<RunVisualData>(currentLineVisualData.CharCount);

    //    var currentCharCount = 0;
    //    foreach (IImmutableRun run in currentLineVisualData.GetSpan())
    //    {
    //        var runSize = Size.Zero;
    //        IList<Size>? charSizeList;
    //        if (run.Count==1)
    //        {
    //            runSize = charSizeListInRunLine[currentCharCount];
    //            // 一个字符就需要创建列表
    //            charSizeList = null;

    //            currentCharCount++;
    //        }
    //        else
    //        {
    //            charSizeList = new List<Size>(run.Count);

    //            for (int i = 0; i < run.Count; i++)
    //            {
    //                charSizeList.Add(charSizeListInRunLine[currentCharCount]);
    //                currentCharCount++;
    //            }
    //        }

    //        var runVisualData = new RunVisualData(run, runSize, charSizeList, currentCharCount);

    //        runVisualDataList.Add(runVisualData);
    //    }

    //    currentLineVisualData.RunVisualDataList = runVisualDataList;
    //}

    private WholeRunLineLayoutResult LayoutWholeLine(ParagraphData paragraph, ReadOnlyListSpan<CharData> charDataList,
        double lineMaxWidth)
    {
        var singleRunLineLayouter = TextEditor.PlatformProvider.GetSingleRunLineLayouter();

        // RunLineMeasurer
        // 行还剩余的空闲宽度
        double lineRemainingWidth = lineMaxWidth;

        // 现在使用字符布局了，不再需要对 Run 进行分割
        //int lastRunHitIndex = 0;
        int currentRunIndex = 0;
        var currentSize = Size.Zero;

        while (currentRunIndex < charDataList.Count)
        {
            //var charData = charDataList[currentRunIndex];
            //var charRenderData = charData.CharRenderData;

            //if (charRenderData == null)
            //{
            //    var charInfo = new CharInfo(charData.CharObject, charData.RunProperty);
            //    CharInfoMeasureResult charInfoMeasureResult;
            //    if (charInfoMeasurer != null)
            //    {
            //        charInfoMeasureResult = charInfoMeasurer.MeasureCharInfo(charInfo);
            //    }
            //    else
            //    {
            //        charInfoMeasureResult = MeasureCharInfo(charInfo);
            //    }

            //    charRenderData = new CharRenderData(charData, paragraph, charInfoMeasureResult.Bounds.Size);

            //    charData.CharRenderData = charRenderData;
            //}

            //if (lineRemainingWidth > charRenderData.Size.Width)
            //{
            //    currentRunIndex++;
            //    var width = currentSize.Width + charRenderData.Size.Width;
            //    var height = Math.Max(currentSize.Height, charRenderData.Size.Height);
            //    currentSize = new Size(width, height);

            //    lineRemainingWidth -= charRenderData.Size.Width;
            //}
            //else
            //{
            //    // 换行，这一行布局完成
            //    break;
            //}

            // 一行里面需要逐个字符进行布局
            var arguments = new SingleCharInLineLayoutArguments(charDataList, currentRunIndex, lineRemainingWidth,
                paragraph.ParagraphProperty);

            SingleCharInLineLayoutResult result;
            if (singleRunLineLayouter is not null)
            {
                result = singleRunLineLayouter.LayoutSingleRunInLine(arguments);
            }
            else
            {
                result = LayoutSingleCharInLine(arguments);
            }

            if (result.CanTake)
            {
                var width = currentSize.Width + result.TotalSize.Width;
                var height = Math.Max(currentSize.Height, result.TotalSize.Height);
                currentSize = new Size(width, height);

                for (int i = currentRunIndex; i < currentRunIndex + result.TaskCount; i++)
                {
                    var charData = charDataList[i];
                    charData.CharRenderData ??=
                        new CharRenderData(charData, paragraph, result.CharSizeList[i - currentRunIndex]);
                }

                currentRunIndex += result.TaskCount;
                //currentCharSizeInRunLine.AddRange(result.CharSizeList);
            }

            //lastRunHitIndex = result.SplitLastRunIndex;

            if (result.ShouldBreakLine)
            {
                // 换行，这一行布局完成
                break;
            }
        }

        return new WholeRunLineLayoutResult(currentSize, currentRunIndex);
    }

    private SingleCharInLineLayoutResult LayoutSingleCharInLine(SingleCharInLineLayoutArguments arguments)
    {
        var charInfoMeasurer = TextEditor.PlatformProvider.GetCharInfoMeasurer();

        var charData = arguments.RunList[arguments.CurrentIndex];

        var charRenderData = charData.CharRenderData;

        Size size;
        if (charRenderData == null)
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
            size = charRenderData.Size;
        }

        if (arguments.LineRemainingWidth > size.Width)
        {
            return new SingleCharInLineLayoutResult(1, size, new Size[]{size});
        }
        else
        {
            return new SingleCharInLineLayoutResult(0, default, Array.Empty<Size>());
        }

        //var singleCharInLineLayouter = TextEditor.PlatformProvider.GetSingleCharInLineLayouter();

        //var currentRun = arguments.RunList[arguments.CurrentIndex];
        //var currentSize = Size.Zero;

        //var currentCharSizeInRun = new List<Size>();

        //for (int i = 0; i < currentRun.Count;)
        //{
        //    var runInfo = new RunInfo(arguments.RunList, arguments.CurrentIndex, i,
        //        TextEditor.DocumentManager.CurrentRunProperty);

        //    var measureCharInLineArguments =
        //        new SingleCharInLineLayoutArguments(runInfo, arguments.LineRemainingWidth, arguments.ParagraphProperty);

        //    SingleCharInLineLayoutResult result;

        //    if (singleCharInLineLayouter is not null)
        //    {
        //        result = singleCharInLineLayouter.LayoutSingleCharInLine(measureCharInLineArguments);
        //    }
        //    else
        //    {
        //        result = LayoutSingleCharInLine(measureCharInLineArguments);
        //    }

        //    if (result.CanTake)
        //    {
        //        i += result.TakeCharCount;
        //        var width = currentSize.Width + result.TotalSize.Width;
        //        var height = Math.Max(currentSize.Height, result.TotalSize.Height);

        //        if (result.TakeCharCount == 1)
        //        {
        //            // 各个字符的尺寸。如果采用的字符数量是 1 个时，因为字符的尺寸等于 TotalSize 尺寸
        //            currentCharSizeInRun.Add(result.TotalSize);
        //        }
        //        else
        //        {
        //            var charSizeList = result.CharSizeList;// 内部判断这个流程一定不为空
        //            currentCharSizeInRun.AddRange(charSizeList);
        //        }

        //        currentSize = new Size(width, height);
        //    }
        //    else
        //    {
        //        if (i == 0)
        //        {
        //            // 一个都获取不到
        //            return new SingleCharInLineLayoutResult(0, 0, currentSize, Array.Empty<Size>());
        //        }
        //        else
        //        {
        //            // 无法将整个 Run 都排版进去，只能排版部分
        //            var hitIndex = i - 1;
        //            return new SingleCharInLineLayoutResult(1, hitIndex, currentSize, currentCharSizeInRun);
        //        }
        //    }
        //}

        //// 整个 Run 都排版进去，不需要将这个 Run 拆分
        //return new SingleCharInLineLayoutResult(1, 0, currentSize, currentCharSizeInRun);
    }

    //private SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArguments layoutArguments)
    //{
    //    var charInfoMeasurer = TextEditor.PlatformProvider.GetCharInfoMeasurer();

    //    var runInfo = layoutArguments.RunInfo;
    //    var charInfo = runInfo.GetCurrentCharInfo();

    //    CharInfoMeasureResult result;
    //    if (charInfoMeasurer != null)
    //    {
    //        result = charInfoMeasurer.MeasureCharInfo(charInfo);
    //    }
    //    else
    //    {
    //        result = MeasureCharInfo(charInfo);
    //    }

    //    if (result.Bounds.Width > layoutArguments.LineRemainingWidth)
    //    {
    //        return new SingleCharInLineLayoutResult(0, default);
    //    }
    //    else
    //    {
    //        return new SingleCharInLineLayoutResult(1, result.Bounds.Size);
    //    }
    //}

    private CharInfoMeasureResult MeasureCharInfo(CharInfo charInfo)
    {
        var bounds = new Rect(0, 0, charInfo.RunProperty.FontSize, charInfo.RunProperty.FontSize);
        return new CharInfoMeasureResult(bounds);
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

    public void UpdateLayout()
    {
        // todo 项目符号的段落，如果在段落上方新建段落，那需要项目符号更新
        // 这个逻辑准备给项目符号逻辑更新，逻辑是，假如现在有两段，分别采用 `1. 2.` 作为项目符号
        // 在 `1.` 后面新建一个段落，需要自动将原本的 `2.` 修改为 `3.` 的内容，这个逻辑准备交给项目符号模块自己编辑实现

        // 布局逻辑：
        // - 获取需要更新布局段落的逻辑
        // - 进入段落布局
        //   - 进入行布局
        // - 所有段落布局完成之后，再根据段落之间的高度信息，更新每个段落的 YOffset 的值

        // 首行出现变脏的序号
        var firstDirtyParagraphIndex = -1;
        var dirtyParagraphDataList = new List<ParagraphData>();
        var list = TextEditor.DocumentManager.TextRunManager.ParagraphManager.GetParagraphList();
        for (var index = 0; index < list.Count; index++)
        {
            ParagraphData paragraphData = list[index];
            if (paragraphData.IsDirty)
            {
                if (firstDirtyParagraphIndex == -1)
                {
                    firstDirtyParagraphIndex = index;
                }

                dirtyParagraphDataList.Add(paragraphData);
            }
        }

        // 进入段落内布局
        foreach (var paragraphData in dirtyParagraphDataList)
        {
            LayoutParagraph(paragraphData);
        }

        // todo 完成测量最大宽度

        // 完成布局之后，全部设置为非脏的（或者是段落内自己实现）
        foreach (var paragraphData in dirtyParagraphDataList)
        {
            paragraphData.IsDirty = false;
        }

        Debug.Assert(TextEditor.DocumentManager.TextRunManager.ParagraphManager.GetParagraphList()
            .All(t => t.IsDirty == false));
    }

    /// <summary>
    /// 段落内布局
    /// </summary>
    /// <param name="paragraph"></param>
    private void LayoutParagraph(ParagraphData paragraph)
    {
        // 先找到首个需要更新的坐标点，这里的坐标是段坐标
        var dirtyParagraphOffset = 0;
        var lastIndex = -1;
        for (var index = 0; index < paragraph.LineVisualDataList.Count; index++)
        {
            LineVisualData lineVisualData = paragraph.LineVisualDataList[index];
            if (lineVisualData.IsDirty == false)
            {
                dirtyParagraphOffset += lineVisualData.CharCount;
            }
            else
            {
                lastIndex = index;
                break;
            }
        }

        if (lastIndex > 0)
        {
            // todo 考虑行信息的复用，或者调用释放方法
            paragraph.LineVisualDataList.RemoveRange(lastIndex, paragraph.LineVisualDataList.Count - lastIndex);
        }

        // 不需要通过如此复杂的逻辑获取有哪些，因为存在的坑在于后续分拆 IImmutableRun 逻辑将会复杂
        //paragraph.GetRunRange(dirtyParagraphOffset);

        if (paragraph.GetRunList().Count == 0)
        {
            // todo 考虑 paragraph.TextRunList 数量为空的情况，只有一个换行的情况
        }

        var startParagraphOffset = new ParagraphOffset(dirtyParagraphOffset);
        var startTextRunIndex = paragraph.GetRunIndex(startParagraphOffset);

        if (startTextRunIndex.ParagraphIndex == -1)
        {
            // todo 理论上不可能
        }

        LayoutParagraphCore(paragraph, startTextRunIndex, startParagraphOffset);
    }

    protected abstract void LayoutParagraphCore(ParagraphData paragraph, in RunIndexInParagraph startTextRunIndex,
        ParagraphOffset startParagraphOffset);
}