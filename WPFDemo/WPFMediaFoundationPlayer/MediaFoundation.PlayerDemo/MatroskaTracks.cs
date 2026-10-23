using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace MediaFoundation.PlayerDemo;

internal static class MatroskaTracks
{
    internal static string Read(string path, CancellationToken token)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Read(file, token);
    }

    internal static string Read(Stream file, CancellationToken token)
    {
        var reader = new Reader(file, token);
        var header = reader.Element(file.Length);
        if (header.Id != 0x1A45DFA3 || header.Unknown) throw new InvalidDataException();
        file.Position = header.End;
        var segment = reader.Element(file.Length);
        if (segment.Id != 0x18538067) throw new InvalidDataException();
        while (file.Position < segment.End)
        {
            var element = reader.Element(segment.End);
            if (element.Id == 0x1654AE6B)
            {
                if (element.Unknown || element.End - file.Position > 4 * 1024 * 1024) throw new InvalidDataException();
                var report = new StringBuilder("EBML Tracks (container metadata; not a playback result)\n");
                int tracks = 0;
                while (file.Position < element.End)
                {
                    var entry = reader.Element(element.End);
                    if (entry.Unknown) throw new InvalidDataException();
                    if (entry.Id == 0xAE)
                    {
                        if (++tracks > 256) throw new InvalidDataException();
                        report.AppendLine(reader.Track(entry.End));
                    }
                    file.Position = entry.End;
                }
                if (tracks == 0) throw new InvalidDataException();
                return report.ToString();
            }
            // Random-access skip: no Cluster payload is read, even for multi-GB files.
            if (element.Unknown) throw new InvalidDataException();
            file.Position = element.End;
        }
        throw new InvalidDataException();
    }

    private readonly record struct ElementInfo(ulong Id, long End, bool Unknown);

    private sealed class Reader(Stream file, CancellationToken token)
    {
        private int headers;
        private int bytesRead;

        private byte Byte()
        {
            token.ThrowIfCancellationRequested();
            if (++bytesRead > 4 * 1024 * 1024) throw new InvalidDataException();
            int value = file.ReadByte();
            if (value < 0) throw new EndOfStreamException();
            return (byte)value;
        }

        private ulong Vint(bool id, out bool unknown)
        {
            byte first = Byte();
            int length = 1, mask = 0x80;
            while (length <= 8 && (first & mask) == 0) { length++; mask >>= 1; }
            if (length > (id ? 4 : 8)) throw new InvalidDataException();
            ulong value = id ? first : (ulong)(first & (mask - 1));
            for (int i = 1; i < length; i++) value = (value << 8) | Byte();
            unknown = !id && value == ((1UL << (7 * length)) - 1);
            return value;
        }

        internal ElementInfo Element(long parentEnd)
        {
            if (++headers > 100000 || file.Position >= parentEnd) throw new InvalidDataException();
            ulong id = Vint(true, out _);
            ulong size = Vint(false, out bool unknown);
            if (file.Position > parentEnd || (!unknown && size > (ulong)(parentEnd - file.Position))) throw new InvalidDataException();
            return new(id, unknown ? parentEnd : checked(file.Position + (long)size), unknown);
        }

        private byte[] Data(long end, int max)
        {
            long length = end - file.Position;
            if (length < 0 || length > max) throw new InvalidDataException();
            var data = new byte[(int)length];
            for (int i = 0; i < data.Length; i++) data[i] = Byte();
            return data;
        }

        private ulong Number(long end)
        {
            var data = Data(end, 8);
            if (data.Length == 0) throw new InvalidDataException();
            ulong value = 0;
            foreach (byte b in data) value = (value << 8) | b;
            return value;
        }

        internal string Track(long end)
        {
            var values = new Dictionary<string, string> { ["language"] = "eng" };
            Fields(end, values, 0);
            if (values.GetValueOrDefault("type") == "2")
            {
                values.TryAdd("channels", "1");
                values.TryAdd("sampleRate", "8000");
            }
            return "EBML " + string.Join(" ", values.Select(pair => $"{pair.Key}={pair.Value}"));
        }

        private void Fields(long end, Dictionary<string, string> values, int depth)
        {
            if (depth > 1) throw new InvalidDataException();
            while (file.Position < end)
            {
                var field = Element(end);
                if (field.Unknown) throw new InvalidDataException();
                string? key = field.Id switch
                {
                    0xD7 => "track", 0x83 => "type", 0xB9 => "enabled", 0x88 => "default",
                    0xB0 => "width", 0xBA => "height", 0x9F => "channels", 0x6264 => "bitDepth", _ => null
                };
                if (key != null) values[key] = Number(field.End).ToString(System.Globalization.CultureInfo.InvariantCulture);
                else if (field.Id is 0x86 or 0x22B59C or 0x22B59D)
                {
                    string text = new UTF8Encoding(false, true).GetString(Data(field.End, 1024));
                    values[field.Id == 0x86 ? "codec" : field.Id == 0x22B59D ? "languageIETF" : "language"] = string.Concat(text.Select(c => char.IsControl(c) || char.IsWhiteSpace(c) ? '_' : c));
                }
                else if (field.Id == 0x63A2)
                {
                    long size = field.End - file.Position;
                    var prefix = Data(file.Position + Math.Min(size, 32), 32);
                    values["codecPrivateBytes"] = size.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    values["codecPrivatePrefix"] = Convert.ToHexString(prefix);
                    if (prefix.Length >= 4 && values.GetValueOrDefault("codec") == "V_MPEG4/ISO/AVC")
                    {
                        values["avcProfile"] = prefix[1].ToString();
                        values["avcLevel"] = prefix[3].ToString();
                    }
                    if (prefix.Length >= 19 && prefix[0] == 1 && values.GetValueOrDefault("codec") == "V_MPEGH/ISO/HEVC")
                    {
                        values["hevcProfileIdc"] = (prefix[1] & 31).ToString();
                        values["hevcLevelIdc"] = prefix[12].ToString();
                        values["hevcLumaBitDepth"] = (8 + (prefix[17] & 7)).ToString();
                        values["hevcChromaBitDepth"] = (8 + (prefix[18] & 7)).ToString();
                    }
                }
                else if (field.Id is 0xB5 or 0x78B5)
                {
                    byte[] data = Data(field.End, 8);
                    double value = data.Length switch
                    {
                        4 => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(data)),
                        8 => BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(data)),
                        _ => throw new InvalidDataException()
                    };
                    if (!double.IsFinite(value) || value <= 0) throw new InvalidDataException();
                    values[field.Id == 0xB5 ? "sampleRate" : "outputSampleRate"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (field.Id is 0xE0 or 0xE1) Fields(field.End, values, depth + 1);
                file.Position = field.End;
            }
        }
    }
}
