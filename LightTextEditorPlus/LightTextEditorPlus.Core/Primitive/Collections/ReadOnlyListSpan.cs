using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LightTextEditorPlus.Core.Primitive.Collections;

public readonly struct ReadOnlyListSpan<T> : IReadOnlyList<T>
{
    public ReadOnlyListSpan(IReadOnlyList<T> source, int start, int length)
    {
        _source = source;
        _start = start;
        _length = length;
    }

    private readonly IReadOnlyList<T> _source;
    private readonly int _start;
    private readonly int _length;

    public IEnumerator<T> GetEnumerator()
    {
        return _source.Skip(_start).Take(_length).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _length;

    public T this[int index] => _source[index + _start];
}