// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var taskList = new List<Task>();
var mutex = new Mutex(false);
var locker = new object();
mutex.WaitOne();

var autoResetEvent = new AutoResetEvent(false);

for (int i = 0; i < 100; i++)
{
    var n = i;
    taskList.Add(Task.Run(() =>
    {
        autoResetEvent.Set();

        mutex.WaitOne();

        lock (locker)
        {
            Console.WriteLine(n);
        }

        mutex.ReleaseMutex();
    }));

    autoResetEvent.WaitOne();
}

mutex.ReleaseMutex();
Task.WaitAll(taskList.ToArray());
