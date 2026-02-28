using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWrite.Utils;

internal static class LinqExtensions
{
    /// <summary>
    /// 当序列是否有且仅有一个元素时返回，其他情况返回空
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static T? OneOrDefault<T>(this IEnumerable<T> source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (source is IList<T> list)
        {
            return list.Count == 1 ? list[0] : default;
        }

        using (var e = source.GetEnumerator())
        {
            if (e.MoveNext())
            {
                var one = e.Current;
                return e.MoveNext() ? default(T) : one;
            }

            return default(T);
        }
    }

    /// <summary>
    /// 当序列是否有且仅有一个元素时返回，其他情况返回空
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static TSource? OneOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        var predicatedEnumerable = source.Where(predicate);
        using (var enumerator = predicatedEnumerable.GetEnumerator())
        {
            if (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (!enumerator.MoveNext())
                    return current;
            }
            return default(TSource);
        }
    }
}
