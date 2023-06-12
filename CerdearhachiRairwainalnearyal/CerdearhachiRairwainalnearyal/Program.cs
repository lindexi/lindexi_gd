// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using System.Diagnostics;

Console.WriteLine("Hello, World!");

var list = new List<Foo>();

for (int i = 0; i < int.MaxValue; i++)
{
    var dictionary = new ConcurrentDictionary<Foo, int>();
    dictionary.TryAdd(new Foo(), i);
    dictionary.TryAdd(new Foo(), i + 1);
    dictionary.TryAdd(new Foo(), i + 2);

    var first = dictionary.FirstOrDefault();
    if (first.Value != i)
    {
        Debugger.Break();
    }
    else
    {
        first = dictionary.FirstOrDefault();

        if (Random.Shared.Next(10) == 1)
        {
            if (list.Count > 100)
            {
                while (list.Count>50)
                {
                    list.RemoveAt(0);
                }
                list.RemoveAt(0);
            }

            list.Add(first.Key);
        }
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