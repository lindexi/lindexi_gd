using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace JewhelgairrairJikailairyairbar
{
    class Program
    {
        static void Main(string[] args)
        {
            //var program = new Program();
            //var autoResetEvent = new AutoResetEvent(false);
            //var manualResetEvent = new ManualResetEvent(false);

            //var task1 = Task.Run(() =>
            //{
            //    lock (program)
            //    {
            //        // 用于让 task1 执行到这里才让 task2 执行
            //        autoResetEvent.Set();

            //        // 用于等待 task2 执行完成
            //        manualResetEvent.WaitOne();
            //    }
            //});

            //var task2 = Task.Run(() =>
            //{
            //    // 用于等待 task1 执行
            //    autoResetEvent.WaitOne();

            //    // 调用禁止冲入的方法
            //    program.F1();

            //    // 如果上面代码调用返回，那么让 tas1 继续执行
            //    manualResetEvent.Set();
            //});

            //Task.WaitAll(task1, task2);

            var program = new Program();
            var taskList = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                taskList.Add(Task.Run(program.F2)); ;
            }

            Task.WaitAll(taskList.ToArray());
        }

        private void F2()
        {
            //var doingCount = Interlocked.Exchange(ref _doingCount, 1);

            //if (doingCount == 0)
            //{
            //    // 执行代码
            //    Console.WriteLine("执行逻辑");
            //}

            var bounded = System.Threading.Channels.Channel.CreateBounded<int>(10);
           
        }

        //private int _doingCount;

        private readonly object _locker = new object();

        private void A()
        {
            B();
        }

        private void B()
        {
            A();
        }

        private void FooLock()
        {
            lock (_locker)
            {
                // 代码
            }
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        private void F1()
        {
            Console.WriteLine("执行逻辑");
        }
    }
}