// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

var foo = new Foo()
{
    F1 = 2
};

Span<Foo> span1 = MemoryMarshal.CreateSpan(ref foo, 1);

Span<byte> sp = MemoryMarshal.Cast<Foo, byte>(span1);
sp[0] = 10;

sp[4] = 20; // F2

sp[5] = 30; // F3

sp[6] = 5;  // Fx


Console.WriteLine("Hello, World!");

[StructLayout(LayoutKind.Sequential)]
struct Foo
{
    public int F1 { get; set; }

    public byte F2 { get; set; }
    public byte F3 { get; set; }
    public byte Fx { get; set; }
}