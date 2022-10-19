using System;

namespace LightTextEditorPlus.Core.Document;

public class TextRun : IImmutableTextRun
{
    public TextRun(string text, IReadOnlyRunProperty? runProperty=null)
    {
        Text = text;
        RunProperty = runProperty;
    }

    public string Text { get; }

    /// <inheritdoc />
    public int Count => Text.Length;

    /// <inheritdoc />
    public ICharObject GetChar(int index)
    {
        return new TextCharObject(Text[index].ToString());
    }

    /// <inheritdoc />
    public IReadOnlyRunProperty? RunProperty { get; }

    /// <inheritdoc />
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

        var firstStart = 0;
        var firstLength = index;

        var secondStart = 0 + index;
        var secondLength = Count - index;

        return (new SpanTextRun(Text, firstStart, firstLength, RunProperty),
            new SpanTextRun(Text, secondStart, secondLength));
    }

    public override string ToString() => Text;
}