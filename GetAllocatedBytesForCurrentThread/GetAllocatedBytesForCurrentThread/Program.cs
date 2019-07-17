using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GetAllocatedBytesForCurrentThread
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var temp in Enumerable.Range(0, 10))
            {
                new Thread(() =>
                {
                    var foo = new Program();

                    for (int i = 0; i < 100; i++)
                    {
                        Console.WriteLine($"线程{Thread.CurrentThread.ManagedThreadId}   {GC.GetAllocatedBytesForCurrentThread()}");
                        foo.Foo();
                    }
                }).Start();
            }

            Console.Read();
        }

        private void Foo()
        {
            var foo = new byte[100];
        }
    }
}
