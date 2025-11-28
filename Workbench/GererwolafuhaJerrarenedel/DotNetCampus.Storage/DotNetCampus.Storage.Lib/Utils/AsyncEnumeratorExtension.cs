using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Storage.Utils;

internal static class AsyncEnumeratorExtension
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var value in source)
        {
            list.Add(value);
        }
        return list;
    }
}
