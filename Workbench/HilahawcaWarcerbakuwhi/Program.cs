// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

Console.WriteLine(string.Equals("Straße", "Strae", StringComparison.InvariantCultureIgnoreCase));
Console.WriteLine(string.Equals("Straße", "STRASSE", StringComparison.OrdinalIgnoreCase));

unsafe
{
    Debugger.Break();

    F* foo = Foo(10);
    //F2(100, 20);

    var a1 = foo->A1;

    var a2 = foo->A2;
    var a3 = foo->A3;

    GC.KeepAlive(a1);
    GC.KeepAlive(a2);
    GC.KeepAlive(a3);

    Console.WriteLine($"{a1} {a2} {a3}");


    F* Foo(int count)
    {
        if (count == 0)
        {
            F f = new F()
            {
                A1 = 100,
                A2 = 200,
                A3 = 300
            };

            return &f;
        }
        else
        {
            count--;
            return Foo(count);
        }
    }

    int F2(int n, int count)
    {
        if (count == 0)
        {
            return n;
        }

        if (n == count)
        {
            return n;
        }

        count--;
        n = Random.Shared.Next();
        return F2(n, count);
    }
}


Console.WriteLine("Hello, World!");



struct F
{
    public int A1;
    public int A2;
    public int A3;
}