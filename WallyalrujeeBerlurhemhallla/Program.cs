using System;
using System.Threading;
using System.Threading.Tasks;

namespace WallyalrujeeBerlurhemhallla
{
    class Program
    {
        static void Main(string[] args)
        {
            object obj = new object();

            Task.Run(() =>
            {
                while (Monitor.IsEntered(obj))
                {
                    Monitor.Exit(obj);
                }

                if (Monitor.TryEnter(obj))
                {
                }

                Monitor.Enter(obj);

                while (true)
                {
                    Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
                    Monitor.Enter(obj);
                    Monitor.Exit(obj);
                }
            });

            while (Monitor.IsEntered(obj))
            {
                Monitor.Exit(obj);
            }

            Monitor.Enter(obj);

            Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();

            Monitor.Enter(obj);

            Monitor.TryEnter(obj);

            while (Monitor.IsEntered(obj))
            {
                Monitor.Exit(obj);
            }

            Console.Read();
        }
    }
}