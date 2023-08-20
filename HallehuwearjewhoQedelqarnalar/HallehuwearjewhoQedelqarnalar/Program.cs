using System.Runtime.InteropServices;
using System.Text;

namespace HallehuwearjewhoQedelqarnalar;

internal class Program
{
    static void Main(string[] args)
    {
        var foo1 = new Foo1()
        {
            A = 1,
            B = 2,
            C = 3,
        };
        var foo1Array = new Foo1[] { foo1 };

        var foo1ByteSpan = MemoryMarshal.AsBytes(foo1Array.AsSpan());

        Log(foo1ByteSpan); // 01 00 00 00 02 00 00 00 03 00 00 00

        foo1Array[0].C = 5;

        Log(foo1ByteSpan); // 01 00 00 00 02 00 00 00 05 00 00 00

        foo1ByteSpan[0] = 6;

        var foo1Span = MemoryMarshal.Cast<byte, Foo1>(foo1ByteSpan);
        Console.WriteLine(foo1Span[0].A); // 6
        Console.WriteLine(foo1Span[0].B); // 2
        Console.WriteLine(foo1Span[0].C); // 5

        var foo2Span = MemoryMarshal.Cast<Foo1,Foo2>(foo1Span);
        Console.WriteLine(foo2Span[0].A); // 6
        Console.WriteLine(foo2Span[0].B); // 2
        Console.WriteLine(foo2Span[0].C); // 5
    }

    private static void Log(Span<byte> byteSpan)
    {
        var stringBuilder = new StringBuilder();
        foreach (var b in byteSpan)
        {
            stringBuilder.Append(b.ToString("X2"));
            stringBuilder.Append(' ');
        }

        Console.WriteLine(stringBuilder.ToString());
    }
}

struct Foo1
{
    public int A { get; set; }
    public int B { get; set; }
    public int C { get; set; }
}

struct Foo2
{
    public int A { get; set; }
    public int B { get; set; }
    public int C { get; set; }
}