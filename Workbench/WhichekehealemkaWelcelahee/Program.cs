// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;

var fx = new Fx();
fx.Foo($"asd");

Console.WriteLine("Hello, World!");


class Fx
{
    public void Foo([InterpolatedStringHandlerArgument("")]M m)
    {

    }

    public bool Enable { set; get; }

    [InterpolatedStringHandler]
    public ref struct M
    {
        public M(int literalLength, int formattedCount, Fx fx)
        {
        }

        public void AppendLiteral(string s)
        {
        }
    }
}