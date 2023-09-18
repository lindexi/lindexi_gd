// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

while (true)
{
    Foo();
}

Console.WriteLine("Hello, World!");

void Foo()
{
    var f1 = new F1();
}

class F1
{
    public F1()
    {
        for (int i = 0; i < Fxx.Length; i++)
        {
            Fxx[i] = new F2();
        }
    }

    public F2[] Fxx { set; get; } = new F2[8600];
}
[StructLayout(LayoutKind.Explicit,Size = 100000)]
class F2{}
