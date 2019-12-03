using System;
using System.Threading;

namespace HacurbonefeciloQicejewarrerai
{
    class Program
    {
        static void Main(string[] args)
        {
            var mutex = new Mutex(true, Const.Lock, out var createdNew);

            if (!createdNew)
            {
                Console.WriteLine("已经有进程启动");
            }

            Console.ReadKey();

            mutex.Dispose();
        }
    }
}