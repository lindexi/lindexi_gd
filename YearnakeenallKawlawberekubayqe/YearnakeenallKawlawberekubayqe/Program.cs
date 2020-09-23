using System;
using System.Threading;
using System.Threading.Tasks;
using dotnetCampus.Threading;

namespace YearnakeenallKawlawberekubayqe
{
    class Program
    {
        static void Main(string[] args)
        {
            var random = new Random();
            var asyncQueue = new AsyncQueue<FooTask>();

            for (int i = 0; i < 100; i++)
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        asyncQueue.Enqueue(new FooTask());

                        await Task.Delay(random.Next(1000));
                    }
                });
            }

            for (int i = 0; i < 10; i++)
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        var fooTask = await asyncQueue.DequeueAsync();
                        fooTask.Do();
                        Console.WriteLine($"剩余 {asyncQueue.Count}");
                        await Task.Delay(random.Next(50));
                    }
                });
            }

            Console.Read();
        }
    }

    class FooTask
    {
        public void Do()
        {
            Console.WriteLine("DoTask");
        }
    }
}
