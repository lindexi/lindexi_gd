// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;

short t = 0x10_10;
var foo = Unsafe.As<short, Foo>(ref t);
Console.WriteLine(foo.F1);

//if (foo.F1)
//{
//    Console.WriteLine($"if (foo.F1)");
//}

if (foo.F1 == true)
{
    Console.WriteLine($"if (foo.F1 == true)");
}

if (true == foo.F1)
{
    Console.WriteLine($"if (true == foo.F1)");
}

var t1 = true;

if (foo.F1 == t1)
{
    Console.WriteLine($"if (foo.F1 == t1)");
}

Console.WriteLine("Hello, World!");

struct Foo
{
    public bool? F1 { get; }
}