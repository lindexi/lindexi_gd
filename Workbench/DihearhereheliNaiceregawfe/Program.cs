// See https://aka.ms/new-console-template for more information

using PowerThreadPool;

var powerPool = new PowerPool();
powerPool.Start();

powerPool.QueueWorkItem(() =>
{
    // Do something
});

Console.WriteLine("Hello, World!");