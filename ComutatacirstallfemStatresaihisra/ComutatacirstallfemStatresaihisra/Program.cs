using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ComutatacirstallfemStatresaihisra
{
    class Program
    {
        static void Main(string[] args)
        {
            var threadManagerService = new ThreadManagerService();
            //threadManagerService.AddBackgroundTask(new BackgroundTask("等待100毫秒的任务", () => { },
            //    TimeSpan.FromMilliseconds(100)));
            //threadManagerService.AddBackgroundTask(new BackgroundTask("等待150毫秒的任务", () => { },
            //    TimeSpan.FromMilliseconds(150)));
            //threadManagerService.AddBackgroundTask(new BackgroundTask("等待500毫秒的任务", () => { },
            //    TimeSpan.FromMilliseconds(500)));
            //threadManagerService.Start();

            //Task.Delay(3000).ContinueWith(_ =>
            //{
            //    threadManagerService.AddBackgroundTask(new BackgroundTask("等待200毫秒", () => { },
            //        TimeSpan.FromMilliseconds(200)));
            //});

            var random = new Random();
            var backgroundTaskList = new List<BackgroundTask>();
            while (true)
            {
                Task.Delay(random.Next(1000, 3000)).Wait();
                if (random.Next(2) == 1 ||backgroundTaskList.Count==0)
                {
                    var n = random.Next(100, 1000);
                    var backgroundTask = new BackgroundTask($"等待{n}毫秒", () => { }, TimeSpan.FromMilliseconds(n));
                    backgroundTaskList.Add(backgroundTask);
                    threadManagerService.RunBackgroundTask(backgroundTask);
                }
                else
                {
                    var backgroundTask = backgroundTaskList[random.Next(backgroundTaskList.Count)];
                    backgroundTaskList.Remove(backgroundTask);
                    threadManagerService.RemoveBackgroundTask(backgroundTask);
                }

                if (random.Next() == 1000)
                {
                    break;
                }
            }

            Console.Read();
        }
    }
}