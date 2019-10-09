using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fujeencemwebaeahale
{
    class Program
    {
        static void Main(string[] args)
        {
            var str1 = new[] {"123", "123"};
            var str2 = new[] {"233", "32"};
            var str3 = new List<string>();
            for (int i = 10; i < 100; i++)
            {
                str3.Add(i.ToString());
            }

            var combineReadonlyList = new CombineReadonlyList<string>(str1, str2, str3);

            for (var i = 0; i < combineReadonlyList.Count; i++)
            {
                Console.WriteLine(combineReadonlyList[i]);
            }

            var readOnlyCollection = combineReadonlyList as IReadOnlyCollection<string>;
        }
    }

    public class CombineReadonlyCollection<T> : IReadOnlyCollection<T>
    {
        public CombineReadonlyCollection(params IReadOnlyCollection<T>[] source)
        {
            Source = source;
        }

        public IReadOnlyCollection<T>[] Source { get; }

        public IEnumerator<T> GetEnumerator()
        {
            return Source.SelectMany(readOnlyList => readOnlyList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => Source.Sum(temp => temp.Count);
    }

    public class CombineReadonlyList<T> : IReadOnlyList<T>
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
}