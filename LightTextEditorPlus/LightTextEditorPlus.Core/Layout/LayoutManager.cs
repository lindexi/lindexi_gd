using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;

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
        //var currentLineRunList = new List<IRun>();
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
            // 预期刚好 dirtyParagraphOffset 是某个 IRun 的起始
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
            var runSpan = paragraph.AsSpan().Slice(i);
            var result = MeasureAndArrangeRunLine(runSpan, lineMaxWidth);

            // 先判断是否需要分割
            if (result.NeedSplitLastRun)
            {
                var lastRunIndex = i + result.RunCount-1; // todo 这里是否存在 -1 问题
                IRun lastRun = runSpan[result.RunCount-1];
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
        }

        // todo 考虑行复用，例如刚好添加的内容是一行。或者在一行内做文本替换等
        // 这个没有啥优先级。测试了 SublimeText 和 NotePad 工具，都没有做此复用，预计有坑
    }

    /// <summary>
    /// 测量和布局行
    /// </summary>
    /// <param name="runSpan"></param>
    /// <param name="lineMaxWidth"></param>
    private RunLineMeasureAndArrangeResult MeasureAndArrangeRunLine(Span<IRun> runSpan, double lineMaxWidth)
    {
        // todo 允许注入可定制的自定义布局方法
        //TextEditor.PlatformProvider.GetCustomMeasureAndArrangeRunLine

        // 行还剩余的空闲宽度
        double lineRemainingWidth = lineMaxWidth;

        // todo 以下是测试数据
        return new RunLineMeasureAndArrangeResult(new Size(100,100),1,0);
    }
}

/// <summary>
/// 段内行测量布局结果
/// </summary>
/// <param name="Size">这一行的尺寸</param>
/// <param name="RunCount">这一行使用的 <see cref="IRun"/> 的数量</param>
/// <param name="LastRunHitIndex">最后一个 <see cref="IRun"/> 被使用的字符数量，如刚好用完一个 <see cref="IRun"/> 那么设置默认为 0 的值。设置为非 0 的值，将会分割最后一个 <see cref="IRun"/> 为多个，保证没有一个 <see cref="IRun"/> 是跨行的</param>
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

        // 不需要通过如此复杂的逻辑获取有哪些，因为存在的坑在于后续分拆 IRun 逻辑将会复杂
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