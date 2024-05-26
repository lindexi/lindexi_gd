// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

var byteSize = sizeof(byte);
var boolSize = sizeof(bool);

var foo = new Foo();
Console.WriteLine(foo.A);

foo.B = 100;
Console.WriteLine(foo.A);

Console.WriteLine("Hello, World!");

[StructLayout(LayoutKind.Explicit)]
struct Foo
{
    [FieldOffset(0)]
    public bool A;

    [FieldOffset(0)]
    public byte B;
}