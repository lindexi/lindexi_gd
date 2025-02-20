// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;

var t = 10;
Foo(t);

Console.WriteLine(t);

void Foo<T>(in T x) where T : ISpanParsable<T>
{
    ref var refX = ref Unsafe.AsRef(in x);
    //refX = new F1(10, 20, 30);
    refX = T.Parse("2", null);
}

readonly record struct F1(int N1, int N2, int N3);