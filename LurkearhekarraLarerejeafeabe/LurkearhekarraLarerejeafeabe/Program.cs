using System;
using System.Threading;
using System.Threading.Tasks;

namespace LurkearhekarraLarerejeafeabe
{
    class Program
    {
        static void Main(string[] args)
        {
            Foo.Value = new Foo()
            {
                Count = 1
            };

            var taskList = new Task[100];

            for (int i = 0; i < 100; i++)
            {
                var n = i;
                var task = Task.Run(() =>
                {
                    Console.WriteLine($"Task {Foo.Value.Count}");

                    Foo.Value = new Foo()
                    {
                        Count = n
                    };

                    Console.WriteLine($"Task {n} {Foo.Value.Count}");
                });

                taskList[n] = task;
            }

            Task.WaitAll(taskList);

            Console.WriteLine(Foo.Value.Count);

            Console.Read();
        }

        private static AsyncLocal<Foo> Foo { get; } = new AsyncLocal<Foo>();
    }

    class Foo
    {
        public int Count { set; get; }
    }
}