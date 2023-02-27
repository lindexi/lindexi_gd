using System.Collections.Generic;

namespace LightTextEditorPlus.Core.Document;

class ImmutableRunList : List<IImmutableRun>, IImmutableRunList
{
    public ImmutableRunList()
    {
    }

    public ImmutableRunList(IEnumerable<IImmutableRun> collection) : base(collection)
    {
    }

    public ImmutableRunList(int capacity) : base(capacity)
    {
    }

    public int RunCount => Count;
    public IImmutableRun GetRun(int index) => this[index];
}