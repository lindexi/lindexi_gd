// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Runtime.CompilerServices;

var n = 10;
string t = string.Join(',', Enumerable.Range(0, 10000));
for (int i = 0; i < int.MaxValue; i++)
{
    Foo($"asd{1 + 2} n={n} {t}");
    //ArrayPool<int>.Shared.Rent(1024);
}

Console.WriteLine("Hello, World!");


void Foo(FooInterpolatedStringHandler handler)
{

}

[InterpolatedStringHandler]
ref struct FooInterpolatedStringHandler
{
    public FooInterpolatedStringHandler(int literalLength, int formattedCount)
    {
    }

    public void AppendLiteral(string s)
    {

    }

    public void AppendFormatted<T>(T t)
    {

    }

    public void AppendFormatted<T>(T t, string format) where T : IFormattable
    {

    }
}