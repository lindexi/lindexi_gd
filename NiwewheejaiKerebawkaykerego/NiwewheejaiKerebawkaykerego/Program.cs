using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NiwewheejaiKerebawkaykerego
{
    class Program
    {
        static void Main(string[] args)
        {
            Foo.StaticProperty = "普通静态属性";
            Foo.ThreadStaticProperty = "线程静态属性";

            var taskList = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                var n = i;
                var task = new Task(() =>
                {
                    Console.WriteLine(
                        $"thread={Thread.CurrentThread.ManagedThreadId} 静态属性={Foo.StaticProperty} 线程静态属性={Foo.ThreadStaticProperty} 次数={n}");

                    Foo.StaticProperty = n.ToString();
                    Foo.ThreadStaticProperty = n.ToString();
                });

                task.Start();
                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());
        }
    }

    class Foo
    {
        public static string StaticProperty
        {
            get => _staticProperty;
            set => _staticProperty = value;
        }

        public static string ThreadStaticProperty
        {
            get => _threadStaticProperty;
            set => _threadStaticProperty = value;
        }

        [ThreadStatic] private static string _threadStaticProperty = "初始值";
        private static string _staticProperty = "初始值";
    }
}