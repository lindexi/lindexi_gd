using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LightTextEditorPlus.Core.Primitive.Collections;

/// <summary>
/// 只读列表转换器，将一个类型的只读列表转换为另一个类型的只读列表
/// </summary>
/// <typeparam name="TOrigin"></typeparam>
/// <typeparam name="TOther"></typeparam>
public class TextReadOnlyListConverter<TOrigin, TOther> : IReadOnlyList<TOther>
{
    /// <summary>
    /// 创建一个只读列表转换器，将一个类型的只读列表转换为另一个类型的只读列表
    /// </summary>
    /// <param name="list"></param>
    /// <param name="converter"></param>
    public TextReadOnlyListConverter(IReadOnlyList<TOrigin> list, Func<TOrigin, TOther> converter)
    {
        _list = list;
        _converter = converter;
    }

    private readonly IReadOnlyList<TOrigin> _list;

    private readonly Func<TOrigin, TOther> _converter;

    /// <inheritdoc />
    public IEnumerator<TOther> GetEnumerator()
    {
        return _list.Select(origin => _converter(origin)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public int Count => _list.Count;

    /// <inheritdoc />
    public TOther this[int index]
    {
        get => _converter(_list[index]);
    }
}