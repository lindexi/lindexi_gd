// See https://aka.ms/new-console-template for more information

F1<Foo>();

Console.WriteLine("Hello, World!");


void F1<T>()
{
    var type = typeof(T);
    GC.KeepAlive(type);
}

class Foo
{
    static Foo()
    {
        Console.WriteLine("静态构造");
    }
}