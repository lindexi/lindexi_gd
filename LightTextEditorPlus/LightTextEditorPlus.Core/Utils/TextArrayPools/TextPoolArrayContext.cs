using System;
using System.Buffers;

namespace LightTextEditorPlus.Core.Utils.TextArrayPools;

/// <summary>
/// 数组池里面的数组信息。使用时记得调用释放哦
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct TextPoolArrayContext<T> : IDisposable
{
    internal TextPoolArrayContext(T[] buffer, int start, int length, ArrayPool<T> arrayPool)
    {
        _buffer = buffer;
        _start = start;
        _length = length;
        _arrayPool = arrayPool;
    }

    private readonly T[] _buffer;

    private readonly int _start;
    private readonly int _length;

    private readonly ArrayPool<T> _arrayPool;

    /// <summary>
    /// 获取 Span 类型的内容
    /// </summary>
    public Span<T> Span => _buffer.AsSpan(_start, _length);

    /// <summary>
    /// 获取 Memory 类型的内容
    /// </summary>
    public Memory<T> Memory => _buffer.AsMemory(_start, _length);

    /// <summary>
    /// 分割
    /// </summary>
    /// <param name="start"></param>
    /// <returns></returns>
    public TextPoolArrayContext<T> Slice(int start)
    {
        return Slice(start, _length - start);
    }

    /// <summary>
    /// 分割
    /// </summary>
    /// <param name="start"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public TextPoolArrayContext<T> Slice(int start, int count)
    {
        return new TextPoolArrayContext<T>(_buffer, _start + start, count, _arrayPool);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _arrayPool.Return(_buffer);
    }
}