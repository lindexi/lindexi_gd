namespace TtfReader;

record TtfInfo(TableDirectoryEntry[] TableDirectoryEntryArray, NameTable NameTable)
{
    public static TtfInfo Read(Stream stream)
    {
        using var bigEndianBinaryReader = new BigEndianBinaryReader(stream);
        var offsetTable = OffsetTable.Read(bigEndianBinaryReader);
        var tableDirectoryEntryArray = TableDirectoryEntryArrayReader.Read(bigEndianBinaryReader, offsetTable);

        // [Glyph Properties Table - TrueType Reference Manual - Apple Developer](https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6prop.html )
        var nameDirTableEntry = tableDirectoryEntryArray.First(t => t.Tag == "name");
        stream.Position = nameDirTableEntry.Offset;

        var nameTable = NameTable.Read(bigEndianBinaryReader);

        return new TtfInfo(tableDirectoryEntryArray, nameTable);
    }
}