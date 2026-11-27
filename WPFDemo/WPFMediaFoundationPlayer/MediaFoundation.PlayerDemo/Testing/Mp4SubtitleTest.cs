using System.Buffers.Binary;
using System.IO;
using System.Text;
using MediaFoundation.Core;

namespace MediaFoundation.PlayerDemo;

internal static class Mp4SubtitleTest
{
    internal static async Task RunAsync(Action<bool, string> check, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(check);
        token.ThrowIfCancellationRequested();
        var tx = new Spec("tx3g", [Tx("Hello 世界"), Tx(""), Tx16("Second")]);
        var web = new Spec("wvtt", [B("vttc", B("iden", Utf("id")), B("payl", Utf("Web\n字幕"))), B("vtte"), B("vttc", B("payl", Utf("Last")))]);
        var result = await Load(File([tx, web]), token);
        check(result.Count == 2, "Mp4BothCodecs");
        check(result[0].Id == "1" && result[0].Name == "Main" && result[0].Language == "eng" && result[0].Codec == "tx3g", "Mp4Metadata");
        check(Document(result[0]).Cues[0].Text == "Hello 世界", "Mp4Tx3gUtf8");
        check(Document(result[0]).Cues[1].Text == "Second", "Mp4Tx3gUtf16Bom");
        check(Document(result[1]).Cues[0].Text == "Web\n字幕", "Mp4WvttPayload");
        foreach (var track in result)
        {
            check(Document(track).Cues[0].Start == TimeSpan.Zero && Document(track).Cues[0].End == TimeSpan.FromSeconds(1), "Mp4MediaTimescale");
            check(Document(track).GetText(TimeSpan.FromSeconds(1)) == "", "Mp4EmptySampleClears");
            check(Document(track).Cues[1].Start == TimeSpan.FromSeconds(2) && Document(track).Cues[1].End == TimeSpan.FromSeconds(3), "Mp4NextSampleTime");
        }
        var chunked = await Load(File([new Spec("tx3g", [Tx("A"), Tx("B"), Tx("C")]) { Chunked = true, FixedSize = true, Co64 = true }], extended: true), token);
        check(Document(chunked[0]).Cues.Count == 3 && Document(chunked[0]).Cues[2].Start == TimeSpan.FromSeconds(2) && Document(chunked[0]).Cues[2].End == TimeSpan.FromSeconds(2.5), "Mp4StscMultipleChunksFixedSizeAndSttsRuns");
        var edited = await Load(File([tx with { Co64 = true, Edit = 1 }]), token);
        check(Document(edited[0]).Cues[0].Start == TimeSpan.FromSeconds(2) && Document(edited[0]).Cues[0].End == TimeSpan.FromSeconds(2.5), "Mp4EmptyEditAndMediaOffset");
        check(Document(edited[0]).Cues[1].Start == TimeSpan.FromSeconds(3.5), "Mp4Co64AndMovieTimescale");
        var clipped = await Load(File([tx with { Edit = 2 }]), token);
        check(Document(clipped[0]).Cues.Count == 1 && Document(clipped[0]).Cues[0].Text == "Second" && Document(clipped[0]).Cues[0].Start == TimeSpan.Zero, "Mp4PositiveMediaEditClips");
        var unsupported = await Load(File([
            tx,
            tx with { Codec = "stpp" },
            tx with { Codec = "enct" },
            tx with { Composition = true },
            tx with { Edit = 3 },
            tx with { Styled = true },
            web with { Samples = [B("vttc", B("sttg", Utf("align:start")), B("payl", Utf("Styled")))] },
            web with { Samples = [B("vttc", B("payl", Utf("<b>Styled</b>")))] },
            tx with { Samples = [Join(Tx("Styled"), B("styl", U16(0)))] },
            web with { Styled = true }
        ]), token);
        check(unsupported.Count == 10 && unsupported[0].Document != null && unsupported.Skip(1).All(t => t.Document == null && !string.IsNullOrWhiteSpace(t.UnsupportedReason)), "Mp4UnsupportedTracksRemainVisible");
        await Unsupported(File([tx], fragmented: true), "Mp4Moof", check, token);
        await Unsupported(Join(B("ftyp", Utf("isom"), U32(0)), B("moof")), "Mp4MoofWithoutMovie", check, token);
        await Broken(File([tx with { BadOffset = true }]), "Mp4SampleOffsetRejected", check, token);
        await Broken(File([tx with { BadCount = true }]), "Mp4SampleCountRejected", check, token);
        await Broken(File([tx with { Samples = [[0, 10, 65]] }]), "Mp4TextLengthRejected", check, token);
        await Broken(File([web with { Samples = [[0, 0, 0, 40, 118, 116, 116, 99]] }]), "Mp4NestedBoxBounds", check, token);
        await Broken(File([tx with { Samples = [[0, 1, 255]] }]), "Mp4InvalidUtf8", check, token);
        await Broken(Join(B("ftyp", Utf("isom"), U32(0)), U32(uint.MaxValue), Utf("moov")), "Mp4BoxOverflowRejected", check, token);
        await Broken(Join(B("ftyp", Utf("isom"), U32(0)), U32(1), Utf("moov"), U64(ulong.MaxValue)), "Mp4ExtendedBoxOverflowRejected", check, token);
        await Broken(File([tx])[..^1], "Mp4TruncationRejected", check, token);
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(token);
        canceled.Cancel();
        string canceledPath = Path.Combine(Path.GetTempPath(), $"mp4-subtitle-{Guid.NewGuid():N}.mp4");
        await System.IO.File.WriteAllBytesAsync(canceledPath, File([tx]), token);
        bool canceledResult = false;
        try { await EmbeddedSubtitleReader.LoadAsync(canceledPath, canceled.Token); }
        catch (OperationCanceledException) { canceledResult = true; }
        check(canceledResult, "Mp4Cancellation");
    }

