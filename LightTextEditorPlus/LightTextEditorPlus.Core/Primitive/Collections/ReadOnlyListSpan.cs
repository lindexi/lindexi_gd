using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LightTextEditorPlus.Core.Primitive.Collections;

/// <summary>
/// 表示一个 List 里面一段只读集合
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>无法处理传入的 source 实际源被更改问题。在 source 不变更情况下，读取是线程安全。 使用结构体能够减少 GC 压力</remarks>
/// todo 改名，不能叫这么通用的名字，不利于 public 到项目外使用
public readonly struct ReadOnlyListSpan<T> : IReadOnlyList<T>, IEquatable<ReadOnlyListSpan<T>>
{
    /// <inheritdoc cref="ReadOnlyListSpan{T}"/>
    public ReadOnlyListSpan(IReadOnlyList<T> source, int start, int length)
    {
        _source = source;
        _start = start;
        _length = length;
    }

    private readonly IReadOnlyList<T> _source;
    private readonly int _start;
    private readonly int _length;

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        return _source.Skip(_start).Take(_length).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public int Count => _length;

    /// <inheritdoc />
    public T this[int index] => _source[index + _start];

    /// <summary>
    /// 分出给定范围的新列表
    /// </summary>
    /// <param name="start"></param>
    /// <returns></returns>
    public ReadOnlyListSpan<T> Slice(int start)
    {
        var length = _length - start;
        return Slice(start, length);
    }

    /// <inheritdoc cref="Slice(int)"/>
    public ReadOnlyListSpan<T> Slice(int start, int length)
    {
        if (length + start > _length)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        return new ReadOnlyListSpan<T>(_source, _start + start, length);
    }

    /// <inheritdoc />
    public bool Equals(ReadOnlyListSpan<T> other)
    {
        return ReferenceEquals(_source, other._source) && _start == other._start && _length == other._length;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ReadOnlyListSpan<T> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(_source, _start, _length);
    }
}