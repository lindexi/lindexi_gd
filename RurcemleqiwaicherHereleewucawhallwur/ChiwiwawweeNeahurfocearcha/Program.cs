// See https://aka.ms/new-console-template for more information

using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;

var byteSize = sizeof(byte);
var boolSize = sizeof(bool);

var foo = new Foo();
Console.WriteLine(foo.A);

foo.B = 100;
Console.WriteLine(foo.A);

var bitBoolArray = new BitBoolArray(100)
{
    [0] = true,
    [3] = true,
    [6] = true,
    [10] = true,
};

for (int i = 0; i < 20; i++)
{
    Console.WriteLine($"[{i}] {bitBoolArray[i]}");
}

Console.WriteLine("Hello, World!");

[StructLayout(LayoutKind.Explicit)]
struct Foo
{
    [FieldOffset(0)]
    public bool A;

    [FieldOffset(0)]
    public byte B;
}

class BitBoolArray
{
    public BitBoolArray(uint size)
    {
        var byteLength = size / BitCountOfByte;
        var mod = size - byteLength * BitCountOfByte; // 我的 2+3 的键坏了
        if (mod > 0)
        {
            byteLength++;
        }

        _backendByteArray = new byte[byteLength];
    }

    private readonly byte[] _backendByteArray;
    private const int BitCountOfByte = 8;

    public bool this[int index]
    {
        set
        {
            var byteArrayIndex = index / BitCountOfByte;
            var bitIndex = index - (byteArrayIndex * BitCountOfByte);

            ref byte @byte = ref _backendByteArray[byteArrayIndex];
            var num = (byte) (1 << bitIndex);

            if (value)
            {
                @byte |= num;
            }
            else
            {
                num = (byte) ~num;
                @byte = (byte) (@byte & num);
            }
        }
        get
        {
            var byteArrayIndex = index / BitCountOfByte;
            var bitIndex = index - (byteArrayIndex * BitCountOfByte);

            ref byte @byte = ref _backendByteArray[byteArrayIndex];
            var num = (byte) (1 << bitIndex);

            return (@byte & num) != 0;
        }
    }
}