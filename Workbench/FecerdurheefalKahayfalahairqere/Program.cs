// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

var span = MemoryMarshal.AsBytes("sdfsadf".AsSpan());

var foo = new Foo()
{
    F1 = 2
};

Span<Foo> span1 = MemoryMarshal.CreateSpan(ref foo,1);
span = MemoryMarshal.AsBytes(span1);


Span<byte> sp = MemoryMarshal.Cast<Foo, byte>(span1);
sp[0] = 10;

Console.WriteLine("Hello, World!");

struct Foo
{
    public int F1 { get; set; }
}