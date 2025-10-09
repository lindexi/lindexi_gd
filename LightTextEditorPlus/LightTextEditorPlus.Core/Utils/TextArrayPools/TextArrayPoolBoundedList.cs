using System;
using System.Buffers;

namespace LightTextEditorPlus.Core.Utils.TextArrayPools;

/// <summary>
/// 数组池的有边界固定最大尺寸的列表
/// </summary>
/// <remarks>
/// 适合瞬时用完，禁止传递，因为这是结构体。传递过程就是拷贝过程。仅用于文本库框架内，业务代码如不了解实现细节请勿使用
/// </remarks>
public struct TextArrayPoolBoundedList<T> : IDisposable
{
    /// <summary>
    /// 创建关联数组池的有边界的列表
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="arrayPool"></param>
    public TextArrayPoolBoundedList(T[] buffer, ArrayPool<T> arrayPool)
    {
        _buffer = buffer;
        _arrayPool = arrayPool;
    }

    /// <summary>
    /// 创建关联数组池的有边界的列表
    /// </summary>
    /// <param name="minimumLength"></param>
    /// <param name="arrayPool"></param>
    public TextArrayPoolBoundedList(int minimumLength, ArrayPool<T>? arrayPool = null)
    {
        arrayPool ??= ArrayPool<T>.Shared;
        _arrayPool = arrayPool;
        _buffer = arrayPool.Rent(minimumLength);
    }

    /// <summary>
    /// 数量
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets the capacity of this list.
    /// </summary>
    public int Capacity => _buffer.Length;

    /// <summary>
    /// Sets or Gets the element at the given index.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public T this[int index]
    {
        get
        {
            if (index >= _count)
            {
                throw new IndexOutOfRangeException();
            }
            return _buffer[index];
        }
        set
        {
            if (index >= _count)
            {
                throw new IndexOutOfRangeException();
            }
            _buffer[index] = value;
        }
    }

    /// <summary>
    /// 添加元素
    /// </summary>
    /// <param name="item"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void Add(T item)
    {
        var currentCount = _count + 1;
        if (currentCount > _buffer.Length)
        {
            throw new ArgumentOutOfRangeException();
        }

        _buffer[_count] = item;
        _count = currentCount;
    }

    private int _count;
    private readonly T[] _buffer;
    private readonly ArrayPool<T> _arrayPool;

    /// <summary>
    /// 转换出 Span 对象，只能表示当前的状态
    /// </summary>
    /// <returns></returns>
    public Span<T> ToSpan() => _buffer.AsSpan(0, _count);

    /// <inheritdoc />
    public void Dispose()
    {
        _arrayPool.Return(_buffer);
    }
}