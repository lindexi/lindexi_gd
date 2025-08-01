using System;

namespace LightTextEditorPlus.Core.Document;

class SingleCharImmutableRun : IImmutableRun
{
    public SingleCharImmutableRun(ICharObject charObject, IReadOnlyRunProperty? runProperty)
    {
        CharObject = charObject;
        RunProperty = runProperty;
    }

    public ICharObject CharObject { get; }
    public int Count => 1;
    public ICharObject GetChar(int index) => CharObject;

    public IReadOnlyRunProperty? RunProperty { get; }

    public (IImmutableRun FirstRun, IImmutableRun SecondRun) SplitAt(int index)
    {
        throw new NotSupportedException($"对于 {nameof(SingleCharImmutableRun)} 只包含单个字符，不可再拆");
    }
}