// See https://aka.ms/new-console-template for more information
using System.Threading;

Console.WriteLine("Hello, World!");

var taskList = new List<Thread>();
var locker = new object();

var semaphore = new SemaphoreSlim(0, 1);

var autoResetEvent = new AutoResetEvent(false);

for (int i = 0; i < 100; i++)
{
    var n = i;

    var thread = new Thread(() =>
    {
        autoResetEvent.Set();

        semaphore.Wait();

        lock (locker)
        {
            Console.WriteLine(n);
        }

        semaphore.Release();
    });

    taskList.Add(thread);
    thread.Start();

    autoResetEvent.WaitOne();
}

semaphore.Release();
