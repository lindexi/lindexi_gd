using System;
using System.Threading;

namespace WayhemcurwelWemqonairbay
{
    class Program
    {
        static void Main(string[] args)
        {
            F3();

            GC.Collect();
            GC.WaitForFullGCComplete();
            GC.Collect();

            Console.WriteLine(_weakReference1.TryGetTarget(out _));
            Console.WriteLine(_weakReference2.TryGetTarget(out _));

            Console.Read();
        }

        private static void F3()
        {
            _weakReference1 = new WeakReference<Foo>(new Foo());

            var foo = new Foo();
            _weakReference2 = new WeakReference<Foo>(foo);

            foo.F1();
        }

        private static WeakReference<Foo> _weakReference1;
        private static WeakReference<Foo> _weakReference2;
    }

    class Foo
    {
        public void F1()
        {
            _semaphoreSlim = new SemaphoreSlim(0);
            F2();
            _semaphoreSlim.Dispose();
            _semaphoreSlim = null;
        }

        private async void F2()
        {
            await _semaphoreSlim.WaitAsync();
            Console.WriteLine("F");
        }

        private SemaphoreSlim _semaphoreSlim;
    }
}
