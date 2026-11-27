using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace MediaFoundation.Core;

internal static class MatroskaSubtitleReader
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    internal static List<EmbeddedSubtitleTrack> Read(Stream stream, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek) throw new ArgumentException(Message("Stream"), nameof(stream));
        return new Reader(stream, token).Read();
    }

    private static string Message(string reason) => reason switch
    {
        "Stream" => SR.Current.MatroskaStream,
        "Structure" => SR.Current.MatroskaStructure,
        "Truncated" => SR.Current.MatroskaTruncated,
        "UnknownLength" => SR.Current.MatroskaUnknownLength,
        "Segments" => SR.Current.MatroskaSegments,
        "TextSize" => SR.Current.MatroskaTextSize,
        "Utf8" => SR.Current.MatroskaUtf8,
        "Timing" => SR.Current.MatroskaTiming,
        "TrackTiming" => SR.Current.MatroskaTrackTiming,
        "Delay" => SR.Current.MatroskaDelay,
        "Encoding" => SR.Current.MatroskaEncoding,
        "Private" => SR.Current.MatroskaPrivate,
        "Codec" => SR.Current.MatroskaCodec,
        "Lacing" => SR.Current.MatroskaLacing,
        "BlockFeatures" => SR.Current.MatroskaBlockFeatures,
        "MissingEnd" => SR.Current.MatroskaMissingEnd,
        "Resolution" => SR.Current.MatroskaResolution,
        _ => throw new ArgumentOutOfRangeException(nameof(reason))
    };

    private readonly record struct Element(ulong Id, long Offset, long Start, long End, bool Unknown);
    private sealed class Track
    {
        internal ulong Number;
        internal ulong Type;
        internal string Codec = "";
        internal string? Name;
        internal string? Language;
        internal string? IetfLanguage;
        internal ulong DefaultDuration;
        internal string? Reason;
        internal readonly List<Frame> Frames = [];
    }
    private readonly record struct Frame(decimal Start, decimal? Duration, long Offset, long Size);

    private sealed class Reader(Stream stream, CancellationToken token)
    {
        private readonly Dictionary<ulong, Track> _tracks = [];
        private ulong _scale = 1_000_000;
        private decimal? _duration;
        private long _segmentStart;
        private long _segmentEnd;

        private FormatException Error(long offset, string key = "Structure") => new(SR.Current.MatroskaError.ToString(offset, Message(key)));
        private void Fail(Track track, string key)
        {
            track.Reason ??= Message(key);
            track.Frames.Clear();
        }
        private int Byte(long end)
        {
            token.ThrowIfCancellationRequested();
            if (stream.Position >= end) throw Error(stream.Position);
            int value = stream.ReadByte();
            if (value < 0) throw Error(stream.Position, "Truncated");
            return value;
        }
        private ulong Vint(long end, bool id, out int length, out bool unknown)
        {
            long offset = stream.Position;
            int first = Byte(end);
            int mask = 0x80;
            length = 1;
            while ((first & mask) == 0 && length <= 8) { mask >>= 1; length++; }
            if (length > (id ? 4 : 8)) throw Error(offset);
            ulong value = (ulong)(id ? first : first & (mask - 1));
            for (int i = 1; i < length; i++) value = (value << 8) | (uint)Byte(end);
            unknown = !id && value == ((1UL << (7 * length)) - 1);
            return value;
        }
        private Element Header(long end)
        {
            token.ThrowIfCancellationRequested();
            long offset = stream.Position;
            ulong id = Vint(end, true, out _, out _);
            ulong size = Vint(end, false, out _, out bool unknown);
            long start = stream.Position;
            if (!unknown && size > (ulong)(end - start)) throw Error(offset);
            return new Element(id, offset, start, unknown ? end : start + (long)size, unknown);
        }
        private void Known(Element e)
        {
            if (e.Unknown) throw Error(e.Offset, "UnknownLength");
        }
        private void Skip(Element e)
        {
            Known(e);
            token.ThrowIfCancellationRequested();
            stream.Position = e.End;
        }
        private ulong UInt(Element e)
        {
            Known(e);
            if (e.End - e.Start > 8) throw Error(e.Offset);
            ulong value = 0;
            while (stream.Position < e.End) value = (value << 8) | (uint)Byte(e.End);
            return value;
        }
        private byte[] Bytes(long end)
        {
            long size = end - stream.Position;
            if (size > Array.MaxLength) throw Error(stream.Position, "TextSize");
            var data = new byte[(int)size];
            int read = 0;
            while (read < data.Length)
            {
                token.ThrowIfCancellationRequested();
                int count = stream.Read(data, read, Math.Min(65536, data.Length - read));
                if (count == 0) throw Error(stream.Position, "Truncated");
                read += count;
            }
            return data;
        }
        private string Text(Element e)
        {
            Known(e);
            try { return Utf8.GetString(Bytes(e.End)).TrimEnd('\0'); }
            catch (DecoderFallbackException) { throw Error(e.Offset, "Utf8"); }
        }
        private double Float(Element e)
        {
            Known(e);
            long size = e.End - e.Start;
            if (size is not (4 or 8)) throw Error(e.Offset);
            byte[] bytes = Bytes(e.End);
            double value = size == 4 ? BinaryPrimitives.ReadSingleBigEndian(bytes) : BinaryPrimitives.ReadDoubleBigEndian(bytes);
            if (!double.IsFinite(value) || value <= 0) throw Error(e.Offset, "Timing");
            return value;
        }
        private static bool LevelOne(ulong id) => id is 0x114D9B74 or 0x1549A966 or 0x1654AE6B or 0x1F43B675 or 0x1C53BB6B or 0x1941A469 or 0x1043A770 or 0x1254C367;

        internal List<EmbeddedSubtitleTrack> Read()
        {
            try { return ReadCore(); }
            catch (OverflowException) { throw Error(stream.Position, "Timing"); }
            catch (IOException error)
            {
                throw new IOException(SR.Current.MatroskaError.ToString(stream.Position, error.Message), error);
            }
        }
        private List<EmbeddedSubtitleTrack> ReadCore()
        {
            token.ThrowIfCancellationRequested();
            long end = stream.Length;
            Element ebml = Header(end);
            if (ebml.Id != 0x1A45DFA3) throw Error(ebml.Offset);
            Known(ebml);
            bool docType = false;
            while (stream.Position < ebml.End)
            {
                Element e = Header(ebml.End);
                switch (e.Id)
                {
                    case 0x4282:
                        string type = Text(e);
                        if (type is not ("matroska" or "webm")) throw Error(e.Offset);
                        docType = true;
                        break;
                    case 0x42F7: if (UInt(e) > 1) throw Error(e.Offset); break;
                    case 0x42F2: if (UInt(e) > 4) throw Error(e.Offset); break;
                    case 0x42F3: if (UInt(e) > 8) throw Error(e.Offset); break;
                    case 0x4285: if (UInt(e) > 4) throw Error(e.Offset); break;
                    default: Skip(e); break;
                }
            }
            if (!docType) throw Error(ebml.Offset);
            Element segment = default;
            bool found = false;
            while (stream.Position < end)
            {
                Element e = Header(end);
                if (e.Id == 0x18538067) { segment = e; found = true; break; }
                if (e.Id is not (0xEC or 0xBF)) throw Error(e.Offset);
                Skip(e);
            }
            if (!found) throw Error(stream.Position);
            _segmentStart = segment.Start;
            _segmentEnd = segment.End;
            bool info = false, tracks = false;
            while (stream.Position < _segmentEnd)
            {
                Element e = Header(_segmentEnd);
                switch (e.Id)
                {
                    case 0x1549A966:
                        if (info) throw Error(e.Offset);
                        info = true; ReadInfo(e); break;
                    case 0x1654AE6B:
                        if (tracks) throw Error(e.Offset);
                        tracks = true; ReadTracks(e); break;
                    case 0x1F43B675: Cluster(e, false); break;
                    default: Skip(e); break;
                }
            }
            // Multiple Segments are separate timelines, not one subtitle document.
            while (stream.Position < end)
            {
                Element e = Header(end);
                if (e.Id is not (0xEC or 0xBF)) throw Error(e.Offset, "Segments");
                Skip(e);
            }
            stream.Position = _segmentStart;
            while (stream.Position < _segmentEnd)
            {
                Element e = Header(_segmentEnd);
                if (e.Id == 0x1F43B675) Cluster(e, true);
                else Skip(e);
            }
            var result = new List<EmbeddedSubtitleTrack>();
            foreach (Track track in _tracks.Values)
            {
                token.ThrowIfCancellationRequested();
                if (track.Type != 17) continue;
                SubtitleDocument? document = track.Reason == null ? Document(track) : null;
                result.Add(new EmbeddedSubtitleTrack(track.Number.ToString(CultureInfo.InvariantCulture), track.Name,
                    track.IetfLanguage ?? track.Language, track.Codec, document, track.Reason));
            }
            return result;
        }
        private void ReadInfo(Element parent)
        {
            Known(parent);
            var seen = new HashSet<ulong>();
            while (stream.Position < parent.End)
            {
                Element e = Header(parent.End);
                if (e.Id is 0x2AD7B1 or 0x4489 && !seen.Add(e.Id)) throw Error(e.Offset);
                if (e.Id == 0x2AD7B1)
                {
                    _scale = UInt(e);
                    if (_scale == 0) throw Error(e.Offset, "Timing");
                }
                else if (e.Id == 0x4489) _duration = checked((decimal)Float(e));
                else Skip(e);
            }
        }
        private void ReadTracks(Element parent)
        {
            Known(parent);
            while (stream.Position < parent.End)
            {
                Element entry = Header(parent.End);
                if (entry.Id != 0xAE) { Skip(entry); continue; }
                Known(entry);
                var track = new Track();
                var seen = new HashSet<ulong>();
                while (stream.Position < entry.End)
                {
                    Element e = Header(entry.End);
                    if (e.Id is 0xD7 or 0x83 or 0x86 or 0x536E or 0x22B59C or 0x22B59D or 0x23E383 or 0x23314F or 0x537F or 0x56AA or 0x56BB or 0x6D80 or 0x63A2 && !seen.Add(e.Id)) throw Error(e.Offset);
                    switch (e.Id)
                    {
                        case 0xD7: track.Number = UInt(e); break;
                        case 0x83: track.Type = UInt(e); break;
                        case 0x86: track.Codec = Text(e); break;
                        case 0x536E: track.Name = Text(e); break;
                        case 0x22B59C: track.Language = Text(e); break;
                        case 0x22B59D: track.IetfLanguage = Text(e); break;
                        case 0x23E383: track.DefaultDuration = UInt(e); break;
                        case 0x23314F:
                            if (Float(e) != 1) Fail(track, "TrackTiming");
                            break;
                        case 0x537F: if (UInt(e) != 0) Fail(track, "TrackTiming"); break;
                        case 0x56AA: if (UInt(e) != 0) Fail(track, "Delay"); break;
                        case 0x56BB: if (UInt(e) != 0) Fail(track, "Delay"); break;
                        case 0x6D80: Fail(track, "Encoding"); Skip(e); break;
                        case 0x63A2:
                            if (e.End != e.Start) Fail(track, "Private");
                            Skip(e); break;
                        default: Skip(e); break;
                    }
                }
                if (track.Number == 0 || track.Type == 0 || track.Codec.Length == 0 || !_tracks.TryAdd(track.Number, track)) throw Error(entry.Offset);
                if (track.Codec != "S_TEXT/UTF8") Fail(track, "Codec");
            }
        }
        private void Cluster(Element parent, bool collect)
        {
            long childrenStart = stream.Position;
            ulong? timestamp = null;
            long actualEnd = parent.End;
            // Timestamp need not precede Blocks. Scan once for it before processing frames.
            while (stream.Position < parent.End)
            {
                Element e = Header(parent.End);
                if (parent.Unknown && LevelOne(e.Id)) { actualEnd = e.Offset; break; }
                if (e.Id == 0xE7)
                {
                    if (timestamp.HasValue) throw Error(e.Offset);
                    timestamp = UInt(e);
                }
                else Skip(e);
            }
            if (collect)
            {
                if (!timestamp.HasValue) throw Error(parent.Offset, "Timing");
                stream.Position = childrenStart;
                while (stream.Position < actualEnd)
                {
                    Element e = Header(actualEnd);
                    if (e.Id == 0xA3) Block(e, timestamp.Value, null, false);
                    else if (e.Id == 0xA0) Group(e, timestamp.Value);
                    else Skip(e);
                }
            }
            stream.Position = actualEnd;
        }
        private void Group(Element parent, ulong timestamp)
        {
            Known(parent);
            Element? block = null;
            ulong? duration = null;
            bool unsupported = false;
            while (stream.Position < parent.End)
            {
                Element e = Header(parent.End);
                if (e.Id == 0xA1)
                {
                    if (block.HasValue) throw Error(e.Offset);
                    block = e;
                }
                else if (e.Id == 0x9B)
                {
                    if (duration.HasValue) throw Error(e.Offset);
                    duration = UInt(e);
                }
                else if (e.Id is 0x75A1 or 0xA4 or 0x75A2 or 0xFA or 0xFD) unsupported = true;
                Skip(e);
            }
            if (!block.HasValue) throw Error(parent.Offset);
            stream.Position = block.Value.Start;
            Block(block.Value, timestamp, duration, unsupported);
            stream.Position = parent.End;
        }
        private void Block(Element e, ulong timestamp, ulong? duration, bool unsupported)
        {
            Known(e);
            // All-one VINT data is a valid track number; only element sizes use the unknown-size sentinel.
            ulong number = Vint(e.End, false, out _, out _);
            if (number == 0 || !_tracks.TryGetValue(number, out Track? track)) throw Error(e.Offset);
            int hi = Byte(e.End), lo = Byte(e.End), flags = Byte(e.End);
            short relative = unchecked((short)((hi << 8) | lo));
            if (track.Type == 17)
            {
                if ((flags & 0x06) != 0) Fail(track, "Lacing");
                if ((flags & (e.Id == 0xA3 ? 0x78 : 0xF9)) != 0 || unsupported) Fail(track, "BlockFeatures");
                if (track.Reason == null)
                {
                    decimal start = ((decimal)timestamp + relative) * _scale;
                    decimal? length = duration.HasValue ? (decimal)duration.Value * _scale : track.DefaultDuration == 0 ? null : track.DefaultDuration;
                    track.Frames.Add(new Frame(start, length, stream.Position, e.End - stream.Position));
                }
            }
            Skip(e);
        }
        private SubtitleDocument? Document(Track track)
        {
            token.ThrowIfCancellationRequested();
            // Stable ordering preserves simultaneous overlapping cues without guessing an end at an equal timestamp.
            Frame[] frames = track.Frames.OrderBy(frame => frame.Start).ToArray();
            var cues = new List<SubtitleCue>();
            int next = 0;
            for (int i = 0; i < frames.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                Frame frame = frames[i];
                next = Math.Max(next, i + 1);
                while (next < frames.Length && frames[next].Start <= frame.Start) { token.ThrowIfCancellationRequested(); next++; }
                decimal? end = frame.Duration.HasValue ? frame.Start + frame.Duration.Value : next < frames.Length ? frames[next].Start : _duration * _scale;
                if (!end.HasValue || end.Value <= frame.Start)
                {
                    Fail(track, "MissingEnd");
                    return null;
                }
                decimal startTicks = Math.Max(0, frame.Start) / 100;
                decimal endTicks = end.Value / 100;
                if (endTicks <= 0) continue;
                if (startTicks > long.MaxValue || endTicks > long.MaxValue) throw Error(frame.Offset, "Timing");
                long start = (long)startTicks, finish = (long)endTicks;
                if (finish <= start) { Fail(track, "Resolution"); return null; }
                stream.Position = frame.Offset;
                string text;
                try { text = Utf8.GetString(Bytes(frame.Offset + frame.Size)); }
                catch (DecoderFallbackException) { throw Error(frame.Offset, "Utf8"); }
                if (!string.IsNullOrWhiteSpace(text)) cues.Add(new SubtitleCue(TimeSpan.FromTicks(start), TimeSpan.FromTicks(finish), text));
            }
            token.ThrowIfCancellationRequested();
            return new SubtitleDocument(cues);
        }
    }
}
