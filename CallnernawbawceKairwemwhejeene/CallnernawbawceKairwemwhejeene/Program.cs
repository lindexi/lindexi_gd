using System;
using System.Threading;
using System.Threading.Tasks;

namespace CallnernawbawceKairwemwhejeene
{
    class SycnContext : SynchronizationContext
    {
        /// <inheritdoc />
        public override void Post(SendOrPostCallback d, object state)
        {
            Run = () => d(state);
            Event.Set();
        }

        /// <inheritdoc />
        public override void Send(SendOrPostCallback d, object state)
        {
            // 用于了解执行完成
            AutoResetEvent autoResetEvent = new AutoResetEvent(false);
            Run = () =>
            {
                d(state);
                autoResetEvent.Set();
            };
            Event.Set();
            autoResetEvent.WaitOne();
        }

        public Action Run { private set; get; }

        public AutoResetEvent Event { get; } = new AutoResetEvent(false);
    }


    class Program
    {
        static void Main(string[] args)
        {
            var synchronizationContext = new SycnContext();
            
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
         
            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}");
            Foo();

            while (true)
            {
                synchronizationContext.Event.WaitOne();
                synchronizationContext.Run();
            }
        }

        private static async void Foo()
        {
            var task = Task.Run(async () =>
            {
                await Task.Delay(100);
                Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}");
            });
            await task;
            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}");
        }
    }
}