using System.Runtime.CompilerServices;

var f1 = new F1();

Console.WriteLine("Hello, World!");


class F1
{
    public F1()
    {
        var f2 = new F2();
    }
}

class F2
{
    [MethodImplAttribute(MethodImplOptions.InternalCall)]
    public void Foo()
    {

    }
}