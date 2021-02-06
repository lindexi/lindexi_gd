using System;
using System.Threading;
using System.Threading.Tasks;

namespace WeceqalchekairKalayhali
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < 100; i++)
            {
                var n = i;
                Task.Run(() =>
                {
                    Foo.Value = new Foo();
                    var foo = Foo.Value;
                    foo.Count = n;
                    F1(n);
                });

                var thread = new Thread(() =>
                {
                    Foo.Value = new Foo();
                    var foo = Foo.Value;
                    foo.Count = n;
                    F1(n);
                });
                thread.Start();
            }

            Console.Read();
        }

        private static void F1(int n)
        {
            Console.WriteLine($"F1 {n} {Foo.Value.Count}");

            Task.Run(() =>
            {
                Console.WriteLine($"F1 Task {n} {Foo.Value.Count}");
            });
        }

        private static AsyncLocal<Foo> Foo { get; } = new AsyncLocal<Foo>();
    }

    class Foo
    {
        public int Count { get; set; }
    }
}
