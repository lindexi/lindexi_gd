// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

var fooStruct = new FooStruct()
{
    N1 = 1,
    N2 = 2,
    N3 = 3
};

Span<FooStruct> fooSpan = MemoryMarshal.CreateSpan(ref fooStruct, 1);

Span<byte> byteSpan = MemoryMarshal.Cast<FooStruct, byte>(fooSpan);

for (var i = 0; i < byteSpan.Length; i++)
{
    var t = byteSpan[i];
    Console.WriteLine($"[{i}] - {t:X2}");
}

byteSpan[0] = 10;
byteSpan[4] = 20;
byteSpan[8] = 30;

var foo2 = MemoryMarshal.Cast<byte, FooStruct>(byteSpan)[0];

// 此时 FooStruct 已经被修改，刚好和 foo2 相同

Console.WriteLine($"FooStruct.N1: {fooStruct.N1}");
Console.WriteLine($"foo2.N1: {foo2.N1}");
Console.WriteLine($"FooStruct.N2: {fooStruct.N2}");
Console.WriteLine($"foo2.N2: {foo2.N2}");
Console.WriteLine($"FooStruct.N3: {fooStruct.N3}");
Console.WriteLine($"foo2.N3: {foo2.N3}");

var buffer = new byte[] { 11, 0, 0, 0, 21, 0, 0, 0, 30, 0, 0, 0 };
var foo3 = MemoryMarshal.Cast<byte, FooStruct>(buffer.AsSpan())[0];

Console.WriteLine($"foo3.N1: {foo3.N1}");
Console.WriteLine($"foo3.N2: {foo3.N2}");

Console.WriteLine("Hello, World!");

[StructLayout(LayoutKind.Sequential)]
struct FooStruct
{
    public int N1 { get; set; }
    public int N2 { get; set; }
    public int N3 { get; set; }
}