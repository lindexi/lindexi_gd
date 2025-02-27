using System;

namespace LightTextEditorPlus.Core.Document;

class SpanTextRun : IImmutableTextRun
{
    public SpanTextRun(string originText, int start, int length, IReadOnlyRunProperty? runProperty = null)
    {
        OriginText = originText;
        RunProperty = runProperty;
        Start = start;
        Count = length;
    }

    public int Start { get; }
    private string OriginText { get; }

    public int Count { get; }

    public ICharObject GetChar(int index)
    {
        return TextCharObjectCreator.CreateCharObject(OriginText, index + Start);
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

        var firstStart = Start;
        var firstLength = index;

        var secondStart = Start + index;
        var secondLength = Count - index;

        return (new SpanTextRun(OriginText, firstStart, firstLength, RunProperty),
            new SpanTextRun(OriginText, secondStart, secondLength));
    }

    public override string ToString() => OriginText.Substring(Start, Count);
}
