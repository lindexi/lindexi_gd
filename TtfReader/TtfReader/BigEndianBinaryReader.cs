using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace TtfReader;

public class BigEndianBinaryReader : BinaryReader
{
    public BigEndianBinaryReader(Stream stream) : base(stream, Encoding.UTF8, leaveOpen: true)
    {
        Buffer = ArrayPool<byte>.Shared.Rent(sizeof(ulong));
    }

    private byte[] Buffer { get; }
    public Stream Stream => BaseStream;

    public string ReadAsciiString(int charCount)
    {
        var buffer = ArrayPool<char>.Shared.Rent(charCount);
        for (int i = 0; i < charCount; i++)
        {
            buffer[i] = (char)(byte)Stream.ReadByte();
        }
        ArrayPool<char>.Shared.Return(buffer);
        return new string(buffer, 0, charCount);
    }

    public override short ReadInt16() => BinaryPrimitives.ReadInt16BigEndian(Read(sizeof(short)));

    public override int ReadInt32() => BinaryPrimitives.ReadInt32BigEndian(Read(sizeof(int)));

    public override long ReadInt64() => BinaryPrimitives.ReadInt64BigEndian(Read(sizeof(long)));

    public override ushort ReadUInt16() => BinaryPrimitives.ReadUInt16BigEndian(Read(sizeof(ushort)));

    public override uint ReadUInt32() => BinaryPrimitives.ReadUInt32BigEndian(Read(sizeof(uint)));

    public override ulong ReadUInt64() => BinaryPrimitives.ReadUInt64BigEndian(Read(sizeof(ulong)));

    private Span<byte> Read(int count)
    {
        var buffer = Buffer.AsSpan(0, count);
        var readCount = Stream.Read(buffer);
        if (readCount != count)
        {
            throw new EndOfStreamException();
        }

        return buffer;
    }

    protected override void Dispose(bool disposing)
    {
        ArrayPool<byte>.Shared.Return(Buffer);
        base.Dispose(disposing);
    }
}