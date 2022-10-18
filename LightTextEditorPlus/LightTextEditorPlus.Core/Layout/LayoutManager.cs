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

    protected override void LayoutParagraphCore(ParagraphData paragraph, in RunIndexInParagraph startTextRunIndex, ParagraphOffset startParagraphOffset)
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
            var result = MeasureAndArrangeRunLine(paragraph,runSpan, lineMaxWidth);

            // 先判断是否需要分割
            if (result.NeedSplitLastRun)
            {
                var lastRunIndex = i + result.RunCount-1; // todo 这里是否存在 -1 问题
                IImmutableRun lastRun = runSpan[result.RunCount-1];
                var (firstRun, secondRun) = lastRun.SplitAt(result.LastRunHitIndex);
                paragraph.SplitReplace(lastRunIndex,firstRun,secondRun);
            }

            currentLineVisualData = new LineVisualData(paragraph)
            {
                IsDirty = false,
                StartParagraphIndex = i,
                EndParagraphIndex = i+ result.RunCount,
                Size=result.Size,
            };

            paragraph.LineVisualDataList.Add(currentLineVisualData);

            i+= result.RunCount;

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

    /// <summary>
    /// 测量和布局行
    /// </summary>
    /// <param name="paragraph"></param>
    /// <param name="runList"></param>
    /// <param name="lineMaxWidth"></param>
    private RunLineMeasureAndArrangeResult MeasureAndArrangeRunLine(ParagraphData paragraph,
        IReadOnlyList<IImmutableRun> runList, double lineMaxWidth)
    {
        // todo 允许注入可定制的自定义布局方法
        //TextEditor.PlatformProvider.GetCustomMeasureAndArrangeRunLine
        var runMeasureProvider = TextEditor.PlatformProvider.GetRunMeasureProvider();

        // 行还剩余的空闲宽度
        double lineRemainingWidth = lineMaxWidth;

        int lastRunHitIndex = 0;
        int currentRunIndex = 0;

        //while (currentRunIndex < runList.Count)
        //{
            
        //}

        var arguments = new MeasureRunInLineArguments(runList, currentRunIndex, lineRemainingWidth,paragraph.ParagraphProperty);

        //for (; i < runList.Count; i++)
        //{
        //    var run = runList[i];
            
        //    //runMeasureProvider.MeasureRun()
        //}

        //var runCount = i;

        // todo 以下是测试数据
        //return new RunLineMeasureAndArrangeResult(new Size(100,100), runCount, 0);
        throw new NotImplementedException();
    }
}

// 这里无法确定采用字符加上属性的方式是否会更优
// 通过字符获取对应的属性，如此即可不需要每次都需要考虑将
// 一个 IImmutableRun 进行分割而已

// 尝试根据字符给出属性的方式，如此可以不用考虑将一个 IImmutableRun 进行分割

public class RunVisitor
{
    public RunVisitor(IReadOnlyRunProperty defaultRunProperty, IReadOnlyList<IImmutableRun> runList)
    {
        DefaultRunProperty = defaultRunProperty;
        RunList = runList;
    }

    private IReadOnlyRunProperty DefaultRunProperty { get; }
    private IReadOnlyList<IImmutableRun> RunList { get; }

    public int CurrentCharIndex { get; private set; }

    /// <summary>
    /// 当前的 <see cref="RunIndex"/> 对应的字符起始点
    /// </summary>
    private int CharStartIndexOfCurrentRun { get; set; }

    /// <summary>
    /// 当前的 <see cref="RunList"/> 的序号
    /// </summary>
    private int RunIndex { get; set; }

    public (ICharObject charObject, IReadOnlyRunProperty RunProperty) GetCurrentCharInfo()
    {
        var run = RunList[RunIndex];
        var charObject = run.GetChar(CurrentCharIndex- CharStartIndexOfCurrentRun);
        return (charObject, run.RunProperty ?? DefaultRunProperty);
    }

    public (ICharObject charObject, IReadOnlyRunProperty RunProperty) GetCharInfo(int charIndex)
    {
        var (charObject, runProperty) = RunList.GetCharInfo(charIndex);
        return (charObject, runProperty ?? DefaultRunProperty);
    }
}

public readonly record struct MeasureRunInLineArguments(IReadOnlyList<IImmutableRun> RunList,int CurrentIndex, double LineRemainingWidth, ParagraphProperty ParagraphProperty)
{
    
}

/// <summary>
/// 测量行内字符的结果
/// </summary>
/// <param name="Size">这一行的布局尺寸</param>
/// <param name="TaskCount">使用了多少个 IImmutableRun 元素</param>
/// <param name="SplitLastRunIndex">最后一个 IImmutableRun 元素是否需要拆分跨行，需要拆分也就意味着需要分行了</param>
public readonly record struct MeasureRunInLineResult(int TaskCount,int SplitLastRunIndex, Size Size)
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

/// <summary>
/// 段内行测量布局结果
/// </summary>
/// <param name="Size">这一行的尺寸</param>
/// <param name="RunCount">这一行使用的 <see cref="IImmutableRun"/> 的数量</param>
/// <param name="LastRunHitIndex">最后一个 <see cref="IImmutableRun"/> 被使用的字符数量，如刚好用完一个 <see cref="IImmutableRun"/> 那么设置默认为 0 的值。设置为非 0 的值，将会分割最后一个 <see cref="IImmutableRun"/> 为多个，保证没有一个 <see cref="IImmutableRun"/> 是跨行的</param>
public readonly record struct RunLineMeasureAndArrangeResult(Size Size,int RunCount, int LastRunHitIndex)
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

        Debug.Assert(TextEditor.DocumentManager.TextRunManager.ParagraphManager.GetParagraphList().All(t => t.IsDirty == false));
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