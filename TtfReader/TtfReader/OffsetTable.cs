namespace TtfReader;

public readonly record struct OffsetTable(Version SfntVersion, ushort NumTables, ushort SearchRange,
    ushort EntrySelector, ushort RangeShift)
{
    public static OffsetTable Read(BinaryReader reader)
    {
        var sfntVersion = Version.Read(reader);

        // 版本是以下三个之一
        // 0x00010000 对应 1.0 版本
        // $"{(char) 0x74}{(char) 0x74}{(char) 0x63}{(char) 0x66}" = "ttcf" 版本是 ttcf 字符串
        // $"{(char) 0x74}{(char) 0x72}{(char) 0x75}{(char) 0x65}" = "true" 版本是 true 字符串
        if
        (
            (sfntVersion.Major == 0x0001 && sfntVersion.Minor == 0x0000)
            || (sfntVersion.Major == 0x7474 && sfntVersion.Minor == 0x6366)
            || (sfntVersion.Major == 0x7472 && sfntVersion.Minor == 0x7565)
        )
        {
            return new OffsetTable(sfntVersion, reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(),
                reader.ReadUInt16());
        }
        else
        {
            // 这不是一个 TTF 文件
            throw new ArgumentException();
        }
    }
}