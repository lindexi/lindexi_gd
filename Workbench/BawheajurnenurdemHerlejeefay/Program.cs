// See https://aka.ms/new-console-template for more information

for (var i = 0; i < 100000; i++)
{
    var origin = Random.Shared.Next();
    var a = Random.Shared.Next();
    var b = Random.Shared.Next();

    var t = origin ^ a;
    t = t ^ b;

    var t2 = origin ^ b;
    t2 = t2 ^ a;

    var t3 = t ^ a ^ b;
    var t4 = t2 ^ b ^ a;

    if (t == t2)
    {
        // 预期是相等的
    }
    else
    {
        return;
    }

    if (t3 == origin)
    {

    }
    else
    {
        return;
    }

    if (t3 == t4)
    {

    }
    else
    {
        return;
    }
}

Console.WriteLine("Hello, World!");
