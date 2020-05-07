using System;
using System.Threading;
using System.Threading.Tasks;

namespace CallnernawbawceKairwemwhejeene
{
    class Context : IContext
    {
        public async Task WaitForContinue()
        {
            await (CurrentTask?.Task ?? Task.CompletedTask);
        }

        private TaskCompletionSource<bool> CurrentTask { set; get; }

        public void SetState(State state)
        {
            switch (state)
            {
                case State.Continue:
                    CurrentTask?.TrySetResult(false);
                    CurrentTask = null;
                    break;
                case State.Pause:
                    CurrentTask ??= new TaskCompletionSource<bool>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    enum State
    {
        Continue,
        Pause,
    }

    interface IContext
    {
        Task WaitForContinue();
    }

    class Program
    {
        static void Main(string[] args)
        {
            var context = new Context();
            Task.Run(() =>
            {
                var random = new Random();
                while (true)
                {
                    Task.Run(() => context.SetState(State.Pause)).Wait();
                    Task.Delay(random.Next(1000, 3000)).Wait();
                    Task.Run(() =>
                    {
                        context.SetState(State.Continue);
                    });
                }
            });
            Task.Delay(100).Wait();
            Foo(context).Wait();
        }

        static async Task Foo(IContext context)
        {
            var n = 0;
            var n1 = 1;
            var n2 = 1;
            while (n1 > 0)
            {
                F1();
                await context.WaitForContinue();
                n1 = F2(n, n1);
                await context.WaitForContinue();
                n = F3(n1, n2);
                await context.WaitForContinue();
                await Task.Delay(10);
            }
        }

        private static int F3(int n1, in int n2)
        {
            Task.Delay(10).Wait();
            Console.WriteLine($"{DateTime.Now:hh:mm:ss} F3");
            return 10;
        }

        private static int F2(int n, int n1)
        {
            Task.Delay(10).Wait();
            Console.WriteLine($"{DateTime.Now:hh:mm:ss} F2");
            return 10;
        }

        private static void F1()
        {
            Task.Delay(10).Wait();

            Console.WriteLine($"{DateTime.Now:hh:mm:ss} F1");
        }
    }
}