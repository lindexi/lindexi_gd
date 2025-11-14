// See https://aka.ms/new-console-template for more information

using System.Text.Json;

Foo foo = new();

var json = JsonSerializer.Serialize(foo);

Console.WriteLine("Hello, World!");

class Foo
{
    public int N1 { get; set; } = 1;
    public int N2 { get; set; } = 3;

    public List<F2> F2List { get; set; } = new List<F2>()
    {
        new F2(),
        new F2(),
        new F2(),
        new F2(),
        new F2(),
        new F2(),
    };
}

class F2
{
    public int N3 { get; set; } = Random.Shared.Next();
}