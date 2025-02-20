// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;

var t = 1;
Foo(t);

Console.WriteLine(t);

void Foo(in int x)
{
    ref var refX = ref Unsafe.AsRef(in x);
    refX = 10;
}