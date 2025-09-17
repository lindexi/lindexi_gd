// See https://aka.ms/new-console-template for more information

Dictionary<int, Foo> dictionary = new();

for (int i = 0; i < 100; i++)
{
    dictionary[i] = new Foo();
}

var manualResetEvent = new ManualResetEvent(false);

for (int i = 0; i < 100; i++)
{
    var thread = new Thread(() =>
    {
        manualResetEvent.WaitOne();

        foreach (var foo in dictionary.Values)
        {
            foo.Count++;
        }
    })
    {
        IsBackground = true
    };
    thread.Start();
}

manualResetEvent.Set();

Console.WriteLine("Hello, World!");
while (true)
{
    Console.Read();
}

record Foo
{
    public int Count { get; set; }
}