    private sealed record Spec(string Codec, byte[][] Samples)
    {
        internal bool Co64 { get; init; }
        internal bool Chunked { get; init; }
        internal bool FixedSize { get; init; }
        internal bool Composition { get; init; }
        internal bool Styled { get; init; }
        internal bool BadOffset { get; init; }
        internal bool BadCount { get; init; }
        internal int Edit { get; init; }
    }

    private static SubtitleDocument Document(EmbeddedSubtitleTrack track) => track.Document ?? throw new InvalidOperationException(track.UnsupportedReason);
    private static async Task<IReadOnlyList<EmbeddedSubtitleTrack>> Load(byte[] data, CancellationToken token)
    {
        string path = Path.Combine(Path.GetTempPath(), $"mp4-subtitle-{Guid.NewGuid():N}.mp4");
        await System.IO.File.WriteAllBytesAsync(path, data, token);
        return await EmbeddedSubtitleReader.LoadAsync(path, token);
    }
    private static async Task Unsupported(byte[] data, string name, Action<bool, string> check, CancellationToken token)
    {
        var tracks = await Load(data, token);
        check(tracks.Count > 0 && tracks.All(t => t.Document == null && !string.IsNullOrWhiteSpace(t.UnsupportedReason)), name);
    }
    private static async Task Broken(byte[] data, string name, Action<bool, string> check, CancellationToken token)
    {
        string originalLanguage = SR.Current.IetfLanguageTag;
        bool rejected = false;
        try
        {
            SR.SetCurrent("en");
            await Load(data, token);
        }
        catch (IOException error)
        {
            rejected = (error.InnerException is FormatException or IOException) && error.InnerException.Message.Contains("offset", StringComparison.Ordinal);
        }
        finally { SR.SetCurrent(originalLanguage); }
        check(rejected, name);
    }
    private static byte[] File(Spec[] specs, bool fragmented = false, bool extended = false)
    {
        byte[] ftyp = B("ftyp", Utf("isom"), U32(0), Utf("isom"));
        byte[] payload = Join(specs.SelectMany(s => s.Samples).ToArray());
        var tracks = new List<byte[]>();
        uint offset = (uint)ftyp.Length + (extended ? 16u : 8u);
        for (int i = 0; i < specs.Length; i++)
        {
            Spec spec = specs[i];
            tracks.Add(Track(spec, (uint)i + 1, offset));
            offset += (uint)spec.Samples.Sum(s => s.Length);
        }
        byte[] mvhd = B("mvhd", U32(0), U32(0), U32(0), U32(100), U32(1000), new byte[80]);
        byte[] mdat = extended ? Join(U32(1), Utf("mdat"), U64((ulong)payload.Length + 16), payload) : B("mdat", payload);
        return Join(ftyp, mdat, B("moov", Join([mvhd, .. tracks])), fragmented ? B("moof") : []);
    }
    private static byte[] Track(Spec spec, uint id, uint offset)
    {
        byte[] header = B("tkhd", U32(3), U32(0), U32(0), U32(id), U32(0), U32(300), new byte[60]);
        ushort language = (ushort)((5 << 10) | (14 << 5) | 7);
        byte[] mdhd = B("mdhd", U32(0), U32(0), U32(0), U32(1000), U32((uint)spec.Samples.Length * 1000), U16(language), U16(0));
        byte[] handler = B("hdlr", U32(0), U32(0), Utf("sbtl"), new byte[12], Utf("Main\0"));
        byte[] defaults = new byte[30];
        defaults[23] = 1;
        defaults[25] = 12;
        defaults.AsSpan(26, 4).Fill(255);
        if (spec.Styled) defaults[24] = 1;
        byte[] description = spec.Codec == "tx3g"
            ? Join(defaults, B("ftab", U16(1), U16(1), [5], Utf("Arial")))
            : spec.Codec == "wvtt" ? B("vttC", Utf(spec.Styled ? "WEBVTT\n\nSTYLE\n::cue { color: red; }" : "WEBVTT")) : [];
        byte[] stsd = B("stsd", U32(0), U32(1), B(spec.Codec, new byte[6], U16(1), description));
        byte[] stsz = B("stsz", U32(0), U32(spec.FixedSize ? (uint)spec.Samples[0].Length : 0), U32((uint)spec.Samples.Length), spec.FixedSize ? [] : Join(spec.Samples.Select(s => U32((uint)s.Length)).ToArray()));
        byte[] stts = B("stts", U32(0), U32(1), U32((uint)spec.Samples.Length + (spec.BadCount ? 1u : 0u)), U32(1000));
        byte[] stsc = B("stsc", U32(0), U32(1), U32(1), U32((uint)spec.Samples.Length), U32(1));
        ulong position = spec.BadOffset ? ulong.MaxValue : offset;
        byte[] chunks = spec.Co64 || spec.BadOffset ? B("co64", U32(0), U32(1), U64(position)) : B("stco", U32(0), U32(1), U32(offset));
        if (spec.Chunked)
        {
            stts = B("stts", U32(0), U32(2), U32(2), U32(1000), U32(1), U32(500));
            stsc = B("stsc", U32(0), U32(2), U32(1), U32(2), U32(1), U32(2), U32(1), U32(1));
            chunks = B("co64", U32(0), U32(2), U64(offset), U64(offset + (uint)(spec.Samples[0].Length + spec.Samples[1].Length)));
        }
        byte[] ctts = spec.Composition ? B("ctts", U32(0), U32(1), U32((uint)spec.Samples.Length), U32(100)) : [];
        byte[] edit = spec.Edit switch
        {
            1 => B("edts", B("elst", U32(0), U32(2), Edit(200, -1), Edit(300, 500))),
            2 => B("edts", B("elst", U32(0), U32(1), Edit(100, 2000))),
            3 => B("edts", B("elst", U32(0), U32(2), Edit(100, 0), Edit(100, 1000))),
            _ => []
        };
        byte[] dinf = B("dinf", B("dref", U32(0), U32(1), B("url ", U32(1))));
        return B("trak", header, edit, B("mdia", mdhd, handler, B("minf", B("nmhd", U32(0)), dinf, B("stbl", stsd, stsz, stts, stsc, chunks, ctts))));
    }
    private static byte[] Edit(uint duration, int media) => Join(U32(duration), U32(unchecked((uint)media)), U16(1), U16(0));
    private static byte[] Tx(string text) { byte[] bytes = Utf(text); return Join(U16((ushort)bytes.Length), bytes); }
    private static byte[] Tx16(string text) { byte[] bytes = Join([254, 255], Encoding.BigEndianUnicode.GetBytes(text)); return Join(U16((ushort)bytes.Length), bytes); }
    private static byte[] Utf(string value) => Encoding.UTF8.GetBytes(value);
    private static byte[] U16(ushort value) { byte[] bytes = new byte[2]; BinaryPrimitives.WriteUInt16BigEndian(bytes, value); return bytes; }
    private static byte[] U32(uint value) { byte[] bytes = new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(bytes, value); return bytes; }
    private static byte[] U64(ulong value) { byte[] bytes = new byte[8]; BinaryPrimitives.WriteUInt64BigEndian(bytes, value); return bytes; }
    private static byte[] B(string type, params byte[][] data) { byte[] payload = Join(data); return Join(U32((uint)payload.Length + 8), Utf(type), payload); }
    private static byte[] Join(params byte[][] parts)
    {
        using var stream = new MemoryStream();
        foreach (byte[] part in parts) stream.Write(part);
        return stream.ToArray();
    }
}
