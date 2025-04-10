using System;
using System.Buffers;

namespace LightTextEditorPlus.Core.Utils.TextArrayPools;

/// <summary>
/// 数组池里面的数组信息。使用时记得调用释放哦
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct TextPoolArrayContext<T> : IDisposable
{
    internal TextPoolArrayContext(T[] buffer, int minimumLength, ArrayPool<T> arrayPool)
    {
        _buffer = buffer;
        _minimumLength = minimumLength;
        _arrayPool = arrayPool;
    }

    private readonly T[] _buffer;

    private readonly int _minimumLength;

    private readonly ArrayPool<T> _arrayPool;

    /// <summary>
    /// 获取 Span 类型的内容
    /// </summary>
    public Span<T> Span => _buffer.AsSpan(0, _minimumLength);

    /// <summary>
    /// 获取 Memory 类型的内容
    /// </summary>
    public Memory<T> Memory => _buffer.AsMemory(0, _minimumLength);

    /// <inheritdoc />
    public void Dispose()
    {
        _arrayPool.Return(_buffer);
    }
}