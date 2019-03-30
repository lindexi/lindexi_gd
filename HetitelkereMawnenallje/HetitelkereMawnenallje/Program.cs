using System;
using System.Collections.Generic;
using System.Linq;

namespace HetitelkereMawnenallje
{
    class Program
    {
        static void Main(string[] args)
        {
            List<int> foo = null;
            foreach (var temp in foo.GetNotNullEnumerable())
            {
                
            }
        }
    }

    public static class ListExtension
    {
        public static IEnumerable<T> GetNotNullEnumerable<T>(this IEnumerable<T> enumerable) =>
            enumerable ?? Enumerable.Empty<T>();
    }
}