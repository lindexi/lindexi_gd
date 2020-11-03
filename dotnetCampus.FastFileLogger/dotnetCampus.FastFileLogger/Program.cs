using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace dotnetCampus.FastFileLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            // 创建出来就能使用
            var fileLogger = new FileLogger();

            const string text = "text";
            var taskList = new List<Task>();

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                taskList.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 10000; j++)
                    {
                        fileLogger.QueueWriteLineToFile(text);
                    }
                }));
            }

            stopwatch.Stop();

            fileLogger.Initialize(new FileInfo("log.txt"));

            Task.WaitAll(taskList.ToArray());
            Console.WriteLine($"耗时 {stopwatch.ElapsedMilliseconds}");

            stopwatch.Restart();
            fileLogger.DisposeAsync().Wait();
            stopwatch.Stop();
            Console.WriteLine($"耗时 {stopwatch.ElapsedMilliseconds}");
        }
    }
}