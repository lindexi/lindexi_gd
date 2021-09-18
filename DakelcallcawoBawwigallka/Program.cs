using System;
using System.Runtime.CompilerServices;

namespace DakelcallcawoBawwigallka
{
    class Program
    {
        static void Main(string[] args)
        {
            var foo = new Foo()
            {
                Number = 10
            };

            var fooF2 = Unsafe.As<IF2>(foo);
            Console.WriteLine(fooF2.Number);

            var fx = Unsafe.As<Fx>(foo);
            fx.Report(foo);
            object o = fx;
            if (o is IF3 foo3)
            {
                // 这里是否会进来
            }

            if (fx is IF3 f3)
            {
                // 这里是否会进来
                Console.WriteLine(f3.Number); // 这里会不会炸
            }

            var fooF3 = Unsafe.As<IF3>(foo);
            Console.WriteLine(fooF3.Number);

            var emptyObject = EmptyObject.GetEmptyObject<Foo>();
            Console.WriteLine(emptyObject.Number);
            emptyObject.Report(null);

            var f2 = emptyObject as IF2;
            Console.WriteLine(f2.Number);

            IProgress<Foo> p = emptyObject as IProgress<Foo>;
            p.Report(null);

            object obj = new object();
            IProgress<Foo> progress = Unsafe.As<IProgress<Foo>>(obj);

            progress.Report(new Foo());
        }
    }

    class Fx : IF3
    {
        public int Number { set; get; }

        public void Report(Foo value)
        {
            Number += value.Number;
            Console.WriteLine(Number);
        }
    }

    interface IF2
    {
        int Number { get; }
    }

    interface IF3
    {
        int Number { get; }
    }

    class Foo: IProgress<Foo>, IF2
    {
        public int Number { set; get; }


        public void Report(Foo value)
        {
            Number++;
        }
    }

    static class EmptyObject
    {
        public static T GetEmptyObject<T>() 
        where T:class
            => Unsafe.As<T>(Empty);

        private static readonly object Empty = new object();
    }
}
