using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 段内行测量布局结果
/// </summary>
/// <param name="Size">这一行的尺寸</param>
/// <param name="RunCount">这一行使用的 <see cref="IImmutableRun"/> 的数量</param>
/// <param name="LastRunHitIndex">最后一个 <see cref="IImmutableRun"/> 被使用的字符数量，如刚好用完一个 <see cref="IImmutableRun"/> 那么设置默认为 0 的值。设置为非 0 的值，将会分割最后一个 <see cref="IImmutableRun"/> 为多个，保证没有一个 <see cref="IImmutableRun"/> 是跨行的</param>
/// <param name="CharSizeListInRunLine">这一行里面每个字符的尺寸</param>
public readonly record struct WholeRunLineLayoutResult(Size Size, int RunCount, int LastRunHitIndex, IReadOnlyList<Size> CharSizeListInRunLine)
{
    /// <summary>
    /// 是否最后一个 Run 需要被分割。也就是最后一个 Run 将会跨多行
    /// </summary>
    public bool NeedSplitLastRun => LastRunHitIndex > 0;
}