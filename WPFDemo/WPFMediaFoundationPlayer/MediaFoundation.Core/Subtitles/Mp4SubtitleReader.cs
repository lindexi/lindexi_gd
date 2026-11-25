using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace MediaFoundation.Core;

internal static class Mp4SubtitleReader
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
    private static readonly Encoding Utf16Be = new UnicodeEncoding(true, false, true);
    private static readonly Encoding Utf16Le = new UnicodeEncoding(false, false, true);
    private const int MaximumEntries = 1_000_000;
    private const int MaximumTextBytes = 16 * 1024 * 1024;

    internal static List<EmbeddedSubtitleTrack> Read(Stream stream, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek) throw new ArgumentException(Message("Stream"), nameof(stream));
        return new Reader(stream, token).Read();
    }

    private static string Message(string reason) => reason switch
    {
        "Stream" => SR.Current.Mp4Stream,
        "Structure" => SR.Current.Mp4Structure,
        "Truncated" => SR.Current.Mp4Truncated,
        "Limit" => SR.Current.Mp4Limit,
        "Fragmented" => SR.Current.Mp4Fragmented,
        "Descriptions" => SR.Current.Mp4Descriptions,
        "Encrypted" => SR.Current.Mp4Encrypted,
        "Codec" => SR.Current.Mp4Codec,
        "Composition" => SR.Current.Mp4Composition,
        "DataReference" => SR.Current.Mp4DataReference,
        "Style" => SR.Current.Mp4Style,
        "Edit" => SR.Current.Mp4Edit,
        "Sample" => SR.Current.Mp4Sample,
        "Time" => SR.Current.Mp4Time,
        "Text" => SR.Current.Mp4Text,
        _ => throw new ArgumentOutOfRangeException(nameof(reason))
    };
    private readonly record struct Box(string Type, long Offset, long Start, long End);
    private readonly record struct Run(uint Count, uint Delta);
    private readonly record struct ChunkRun(uint First, uint Count, uint Description);
    private readonly record struct Edit(ulong Duration, long MediaTime, short IntegerRate, short FractionRate);

    private sealed class Reader(Stream stream, CancellationToken token)
    {
        private readonly List<Box> _media = [];
        private uint _movieScale;
        private bool _fragmented;

        private FormatException Error(Box box, string key = "Structure") => new(SR.Current.Mp4Error.ToString(box.Type, box.Offset, Message(key)));
        private void Need(Box box, long count)
        {
            token.ThrowIfCancellationRequested();
            if (count < 0 || stream.Position < box.Start || stream.Position > box.End || count > box.End - stream.Position) throw Error(box);
        }
        private byte[] Bytes(Box box, int count)
        {
            Need(box, count);
            byte[] bytes = new byte[count];
            try { stream.ReadExactly(bytes); }
            catch (IOException error) { throw new IOException(Error(box, "Truncated").Message, error); }
            return bytes;
        }
        private uint U32(Box box) => BinaryPrimitives.ReadUInt32BigEndian(Bytes(box, 4));
        private ulong U64(Box box) => BinaryPrimitives.ReadUInt64BigEndian(Bytes(box, 8));
        private ushort U16(Box box) => BinaryPrimitives.ReadUInt16BigEndian(Bytes(box, 2));
        private void Skip(Box box, long count) { Need(box, count); stream.Position += count; }
        private void At(Box box) { token.ThrowIfCancellationRequested(); stream.Position = box.Start; }
        private void Done(Box box) { if (stream.Position != box.End) throw Error(box); }
        private int Count(Box box, int width)
        {
            uint count = U32(box);
            if (count > MaximumEntries) throw Error(box, "Limit");
            Need(box, (long)count * width);
            return (int)count;
        }
        private byte Version(Box box)
        {
            At(box);
            uint full = U32(box);
            if ((full & 0xFFFFFF) != 0) throw Error(box);
            return (byte)(full >> 24);
        }
        private void VersionZero(Box box) { if (Version(box) != 0) throw Error(box); }
        private List<Box> Children(Box parent)
        {
            var result = new List<Box>();
            At(parent);
            while (stream.Position < parent.End)
            {
                token.ThrowIfCancellationRequested();
                long offset = stream.Position;
                uint size = U32(parent);
                string type = Encoding.Latin1.GetString(Bytes(parent, 4));
                var context = new Box(type, offset, stream.Position, parent.End);
                ulong length = size == 1 ? U64(context) : size == 0 ? (ulong)(parent.End - offset) : size;
                long header = stream.Position - offset;
                if (length < (ulong)header || length > (ulong)(parent.End - offset)) throw Error(context);
                var box = new Box(type, offset, stream.Position, offset + (long)length);
                if (result.Count == MaximumEntries) throw Error(box, "Limit");
                result.Add(box);
                stream.Position = box.End;
            }
            return result;
        }
        private Box? Optional(List<Box> boxes, string type)
        {
            Box? found = null;
            foreach (var box in boxes)
            {
                token.ThrowIfCancellationRequested();
                if (box.Type != type) continue;
                if (found != null) throw Error(box);
                found = box;
            }
            return found;
        }
        private Box Required(List<Box> boxes, string type, Box parent) => Optional(boxes, type) ?? throw Error(parent);
        private uint Scale(Box box)
        {
            byte version = Version(box);
            if (version > 1) throw Error(box);
            Skip(box, version == 1 ? 16 : 8);
            uint scale = U32(box);
            if (scale == 0) throw Error(box);
            return scale;
        }
        internal List<EmbeddedSubtitleTrack> Read()
        {
            token.ThrowIfCancellationRequested();
            var root = new Box("file", 0, 0, stream.Length);
            var boxes = Children(root);
            _fragmented = boxes.Any(b => b.Type == "moof");
            _media.AddRange(boxes.Where(b => b.Type == "mdat"));
            Box? movie = Optional(boxes, "moov");
            if (movie == null)
            {
                if (_fragmented) return [new EmbeddedSubtitleTrack("0", null, null, "MP4", null, Message("Fragmented"))];
                throw Error(root);
            }
            var children = Children(movie.Value);
            _fragmented |= Optional(children, "mvex") != null;
            _movieScale = Scale(Required(children, "mvhd", movie.Value));
            var tracks = new List<EmbeddedSubtitleTrack>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var box in children)
            {
                token.ThrowIfCancellationRequested();
                if (box.Type != "trak") continue;
                var track = Track(box);
                if (track == null) continue;
                if (!ids.Add(track.Id)) throw Error(box);
                tracks.Add(track);
            }
            if (_fragmented && tracks.Count == 0) tracks.Add(new EmbeddedSubtitleTrack("0", null, null, "MP4", null, Message("Fragmented")));
            return tracks;
        }
        private EmbeddedSubtitleTrack? Track(Box trak)
        {
            var track = Children(trak);
            Box mdia = Required(track, "mdia", trak);
            var media = Children(mdia);
            Box handler = Required(media, "hdlr", mdia);
            VersionZero(handler);
            Skip(handler, 4);
            string kind = Encoding.Latin1.GetString(Bytes(handler, 4));
            Skip(handler, 12);
            string? name = stream.Position == handler.End ? null : Text(handler, TextLength(handler)).TrimEnd('\0');
            Box minf = Required(media, "minf", mdia);
            var information = Children(minf);
            Box stbl = Required(information, "stbl", minf);
            var tables = Children(stbl);
            Box stsd = Required(tables, "stsd", stbl);
            VersionZero(stsd);
            uint descriptions = U32(stsd);
            var entries = Children(stsd with { Start = stream.Position });
            if (descriptions != entries.Count || entries.Count == 0) throw Error(stsd);
            string codec = string.Join(",", entries.Select(b => b.Type));
            if (kind is not ("text" or "sbtl" or "subt") && !entries.Any(b => b.Type is "tx3g" or "wvtt" or "stpp" or "enct" or "encs")) return null;

            Box tkhd = Required(track, "tkhd", trak);
            At(tkhd);
            uint tkFlags = U32(tkhd);
            uint tkVersion = tkFlags >> 24;
            if (tkVersion > 1) throw Error(tkhd);
            Skip(tkhd, tkVersion == 1 ? 16 : 8);
            uint id = U32(tkhd);
            if (id == 0) throw Error(tkhd);
            Box mdhd = Required(media, "mdhd", mdia);
            uint scale = Scale(mdhd);
            At(mdhd);
            byte mediaVersion = (byte)(U32(mdhd) >> 24);
            Skip(mdhd, mediaVersion == 1 ? 28 : 16);
            ushort packed = U16(mdhd);
            string? language = null;
            if (packed != 0)
            {
                int a = (packed >> 10) & 31, b = (packed >> 5) & 31, c = packed & 31;
                if ((packed & 0x8000) != 0 || a is < 1 or > 26 || b is < 1 or > 26 || c is < 1 or > 26) throw Error(mdhd);
                language = new string([(char)(a + 96), (char)(b + 96), (char)(c + 96)]);
            }
            string? reason = _fragmented ? Message("Fragmented") : null;
            if (entries.Count != 1) reason ??= Message("Descriptions");
            foreach (var entry in entries)
            {
                token.ThrowIfCancellationRequested();
                if (entry.Type is "enct" or "encs") reason ??= Message("Encrypted");
                else if (entry.Type is not ("tx3g" or "wvtt")) reason ??= Message("Codec");
                else reason ??= Description(entry);
            }
            Box? dinf = Optional(information, "dinf");
            if (dinf == null) reason ??= Message("DataReference");
            else
            {
                Box dref = Required(Children(dinf.Value), "dref", dinf.Value);
                VersionZero(dref);
                uint references = U32(dref);
                var locations = Children(dref with { Start = stream.Position });
                if (references != locations.Count) throw Error(dref);
                if (locations.Count != 1 || locations[0].Type != "url ") reason ??= Message("DataReference");
                else
                {
                    At(locations[0]);
                    if (U32(locations[0]) != 1 || stream.Position != locations[0].End) reason ??= Message("DataReference");
                }
            }
            if (Optional(tables, "ctts") != null) reason ??= Message("Composition");
            if (Optional(tables, "senc") != null || Optional(tables, "saiz") != null || Optional(tables, "saio") != null) reason ??= Message("Encrypted");
            decimal delay = 0, mediaStart = 0;
            decimal? editEnd = null;
            Box? edts = Optional(track, "edts");
            if (edts != null) reason ??= Edits(edts.Value, scale, out delay, out mediaStart, out editEnd);
            SubtitleDocument? document = null;
            if (reason == null)
            {
                var cues = Samples(tables, stbl, codec, scale, delay, mediaStart, editEnd, out reason);
                if (reason == null) document = new SubtitleDocument(cues);
            }
            return new EmbeddedSubtitleTrack(id.ToString(CultureInfo.InvariantCulture), string.IsNullOrWhiteSpace(name) ? null : name, language, codec, document, reason);
        }
        private string? Description(Box entry)
        {
            At(entry);
            Skip(entry, 6);
            if (U16(entry) != 1) return Message("DataReference");
            bool styled = false;
            if (entry.Type == "tx3g")
            {
                byte[] defaults = Bytes(entry, 30);
                // Only the unpositioned, white, plain default font is faithfully representable.
                styled = defaults.AsSpan(0, 22).IndexOfAnyExcept((byte)0) >= 0 ||
                    BinaryPrimitives.ReadUInt16BigEndian(defaults.AsSpan(22)) != 1 || defaults[24] != 0 || defaults[25] != 12 ||
                    BinaryPrimitives.ReadUInt32BigEndian(defaults.AsSpan(26)) != uint.MaxValue;
            }
            foreach (var child in Children(entry with { Start = stream.Position }))
            {
                token.ThrowIfCancellationRequested();
                if (child.Type == "sinf") return Message("Encrypted");
                At(child);
                if (entry.Type == "tx3g" && child.Type == "ftab")
                {
                    ushort count = U16(child);
                    if (count != 1) { styled = true; continue; }
                    ushort font = U16(child);
                    int length = Bytes(child, 1)[0];
                    string family = Text(child, length);
                    Done(child);
                    if (font != 1 || family != "Arial") styled = true;
                }
                else if (entry.Type == "wvtt" && child.Type == "vttC")
                {
                    string config = Text(child, TextLength(child)).Trim('\uFEFF', '\r', '\n', ' ', '\t');
                    if (config != "WEBVTT") styled = true;
                }
                else if (entry.Type == "wvtt" && child.Type == "vlab") { _ = Text(child, TextLength(child)); }
                else styled = true;
            }
            return styled ? Message("Style") : null;
        }
        private string? Edits(Box edts, uint scale, out decimal delay, out decimal mediaStart, out decimal? end)
        {
            delay = mediaStart = 0;
            end = null;
            var children = Children(edts);
            Box? editBox = Optional(children, "elst");
            if (editBox == null) return children.Count == 0 ? null : Message("Edit");
            Box box = editBox.Value;
            byte version = Version(box);
            if (version > 1) return Message("Edit");
            int count = Count(box, version == 1 ? 20 : 12);
            var edits = new List<Edit>();
            for (int i = 0; i < count; i++)
            {
                token.ThrowIfCancellationRequested();
                ulong duration = version == 1 ? U64(box) : U32(box);
                long time = version == 1 ? unchecked((long)U64(box)) : unchecked((int)U32(box));
                edits.Add(new Edit(duration, time, unchecked((short)U16(box)), unchecked((short)U16(box))));
            }
            Done(box);
            if (count == 0) return null;
            int index = 0;
            if (edits[0].MediaTime == -1)
            {
                delay = (decimal)edits[0].Duration / _movieScale;
                index++;
            }
            if (index != count - 1 || edits.Any(e => e.IntegerRate != 1 || e.FractionRate != 0) || edits[index].MediaTime < 0 || edits[index].Duration == 0) return Message("Edit");
            mediaStart = (decimal)edits[index].MediaTime / scale;
            end = delay + (decimal)edits[index].Duration / _movieScale;
            return null;
        }
        private List<SubtitleCue> Samples(List<Box> tables, Box stbl, string codec, uint scale, decimal delay, decimal mediaStart, decimal? editEnd, out string? reason)
        {
            reason = null;
            Box stsz = Required(tables, "stsz", stbl);
            VersionZero(stsz);
            uint fixedSize = U32(stsz);
            int count = Count(stsz, fixedSize == 0 ? 4 : 0);
            var sizes = new uint[count];
            for (int i = 0; i < count; i++) { token.ThrowIfCancellationRequested(); sizes[i] = fixedSize == 0 ? U32(stsz) : fixedSize; }
            Done(stsz);
            Box stts = Required(tables, "stts", stbl);
            VersionZero(stts);
            int timeCount = Count(stts, 8);
            var times = new Run[timeCount];
            ulong total = 0;
            for (int i = 0; i < timeCount; i++)
            {
                uint n = U32(stts), delta = U32(stts);
                if (n == 0 || delta == 0) throw Error(stts);
                total += n;
                times[i] = new Run(n, delta);
            }
            Done(stts);
            if (total != (ulong)count) throw Error(stts);
            Box stsc = Required(tables, "stsc", stbl);
            VersionZero(stsc);
            int mapCount = Count(stsc, 12);
            var map = new ChunkRun[mapCount];
            for (int i = 0; i < mapCount; i++)
            {
                uint first = U32(stsc), n = U32(stsc), desc = U32(stsc);
                if ((i == 0 ? first != 1 : first <= map[i - 1].First) || n == 0 || desc != 1) throw Error(stsc);
                map[i] = new ChunkRun(first, n, desc);
            }
            Done(stsc);
            Box? stco = Optional(tables, "stco"), co64 = Optional(tables, "co64");
            if ((stco == null) == (co64 == null)) throw Error(stbl);
            Box offsetsBox = stco ?? co64!.Value;
            VersionZero(offsetsBox);
            int chunkCount = Count(offsetsBox, stco == null ? 8 : 4);
            var offsets = new ulong[chunkCount];
            for (int i = 0; i < chunkCount; i++) offsets[i] = stco == null ? U64(offsetsBox) : U32(offsetsBox);
            Done(offsetsBox);
            if ((count == 0 && (chunkCount != 0 || mapCount != 0)) || (count != 0 && (mapCount == 0 || chunkCount == 0)) || (mapCount != 0 && map[^1].First > chunkCount)) throw Error(stsc);
            var cues = new List<SubtitleCue>();
            int sample = 0, mapping = 0, timing = 0;
            uint used = 0;
            decimal decodeTime = 0;
            var ranges = new List<Box>(count);
            for (int chunk = 0; chunk < chunkCount; chunk++)
            {
                token.ThrowIfCancellationRequested();
                while (mapping + 1 < map.Length && map[mapping + 1].First <= chunk + 1) mapping++;
                if (map[mapping].Count > count - sample) throw Error(stsc);
                ulong offset = offsets[chunk];
                for (uint j = 0; j < map[mapping].Count; j++)
                {
                    token.ThrowIfCancellationRequested();
                    uint size = sizes[sample++];
                    if (offset > (ulong)stream.Length || size > (ulong)stream.Length - offset) throw Error(offsetsBox);
                    long start = (long)offset, finish = start + size;
                    var frame = new Box(codec, start, start, finish);
                    if (!_media.Any(m => start >= m.Start && finish <= m.End)) throw Error(frame, "Sample");
                    ranges.Add(frame);
                    if (size > MaximumTextBytes) throw Error(frame, "Limit");
                    decimal from = delay + decodeTime / scale - mediaStart;
                    decodeTime += times[timing].Delta;
                    decimal to = delay + decodeTime / scale - mediaStart;
                    if (++used == times[timing].Count) { timing++; used = 0; }
                    string? failure;
                    var texts = codec == "tx3g" ? Tx3g(frame, out failure) : Wvtt(frame, out failure);
                    reason ??= failure;
                    from = Math.Max(delay, from);
                    if (editEnd is decimal limit) to = Math.Min(to, limit);
                    if (from < to && reason == null)
                    {
                        TimeSpan begin = Time(from, frame), end = Time(to, frame);
                        if (end > begin)
                            foreach (string text in texts)
                            {
                                token.ThrowIfCancellationRequested();
                                if (!string.IsNullOrWhiteSpace(text)) cues.Add(new SubtitleCue(begin, end, text));
                            }
                    }
                    offset += size;
                }
            }
            if (sample != count) throw Error(stsc);
            long previousEnd = -1;
            foreach (var range in ranges.OrderBy(r => r.Start))
            {
                token.ThrowIfCancellationRequested();
                if (range.Start < previousEnd) throw Error(range, "Sample");
                previousEnd = range.End;
            }
            return cues;
        }
        private TimeSpan Time(decimal seconds, Box box)
        {
            decimal ticks = decimal.Truncate(seconds * TimeSpan.TicksPerSecond);
            if (ticks < 0 || ticks > long.MaxValue) throw Error(box, "Time");
            return TimeSpan.FromTicks((long)ticks);
        }
        private int TextLength(Box box)
        {
            long length = box.End - stream.Position;
            if (length > MaximumTextBytes) throw Error(box, "Limit");
            return (int)length;
        }
        private string Text(Box box, int count, bool bom = false)
        {
            byte[] bytes = Bytes(box, count);
            try
            {
                string text;
                if (bom && bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) text = Utf16Be.GetString(bytes, 2, bytes.Length - 2);
                else if (bom && bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) text = Utf16Le.GetString(bytes, 2, bytes.Length - 2);
                else text = Utf8.GetString(bytes);
                return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
            }
            catch (DecoderFallbackException error) { throw new FormatException(Error(box, "Text").Message, error); }
        }
        private List<string> Tx3g(Box frame, out string? reason)
        {
            reason = null;
            At(frame);
            int length = U16(frame);
            string text = Text(frame, length, true);
            var extensions = Children(frame with { Start = stream.Position });
            if (extensions.Count != 0) reason = Message("Style");
            return [text];
        }
        private List<string> Wvtt(Box frame, out string? reason)
        {
            reason = null;
            var texts = new List<string>();
            foreach (var box in Children(frame))
            {
                token.ThrowIfCancellationRequested();
                if (box.Type == "vtte") { if (box.Start != box.End) throw Error(box); continue; }
                if (box.Type != "vttc") { reason ??= Message("Style"); continue; }
                bool payload = false;
                foreach (var child in Children(box))
                {
                    token.ThrowIfCancellationRequested();
                    At(child);
                    if (child.Type == "payl")
                    {
                        if (payload) throw Error(child);
                        payload = true;
                        string text = Text(child, TextLength(child));
                        if (text.Contains('<') || text.Contains('&')) reason ??= Message("Style");
                        texts.Add(text);
                    }
                    else if (child.Type == "iden") { _ = Text(child, TextLength(child)); }
                    else if (child.Type == "sttg") { if (!string.IsNullOrWhiteSpace(Text(child, TextLength(child)))) reason ??= Message("Style"); }
                    else reason ??= Message("Style");
                }
                if (!payload) throw Error(box);
            }
            return texts;
        }
    }
}
