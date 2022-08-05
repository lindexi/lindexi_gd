// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Runtime.InteropServices;

var file = @"C:\windows\fonts\simhei.ttf";

using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);

TtfInfo.Read(fileStream);

Console.WriteLine("Hello, World!");


class TtfInfo
{
    public static unsafe void Read(Stream stream)
    {
        var originBuffer = ArrayPool<byte>.Shared.Rent(1024);
        var i = sizeof(OffsetTable);
        Span<byte> buffer = originBuffer.AsSpan(0,i);
        stream.Read(originBuffer,0,i);

        fixed (byte* ptr = originBuffer)
        {
            var offsetTable = Marshal.PtrToStructure<OffsetTable>(new IntPtr(ptr));
        }
        //stream.Read()
    }
}

public readonly struct Version
{
    public ushort Major { get; }
    public ushort Minor { get; }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct OffsetTable
{
    public Version SfntVersion { get; }
    public ushort NumTables { get; }
    public ushort SearchRange { get; }
    public ushort EntrySelector { get; }
    public ushort RangeShift { get; }
}