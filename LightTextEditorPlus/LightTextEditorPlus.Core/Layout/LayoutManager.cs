using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;
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
    /// 段落里面，需要对每一行进行布局 <see cref="MeasureWholeRunLine"/>
    /// 每一行里面，需要对每个 Run 进行布局 <see cref="MeasureSingleRunLine"/>
    /// 每个 Run 需要对每个 Char 字符进行布局 <see cref="MeasureCharInLine"/>
    /// 每个字符需要调用平台的测量 <see cref="MeasureCharInfo"/>
    /// </remarks>
    protected override void LayoutParagraphCore(ParagraphData paragraph, in RunIndexInParagraph startTextRunIndex,
        ParagraphOffset startParagraphOffset)
    {
        var runList = paragraph.GetRunList();

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

        var wholeRunLineMeasurer = TextEditor.PlatformProvider.GetWholeRunLineMeasurer();

        for (var i = startTextRunIndex.ParagraphIndex; i < runList.Count;)
        {
            // 预期刚好 dirtyParagraphOffset 是某个 IImmutableRun 的起始
            //if (currentLineVisualData is null)
            //{
            //    currentLineVisualData = new LineVisualData(paragraph)
            //    {
            //        IsDirty = false,
            //        StartParagraphIndex = i,
            //    };
            //}

            // 开始行布局
            // 第一个 Run 就是行的开始
            var runSpan = paragraph.ToReadOnlyListSpan(i);
            RunLineMeasureAndArrangeResult result;
            if (wholeRunLineMeasurer != null)
            {
                var argument = new ParagraphRunLineMeasureAndArrangeArgument(paragraph.ParagraphProperty, runSpan, lineMaxWidth);
                result = wholeRunLineMeasurer.MeasureWholeRunLine(argument);
            }
            else
            {
                // 继续往下执行，如果没有注入自定义的行布局层的话
                result = MeasureWholeRunLine(paragraph, runSpan, lineMaxWidth);
            }

            // 先判断是否需要分割
            if (result.NeedSplitLastRun)
            {
                var lastRunIndex = i + result.RunCount - 1; // todo 这里是否存在 -1 问题
                IImmutableRun lastRun = runSpan[result.RunCount - 1];
                var (firstRun, secondRun) = lastRun.SplitAt(result.LastRunHitIndex);
                paragraph.SplitReplace(lastRunIndex, firstRun, secondRun);
            }

            currentLineVisualData = new LineVisualData(paragraph)
            {
                IsDirty = false,
                StartParagraphIndex = i,
                EndParagraphIndex = i + result.RunCount,
                Size = result.Size,
            };

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

    private RunLineMeasureAndArrangeResult MeasureWholeRunLine(ParagraphData paragraph, ReadOnlyListSpan<IImmutableRun> runList,
        double lineMaxWidth)
    {
        var runLineMeasurer = TextEditor.PlatformProvider.GetSingleRunLineMeasurer();

        // RunLineMeasurer
        // 行还剩余的空闲宽度
        double lineRemainingWidth = lineMaxWidth;

        int lastRunHitIndex = 0;
        int currentRunIndex = 0;
        var currentSize = Size.Zero;

        while (currentRunIndex < runList.Count)
        {
            var arguments = new MeasureSingleRunInLineArguments(runList, currentRunIndex, lineRemainingWidth,
                paragraph.ParagraphProperty);

            MeasureSingleRunInLineResult result;
            if (runLineMeasurer is not null)
            {
                result = runLineMeasurer.MeasureSingleRunLine(arguments);
            }
            else
            {
                result = MeasureSingleRunLine(arguments);
            }

            if (result.CanTake)
            {
                currentRunIndex += result.TaskCount;

                var width = currentSize.Width + result.Size.Width;
                var height = Math.Max(currentSize.Height, result.Size.Height);
                currentSize = new Size(width, height);
            }

            lastRunHitIndex = result.SplitLastRunIndex;

            if (result.ShouldBreakLine)
            {
                // 换行
                //return new RunLineMeasureAndArrangeResult(currentSize, currentRunIndex, lastRunHitIndex);
                break;
            }
            //MeasureSingleRunInLineResult result = runMeasureProvider.MeasureAndArrangeRunLine(arguments);
        }

        return new RunLineMeasureAndArrangeResult(currentSize, currentRunIndex, lastRunHitIndex);
    }

    private MeasureSingleRunInLineResult MeasureSingleRunLine(MeasureSingleRunInLineArguments arguments)
    {
        var charLineMeasurer = TextEditor.PlatformProvider.GetCharLineMeasurer();

        var currentRun = arguments.RunList[arguments.CurrentIndex];
        var currentSize = Size.Zero;

        for (int i = 0; i < currentRun.Count;)
        {
            var runInfo = new RunInfo(arguments.RunList, arguments.CurrentIndex, i,
                TextEditor.DocumentManager.CurrentRunProperty);

            var measureCharInLineArguments =
                new MeasureCharInLineArguments(runInfo, arguments.LineRemainingWidth, arguments.ParagraphProperty);

            MeasureCharInLineResult result;

            if (charLineMeasurer is not null)
            {
                result = charLineMeasurer.MeasureCharInLine(measureCharInLineArguments);
            }
            else
            {
                result = MeasureCharInLine(measureCharInLineArguments);
            }

            if (result.CanTake)
            {
                i += result.TakeCharCount;
                var width = currentSize.Width + result.Size.Width;
                var height = Math.Max(currentSize.Height, result.Size.Height);

                currentSize = new Size(width, height);
            }
            else
            {
                if (i == 0)
                {
                    // 一个都获取不到
                    return new MeasureSingleRunInLineResult(0, 0, currentSize);
                }
                else
                {
                    // 无法将整个 Run 都排版进去，只能排版部分
                    var hitIndex = i - 1;
                    return new MeasureSingleRunInLineResult(1, hitIndex, currentSize);
                }
            }
        }

        // 整个 Run 都排版进去，不需要将这个 Run 拆分
        return new MeasureSingleRunInLineResult(1, 0, currentSize);
    }

    private MeasureCharInLineResult MeasureCharInLine(in MeasureCharInLineArguments arguments)
    {
        var charInfoMeasurer = TextEditor.PlatformProvider.GetCharInfoMeasurer();

        var runInfo = arguments.RunInfo;
        var charInfo = runInfo.GetCurrentCharInfo();

        CharInfoMeasureResult result;
        if (charInfoMeasurer != null)
        {
            result = charInfoMeasurer.MeasureCharInfo(charInfo);
        }
        else
        {
            result = MeasureCharInfo(charInfo);
        }

        if (result.Bounds.Width > arguments.LineRemainingWidth)
        {
            return new MeasureCharInLineResult(0, default);
        }
        else
        {
            return new MeasureCharInLineResult(1, result.Bounds.Size);
        }
    }

    private CharInfoMeasureResult MeasureCharInfo(CharInfo charInfo)
    {
        var bounds = new Rect(0, 0, charInfo.RunProperty.FontSize, charInfo.RunProperty.FontSize);
        return new CharInfoMeasureResult(bounds);
    }

    //private void TakeChar(RunInfo runInfo, double lineMaxWidth, ParagraphProperty paragraphProperty)
    //{
    //    runInfo.GetCurrentCharInfo();
    //    runInfo.GetNextCharInfo(index);

    //}
}

public readonly record struct CharInfoMeasureResult(Rect Bounds)
{
}

public readonly record struct CharInfo(ICharObject CharObject, IReadOnlyRunProperty RunProperty)
{
}

public readonly record struct RunInfo(ReadOnlyListSpan<IImmutableRun> RunList, int CurrentIndex,
    int CurrentCharHitIndex, IReadOnlyRunProperty DefaultRunProperty)
{
    public CharInfo GetCurrentCharInfo()
    {
        var run = RunList[CurrentIndex];
        var charObject = run.GetChar(CurrentCharHitIndex);
        return new CharInfo(charObject, run.RunProperty ?? DefaultRunProperty);
    }

    public CharInfo GetNextCharInfo(int index = 1)
    {
        if (index > 0)
        {
            var charIndex = CurrentCharHitIndex + index;

            var currentRun = RunList[CurrentIndex];
            if (charIndex < currentRun.Count)
            {
                // 这是一个优化，判断是否在当前的文本段内

                return new CharInfo(currentRun.GetChar(charIndex), currentRun.RunProperty ?? DefaultRunProperty);
            }


            // 从当前开始进行拆分，拆分之后即可相对于当前的索引开始计算
            var runSpan = RunList.Slice(CurrentIndex);

            var (charObject, runProperty) = runSpan.GetCharInfo(charIndex);

            return new CharInfo(charObject, runProperty ?? DefaultRunProperty);
        }
        else if (index == 0)
        {
            throw new ArgumentException($"请使用 {nameof(GetCurrentCharInfo)} 方法代替", nameof(index));
        }
        else
        {
            // 计算出 index 的相对于 RunList 的字符序号是多少
            // 然后对整个进行计算获取到对应的字符
            var charIndex = 0;
            for (var i = 0; i < CurrentIndex; i++)
            {
                var run = RunList[i];
                charIndex += run.Count;
            }

            charIndex += CurrentCharHitIndex;
            charIndex += index;

            var (charObject, runProperty) = RunList.GetCharInfo(charIndex);
            return new CharInfo(charObject, runProperty ?? DefaultRunProperty);
        }
    }
}

public readonly record struct MeasureCharInLineArguments(RunInfo RunInfo, double LineRemainingWidth,
    ParagraphProperty ParagraphProperty)
{
}

public readonly record struct MeasureCharInLineResult(int TakeCharCount, Size Size)
{
    public bool CanTake => TakeCharCount > 0;
}

public readonly record struct MeasureSingleRunInLineArguments(ReadOnlyListSpan<IImmutableRun> RunList, int CurrentIndex,
    double LineRemainingWidth, ParagraphProperty ParagraphProperty)
{
}

/// <summary>
/// 测量行内字符的结果
/// </summary>
/// <param name="Size">这一行的布局尺寸</param>
/// <param name="TaskCount">使用了多少个 IImmutableRun 元素</param>
/// <param name="SplitLastRunIndex">最后一个 IImmutableRun 元素是否需要拆分跨行，需要拆分也就意味着需要分行了</param>
public readonly record struct MeasureSingleRunInLineResult(int TaskCount, int SplitLastRunIndex, Size Size)
{
    // 测量一个 Run 在行内布局的结果

    /// <summary>
    /// 是否最后一个 Run 需要被分割。也就是最后一个 Run 将会跨多行
    /// </summary>
    public bool NeedSplitLastRun => SplitLastRunIndex > 0;

    /// <summary>
    /// 是否这一行可以加入字符。不可加入等于需要换行
    /// </summary>
    public bool CanTake => TaskCount > 0;

    /// <summary>
    /// 是否需要换行了。等同于这一行不可再加入字符
    /// </summary>
    public bool ShouldBreakLine => CanTake is false || NeedSplitLastRun;
}

public readonly record struct ParagraphRunLineMeasureAndArrangeArgument(ParagraphProperty ParagraphProperty, in ReadOnlyListSpan<IImmutableRun> RunList, double LineMaxWidth)
{
    
}

/// <summary>
/// 段内行测量布局结果
/// </summary>
/// <param name="Size">这一行的尺寸</param>
/// <param name="RunCount">这一行使用的 <see cref="IImmutableRun"/> 的数量</param>
/// <param name="LastRunHitIndex">最后一个 <see cref="IImmutableRun"/> 被使用的字符数量，如刚好用完一个 <see cref="IImmutableRun"/> 那么设置默认为 0 的值。设置为非 0 的值，将会分割最后一个 <see cref="IImmutableRun"/> 为多个，保证没有一个 <see cref="IImmutableRun"/> 是跨行的</param>
public readonly record struct RunLineMeasureAndArrangeResult(Size Size, int RunCount, int LastRunHitIndex)
{
    /// <summary>
    /// 是否最后一个 Run 需要被分割。也就是最后一个 Run 将会跨多行
    /// </summary>
    public bool NeedSplitLastRun => LastRunHitIndex > 0;
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