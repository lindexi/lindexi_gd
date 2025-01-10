using System;

using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Document;

class CharObjectSpanTextRun : IImmutableTextRun
{
    public CharObjectSpanTextRun(TextReadOnlyListSpan<ICharObject> listSpan, IReadOnlyRunProperty? runProperty)
    {
        _listSpan = listSpan;
        RunProperty = runProperty;
    }

    private readonly TextReadOnlyListSpan<ICharObject> _listSpan;

    public int Count => _listSpan.Count;
    public ICharObject GetChar(int index)
    {
        return _listSpan[index];
    }

    public IReadOnlyRunProperty? RunProperty { get; }

    public (IImmutableRun FirstRun, IImmutableRun SecondRun) SplitAt(int index)
    {
        if (index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index == 0)
        {
            throw new ArgumentException($"{nameof(index)} must more than 0");
        }

        var firstSpan = _listSpan.Slice(0, index);
        var secondSpan = _listSpan.Slice(index);
        return (new CharObjectSpanTextRun(firstSpan, RunProperty), new CharObjectSpanTextRun(secondSpan, RunProperty));
    }
}
