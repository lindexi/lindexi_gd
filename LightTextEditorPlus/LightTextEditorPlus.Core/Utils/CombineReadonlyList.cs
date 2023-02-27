using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LightTextEditorPlus.Core.Utils;

/// <summary>
/// 只读列表的组合
/// </summary>
/// <typeparam name="T"></typeparam>
/// [dotnet 不申请额外数组空间合并多个只读数组列表](https://blog.lindexi.com/post/dotnet-%E4%B8%8D%E7%94%B3%E8%AF%B7%E9%A2%9D%E5%A4%96%E6%95%B0%E7%BB%84%E7%A9%BA%E9%97%B4%E5%90%88%E5%B9%B6%E5%A4%9A%E4%B8%AA%E5%8F%AA%E8%AF%BB%E6%95%B0%E7%BB%84%E5%88%97%E8%A1%A8.html )
internal class CombineReadonlyList<T> : IReadOnlyList<T>
{
    public CombineReadonlyList(params IReadOnlyList<T>[] source)
    {
        Source = source;
    }

    public IReadOnlyList<T>[] Source { get; }

    public IEnumerator<T> GetEnumerator()
    {
        return Source.SelectMany(readOnlyList => readOnlyList).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => Source.Sum(temp => temp.Count);

    public T this[int index]
    {
        get
        {
            var n = index;
            var source = Source;

            foreach (var list in source)
            {
                if (n < list.Count)
                {
                    return list[n];
                }

                n -= list.Count;
            }

            throw new IndexOutOfRangeException();
        }
    }
}