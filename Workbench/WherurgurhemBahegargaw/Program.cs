// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;

List<byte> buffer = [];

for (int i = 1; i < 6; i++)
{
    buffer.AddRange([(byte) (1 * i), 0, 0, 0, (byte) (2 * i), 0, 0, 0, (byte) (3 * i), 0, 0, 0]);
}

for (var i = 0; i < buffer.Count; i++)
{
    Console.WriteLine($"[{i}] - {buffer[i]:X2}");
}

Span<Foo1> foo1Span = MemoryMarshal.Cast<byte, Foo1>(buffer.ToArray().AsSpan());

foreach (var foo1 in foo1Span)
{
    Console.WriteLine($"foo1.A={foo1.A}");
    Console.WriteLine($"foo1.B={foo1.B}");
    Console.WriteLine($"foo1.C={foo1.C}");
}

Console.WriteLine("Hello, World!");

[StructLayout(LayoutKind.Sequential)]
struct Foo1
{
    public int A { get; set; }
    public int B { get; set; }
    public int C { get; set; }
}