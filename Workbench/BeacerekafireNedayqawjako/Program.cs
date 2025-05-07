// See https://aka.ms/new-console-template for more information

var foo = new Foo("Foo")
{
    Total = Random.Shared.Next()
};

ThreadPool.SetMinThreads(100, 100);
var manualResetEvent = new ManualResetEvent(false);
for (int i = 0; i < 1000; i++)
{
    Task.Factory.StartNew(() =>
    {
        manualResetEvent.WaitOne();

        while (true)
        {
            foo.Total++;
            Thread.Sleep(100);
        }
    }, TaskCreationOptions.LongRunning);
}

manualResetEvent.Set();

var current = 0;
var count = 0;
while (true)
{
    var total = foo.Total;
    if (total < current)
    {
        Console.WriteLine($"数值返回 Total={total} Current={current}");
        count++;
    }
    else
    {
        count = 0;
    }
    current = total;

    if (count > 100)
    {
        break;
    }
}

Console.WriteLine("Hello, World!");

record Foo(string Name)
{
    public int Total { get; set; }
}