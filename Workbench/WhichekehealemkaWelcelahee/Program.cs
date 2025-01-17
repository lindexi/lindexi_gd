// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;

var fx = new Fx();
fx.Foo($"asd{GetTestInfo()}");

Console.WriteLine("Hello, World!");

int GetTestInfo()
{
    Console.WriteLine($"GetTestInfo");
    return 2;
}

class Fx
{
    public void Foo([InterpolatedStringHandlerArgument("")] FooInterpolatedStringHandler fooInterpolatedStringHandler)
    {
    }

    public bool Enable { set; get; }

    [InterpolatedStringHandler]
    public ref struct FooInterpolatedStringHandler
    {
        public FooInterpolatedStringHandler(int literalLength, int formattedCount, Fx fx, out bool isEnable)
        {
            isEnable = fx.Enable;
        }

        public void AppendLiteral(string s)
        {
        }

        public void AppendFormatted<T>(T value)
        {
        }
    }
}