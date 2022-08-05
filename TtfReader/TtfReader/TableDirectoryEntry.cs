namespace TtfReader;

public readonly record struct TableDirectoryEntry(string Tag, uint Checksum, uint Offset, uint Length)
{
    public static TableDirectoryEntry Read(BigEndianBinaryReader reader)
    {
        return new TableDirectoryEntry(reader.ReadAsciiString(4), reader.ReadUInt32(), reader.ReadUInt32(),
            reader.ReadUInt32());
    }
}