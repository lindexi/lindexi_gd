using System;
using System.Buffers;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Utils;

/// <summary>
/// 从 <see cref="CharData"/> 列表转换为 <see cref="char"/> 列表的结果。需要调用释放，将数组归还到池中
/// </summary>
public readonly struct CharDataListToCharSpanResult : IDisposable
{
    internal CharDataListToCharSpanResult(char[] buffer, int length, ArrayPool<char> arrayPool)
    {
        _buffer = buffer;
        _length = length;
        _arrayPool = arrayPool;
    }

    /// <summary>
    /// 转换后的字符数组
    /// </summary>
    public ReadOnlySpan<char> CharSpan => _buffer.AsSpan(0, _length);

    private readonly char[] _buffer;
    private readonly int _length;
    private readonly ArrayPool<char> _arrayPool;

    /// <inheritdoc />
    public void Dispose()
    {
        _arrayPool.Return(_buffer);
    }
}