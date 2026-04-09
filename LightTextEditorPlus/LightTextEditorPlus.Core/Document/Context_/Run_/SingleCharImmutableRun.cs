using System;
using LightTextEditorPlus.Core.Resources;

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
        throw new NotSupportedException(ExceptionMessages.Format(
            nameof(SingleCharImmutableRun) + "_SplitAt_NotSupported", nameof(SingleCharImmutableRun)));
    }
}