using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace CallbadojuBaheanurjair
{
    class Program
    {
        static void Main(string[] args)
        {
            _taskCompletionSource = new TaskCompletionSource<bool>();

            Foo();

            var taskList = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                var task = Task.Run(() =>
                {
                    _taskCompletionSource.TrySetResult(true);
                });

                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());

            Console.Read();
        }

        private static async void Foo()
        {
            await _taskCompletionSource.Task;
            Console.WriteLine("F");
        }

        private static TaskCompletionSource<bool> _taskCompletionSource;
    }
}
