// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;

for (int i = 0; i < int.MaxValue; i++)
{
    var dictionary = new ConcurrentDictionary<Foo, int>();
    dictionary.TryAdd(new Foo(), i);
    dictionary.TryAdd(new Foo(), i + 1);

    var first = dictionary.FirstOrDefault();
    if (first.Value != i)
    {
        // 证明首个不是第一个加入的
        Console.WriteLine($"首个不是第一个加入的");
        return;
    }
}

class Foo
{
    public Foo()
    {
        Number = _count;
        _count++;
    }

    private static int _count;

    public int Number { get; }
}