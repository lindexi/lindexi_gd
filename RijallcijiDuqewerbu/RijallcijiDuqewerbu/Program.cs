// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var taskList = new List<Task>();
var locker = new object();

ThreadPool.SetMinThreads(100, 100);
ThreadPool.SetMaxThreads(100,100);
var semaphore = new SemaphoreSlim(0, 1);

var autoResetEvent = new AutoResetEvent(false);

for (int i = 0; i < 100; i++)
{
    var n = i;
    taskList.Add(Task.Run(() =>
    {
        autoResetEvent.Set();

        semaphore.Wait();

        lock (locker)
        {
            Console.WriteLine(n);
        }

        semaphore.Release();
    }));

    autoResetEvent.WaitOne();
}

semaphore.Release();

Task.WaitAll(taskList.ToArray());
