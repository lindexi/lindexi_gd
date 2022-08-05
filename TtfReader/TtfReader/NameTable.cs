using System.Buffers;
using System.Text;

namespace TtfReader;

public readonly record struct NameTable(ushort Format, ushort Count, ushort StringOffset, NameRecord[] NameRecords)
{
    public static NameTable Read(BigEndianBinaryReader reader)
    {
        var format = reader.ReadUInt16();
        var count = reader.ReadUInt16();
        var stringOffset = reader.ReadUInt16();

        var nameRecords = new NameRecord[count];
        for (int i = 0; i < count; i++)
        {
            nameRecords[i] = NameRecord.Read(reader);
        }

        // 连续的空间存放 NameRecord 对象，在 NameRecord 里面对应的字符串内容，是需要根据内容获取，放在不连续的空间
        for (int i = 0; i < count; i++)
        {
            var nameRecord = nameRecords[i];

            var buffer = ArrayPool<byte>.Shared.Rent(nameRecord.Length);

            // 这里的 Offset 是相对读取 NameRecord 集合完成的
            var currentPosition = reader.Stream.Position;
            reader.Stream.Seek(nameRecord.Offset, SeekOrigin.Current);
            var readCount = reader.Read(buffer, 0, nameRecord.Length);
            reader.Stream.Position = currentPosition;

            if (readCount != nameRecord.Length)
            {
                throw new EndOfStreamException();
            }

            switch (nameRecord.PlatformId)
            {
                case PlatformIdentifier.Unicode:
                case PlatformIdentifier.Microsoft:
                {
                    var value = Encoding.BigEndianUnicode.GetString(buffer, 0, nameRecord.Length);
                    nameRecord = nameRecord with { Value = value };
                    break;
                }
                case PlatformIdentifier.Macintosh:
                {
                    // Copy From https://github.com/parzivail/TtfLoader
                    if (nameRecord.PlatformSpecificId == 0)
                    {
                        var value = Encoding.ASCII.GetString(buffer, 0, nameRecord.Length);
                        nameRecord = nameRecord with { Value = value };
                    }

                    break;
                }
                case PlatformIdentifier.Reserved:
                    // 理论上不会进入
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ArrayPool<byte>.Shared.Return(buffer);
            nameRecords[i] = nameRecord;
        }

        return new NameTable(format, count, stringOffset, nameRecords);
    }
}