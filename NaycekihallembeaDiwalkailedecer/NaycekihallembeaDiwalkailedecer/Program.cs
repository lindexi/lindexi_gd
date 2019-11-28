using System;
using System.Threading.Tasks;

namespace NaycekihallembeaDiwalkailedecer
{
    class Program
    {
        static void Main(string[] args)
        {
            var weakReferenceCache = new WeakReferenceCache();
            while (true)
            {
                LocahaibokaileaHelekekujochairkai(weakReferenceCache);
                GC.Collect();
            }
        }

        private static void LocahaibokaileaHelekekujochairkai(WeakReferenceCache weakReferenceCache)
        {
            var foo = weakReferenceCache.GetOrCreate(() => new Foo());
            Console.WriteLine(foo.Count);
        }
    }

    class Foo
    {
        /// <inheritdoc />
        public Foo()
        {
            Count = Ran.Next();
        }

        private static Random Ran { get; } = new Random();

        public int Count { get; }
    }
}