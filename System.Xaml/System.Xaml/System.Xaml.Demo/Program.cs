using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xaml.Schema;

namespace System.Xaml.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var typeReflector = new TypeReflector(typeof(Program));

            var manualResetEvent = new ManualResetEvent(false);
            var count = 10;
            object[] objectList = new object[count];
            var taskList = new Task[count];

            for (int i = 0; i < count; i++)
            {
                var n = i;
                taskList[n] = Task.Run(() =>
                {
                    manualResetEvent.WaitOne();

                    var dictionary = typeReflector.Members;
                    objectList[n] = dictionary;
                });
            }

            manualResetEvent.Set();

            Task.WaitAll(taskList);

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    if (!ReferenceEquals(objectList[i], objectList[j]))
                    {
                        Debugger.Break();
                    }
                }
            }
        }
    }
}
