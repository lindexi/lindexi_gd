// See https://aka.ms/new-console-template for more information

var foo = new Foo("Foo");

for (int i = 0; i < 1000; i++)
{
    Task.Run(async () =>
    {
        while (true)
        {
            foo.Total++;
            await Task.Delay(100);
        }
    });
}

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
    current = total;

    if (count > 10)
    {
        break;
    }
}

Console.WriteLine("Hello, World!");

record Foo(string Name)
{
    public int Total { get; set; }
}