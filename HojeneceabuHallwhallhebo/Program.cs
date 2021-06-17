using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HojeneceabuHallwhallhebo
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var program = new Program();
            program.F1();

            GC.Collect();
            GC.WaitForFullGCComplete();
            GC.Collect();

            Task.Delay(1000).Wait();

            Console.WriteLine("Hello World!");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
        }

        private void F1()
        {
            try
            {
                _ = new Foo();
            }
            catch
            {
               // 忽略
            }
        }
    }

    class Foo
    {
        public Foo()
        {
            throw new Exception("lindexi is doubi");
        }

        ~Foo()
        {
            throw new Exception("lsj is doubi");
        }
    }
}
