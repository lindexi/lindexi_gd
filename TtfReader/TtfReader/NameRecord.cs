namespace TtfReader;

public readonly record struct NameRecord(PlatformIdentifier PlatformId, ushort PlatformSpecificId, ushort LanguageId,
    NameIdentifier NameId, ushort Length, ushort Offset, string Value)
{
    public static NameRecord Read(BigEndianBinaryReader reader)
    {
        var platformId = (PlatformIdentifier) reader.ReadUInt16();
        var platformSpecificId = reader.ReadUInt16();
        var languageId = reader.ReadUInt16();
        var nameId = (NameIdentifier) reader.ReadUInt16();
        var length = reader.ReadUInt16();
        var offset = reader.ReadUInt16();

        // 这里的 Value 是在不连续的空间，推荐是先连续读取，然后再逐个 Value 获取
        // 先设置 Value 为 string.Empty 后续再读取。因为 Value 不是一个连续的值，需要根据 Offset 的内容读取
        return new NameRecord(platformId, platformSpecificId, languageId, nameId, length, offset, string.Empty);
    }
}