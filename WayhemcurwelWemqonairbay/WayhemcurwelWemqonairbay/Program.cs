using System;
using System.Threading;

namespace WayhemcurwelWemqonairbay
{
    class Program
    {
        static void Main(string[] args)
        {
            _semaphoreSlim = new SemaphoreSlim(0);

            Foo();

            Thread.Sleep(1000);
            _semaphoreSlim.Dispose();
            Thread.Sleep(1000);
            Console.Read();
        }

        private static async void Foo()
        {
            await _semaphoreSlim.WaitAsync();
            Console.WriteLine("F");
        }

        private static SemaphoreSlim _semaphoreSlim;

    }
}
