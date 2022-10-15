// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

AsyncLocal<Foo> fooAsyncLocal = new AsyncLocal<Foo>();

for (int i = 0; i < 100; i++)
{
    Console.WriteLine($"Name={fooAsyncLocal.Value?.Name}");
    F1(i);
    F2();
}

void F1(int i)
{
    fooAsyncLocal.Value = new Foo()
    {
        Name = i.ToString()
    };
}

void F2()
{
    Console.WriteLine($"F2 {fooAsyncLocal.Value?.Name}");
}

class Foo
{
    public string Name { set; get; }
}