// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

Console.WriteLine("Hello, World!");

var list = new List<Foo>();

for (int i = 0; i < int.MaxValue; i++)
{
    var dictionary = new Dictionary<Foo, int>();
    dictionary.Add(new Foo(), i);
    dictionary.Add(new Foo(), i + 1);
    dictionary.Add(new Foo(), i + 2);

    var first = dictionary.FirstOrDefault();
    if (first.Value != i)
    {
        Debugger.Break();
    }
    else
    {
        dictionary.Remove(first.Key);
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