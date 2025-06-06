// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;

byte t = 2;
var foo = Unsafe.As<byte, bool>(ref t);
Console.WriteLine(foo);

if (foo)
{
    Console.WriteLine($"if (foo.F1)");
}

if (foo == true)
{
    Console.WriteLine($"if (foo.F1 == true)");
}
else
{
    Console.WriteLine($"if (foo.F1 != true)");
}

var t1 = true;
if (foo == t1)
{
    Console.WriteLine($"if (foo.F1 == t1)");
}
else
{
    Console.WriteLine($"if (foo.F1 != t1)");
}

Console.WriteLine("Hello, World!");