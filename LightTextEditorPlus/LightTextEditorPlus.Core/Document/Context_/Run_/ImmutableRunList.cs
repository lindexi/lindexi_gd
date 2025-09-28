using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个 <see cref="IImmutableRun"/> 列表
/// </summary>
internal class ImmutableRunList : IImmutableRunList
{
    /// <summary>
    /// 创建不可变的文本列表
    /// </summary>
    /// <param name="collection"></param>
    public ImmutableRunList(IEnumerable<IImmutableRun> collection)
    {
        _runs = collection.ToImmutableArray();
    }

    /// <inheritdoc />
    public int CharCount => _runs.Sum(t => t.Count);

    /// <inheritdoc />
    public int RunCount => _runs.Length;

    /// <inheritdoc />
    public IImmutableRun GetRun(int index) => _runs[index];

    private readonly ImmutableArray<IImmutableRun> _runs;
}
