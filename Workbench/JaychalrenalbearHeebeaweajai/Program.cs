// See https://aka.ms/new-console-template for more information

var dictionary = new Dictionary<long, Configuration>();

object lockObject = new object();

for (int i = 0; i < 10; i++)
{
    Test();
}

for (int i = 0; i < 10; i++)
{
    Test2();
}

Thread.Sleep(TimeSpan.FromDays(10));
Console.WriteLine("Hello, World!");

void Test()
{
    Task.Run(() =>
    {
        for (long i = 0; i < long.MaxValue; i++)
        {
            _ = TryGetValue(i);
        }
    });
}

void Test2()
{
    for (long i = 0; i < long.MaxValue; i++)
    {
        if (dictionary.TryGetValue(i, out var value))
        {
            _ = value;
        }
    }
}

Configuration TryGetValue(long key)
{
    if (dictionary.TryGetValue(key, out var value))
    {
        return value;
    }

    lock (lockObject)
    {
        if (dictionary.TryGetValue(key, out value))
        {
            return value;
        }

        value = new Configuration();
        dictionary[key] = value;
        if (dictionary.Count > 1000)
        {
            for (int i = 0; i < key - 100; i++)
            {
                dictionary.Remove(i);
            }
        }
        return value;
    }
}

class Configuration
{
}