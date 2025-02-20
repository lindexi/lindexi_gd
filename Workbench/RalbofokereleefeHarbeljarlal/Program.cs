// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;

var t = new F1(1, 2, 3);
Foo(t);

Console.WriteLine(t);

void Foo(in F1 x)
{
    ref var refX = ref Unsafe.AsRef(in x);
    refX = new F1(10, 20, 30);
}

readonly record struct F1(int N1, int N2, int N3);