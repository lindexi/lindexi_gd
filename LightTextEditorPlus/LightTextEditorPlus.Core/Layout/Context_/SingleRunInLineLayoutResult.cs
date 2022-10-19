using System.Collections.Generic;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 测量行内字符的结果
/// </summary>
/// <param name="TotalSize">这一行的布局尺寸</param>
/// <param name="TaskCount">使用了多少个 IImmutableRun 元素</param>
/// <param name="SplitLastRunIndex">最后一个 IImmutableRun 元素是否需要拆分跨行，需要拆分也就意味着需要分行了</param>
public readonly record struct SingleRunInLineLayoutResult(int TaskCount, int SplitLastRunIndex, Size TotalSize,IReadOnlyList<Size> CharSizeList)
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