using System.Collections.Generic;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个支持合入的文本段
/// </summary>
/// todo 实现文本段合并功能
public interface IMergeableRun
{
    /// <summary>
    /// 给定一个文本列表，将可合并到一起的文本合成一个，用于提升性能。例如有两个相邻的相同的 <see cref="LayoutOnlyRunProperty"/> 的 <see cref="TextRun"/> 那么可以合并为一个 <see cref="TextRun"/> 对象
    /// </summary>
    /// <param name="runList"></param>
    /// <returns></returns>
    List<IImmutableRun?> MergeRunList(IList<IImmutableRun> runList);
}