namespace TtfReader;

public static class TableDirectoryEntryArrayReader
{
    public static TableDirectoryEntry[] Read(BigEndianBinaryReader reader, in OffsetTable offsetTable)
    {
        var tableDirectoryEntryArray = new TableDirectoryEntry[offsetTable.NumTables];

        for (int i = 0; i < tableDirectoryEntryArray.Length; i++)
        {
            tableDirectoryEntryArray[i] = TableDirectoryEntry.Read(reader);
        }

        return tableDirectoryEntryArray;
    }
}