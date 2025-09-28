using System.Collections.Generic;
using System.Linq;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 这是继承了不可变列表的可变列表，只能程序集内部使用，不安全
/// </summary>
/// 用于解决 API 定义和性能，防止不可变导致更多内存分配
class MutableRunList : List<IImmutableRun>, IImmutableRunList
{
    public MutableRunList()
    {
    }

    public MutableRunList(IEnumerable<IImmutableRun> collection) : base(collection)
    {
    }

    public MutableRunList(int capacity) : base(capacity)
    {
    }

    public int CharCount => this.Sum(t => t.Count);
    public int RunCount => Count;
    public IImmutableRun GetRun(int index) => this[index];
}