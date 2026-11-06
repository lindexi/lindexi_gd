using System.Buffers.Binary;
using System.IO;
using System.Text;
using MediaFoundation.Core;

namespace MediaFoundation.PlayerDemo;

internal static class MatroskaSubtitleTest
{
    // Generated fixtures exercise the public file API; no media decoder or third-party fixture is used.
    internal static async Task RunAsync(Action<bool, string> check, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(check);
        token.ThrowIfCancellationRequested();
        byte[] tracks = E(0x1654AE6B,
            Track(1, "S_TEXT/UTF8", E(0x536E, Encoding.UTF8.GetBytes("Main")), E(0x22B59C, Encoding.UTF8.GetBytes("eng")), E(0x22B59D, Encoding.UTF8.GetBytes("en-US"))),
            Track(2, "S_TEXT/ASS"),
            Track(3, "S_TEXT/UTF8", U(0x23E383, 500_000_000)));
        byte[] cluster = E(0x1F43B675,
            Group(1, -100, 300, "Hello 世界\nSecond"),
            Group(1, 100, 400, "Overlap"),
            Block(0xA3, 3, 200, Encoding.UTF8.GetBytes("Default")), U(0xE7, 0));
        var result = await Load(File(tracks, cluster), token);
        check(result.Count == 3, "MkvAllSubtitleTracks");
        check(result[0].Id == "1" && result[0].Name == "Main" && result[0].Language == "en-US", "MkvTrackMetadata");
        check(result[1].Document == null && !string.IsNullOrWhiteSpace(result[1].UnsupportedReason), "MkvUnsupportedCodecEnumerated");
        SubtitleDocument document = Document(result[0]);
        check(document.Cues[0].Start == TimeSpan.Zero && document.Cues[0].End == TimeSpan.FromMilliseconds(200), "MkvNegativeStartClippedNotShifted");
        check(document.Cues[0].Text == "Hello 世界\nSecond", "MkvUtf8Preserved");
        check(document.GetText(TimeSpan.FromMilliseconds(100)) == "Hello 世界\nSecond\nOverlap", "MkvOverlap");
        check(document.GetText(TimeSpan.FromMilliseconds(200)) == "Overlap", "MkvHalfOpenBoundary");
        check(Document(result[2]).Cues[0].End == TimeSpan.FromMilliseconds(700), "MkvDefaultDurationNanoseconds");

        var inferred = await Load(File(E(0x1654AE6B, Track(1, "S_TEXT/UTF8")),
            E(0x1549A966, U(0x2AD7B1, 2_000_000), E(0x4489, Double(1000))),
            E(0x1F43B675, U(0xE7, 100), Block(0xA3, 1, -50, Encoding.UTF8.GetBytes("First")),
                Block(0xA3, 1, 100, Encoding.UTF8.GetBytes("Last")))), token);
        check(Document(inferred[0]).Cues[0].Start == TimeSpan.FromMilliseconds(100), "MkvScaleAndSignedRelativeTime");
        check(Document(inferred[0]).Cues[0].End == TimeSpan.FromMilliseconds(400), "MkvNextSameTrackEnd");
        check(Document(inferred[0]).Cues[1].End == TimeSpan.FromSeconds(2), "MkvSegmentDurationEnd");

        byte[] oneTrack = E(0x1654AE6B, Track(1, "S_TEXT/UTF8"));
        var unknown = await Load(Join(E(0x1A45DFA3, E(0x4282, Encoding.ASCII.GetBytes("matroska"))),
            Id(0x18538067), [0xFF], oneTrack, Id(0x1F43B675), [0xFF], U(0xE7, 0), Group(1, 0, 100, "One"),
            E(0x1F43B675, U(0xE7, 100), Group(1, 0, 100, "Two"))), token);
        check(Document(unknown[0]).Cues.Count == 2, "MkvUnknownSegmentClusterSiblingBoundary");

        await Unsupported(File(E(0x1654AE6B, Track(1, "S_TEXT/UTF8", U(0x56AA, 1))), E(0x1F43B675, U(0xE7, 0), Group(1, 0, 10, "Delay"))), "MkvCodecDelay", check, token);
        await Unsupported(File(E(0x1654AE6B, Track(1, "S_TEXT/UTF8", E(0x6D80, E(0x6240, E(0x5034)))))), "MkvCompression", check, token);
        await Unsupported(File(E(0x1654AE6B, Track(1, "S_TEXT/UTF8", E(0x6D80, E(0x6240, E(0x5035)))))), "MkvEncryption", check, token);
        await Unsupported(File(oneTrack, E(0x1F43B675, U(0xE7, 0), Block(0xA3, 1, 0, [0, 65], 2))), "MkvLacing", check, token);
        await Unsupported(File(oneTrack, E(0x1F43B675, U(0xE7, 0), Block(0xA3, 1, 0, [65]))), "MkvMissingEnd", check, token);
        await Unsupported(File(oneTrack, E(0x1F43B675, U(0xE7, 0), E(0xA0, Block(0xA1, 1, 0, [65]), U(0x9B, 10), U(0x75A2, 1)))), "MkvDiscardPadding", check, token);

        var clipped = await Load(File(oneTrack, E(0x1F43B675, U(0xE7, 0), Group(1, -100, 50, "Before"), Group(1, 0, 1, "Visible"))), token);
        check(Document(clipped[0]).Cues.Count == 1, "MkvFullyNegativeCueRemoved");
        await Reject(File(oneTrack, E(0x1F43B675, U(0xE7, 0), E(0xA0, Block(0xA1, 1, 0, [0xC3, 0x28]), U(0x9B, 10)))), "MkvInvalidUtf8", check, token);
        await Reject(File(oneTrack, E(0x1F43B675, U(0xE7, 0), E(0xA3, [0x81, 0]))), "MkvShortBlock", check, token);
        byte[] valid = File(oneTrack, E(0x1F43B675, U(0xE7, 0), Group(1, 0, 10, "A")));
        await Reject(valid[..^1], "MkvTruncatedElement", check, token);
        await Reject(File(oneTrack, [0x1F, 0x43, 0xB6, 0x75, 0x81, 0xE7]), "MkvParentBoundary", check, token);
        await Reject(File(oneTrack, [0xEC, 0xFF]), "MkvUnknownLeafSize", check, token);
        await Reject(File(oneTrack, E(0x1F43B675, U(0xE7, 0), E(0xA3, [0]))), "MkvZeroVint", check, token);
        await Reject(File(E(0x1654AE6B, Track(1, "S_TEXT/UTF8"), Track(1, "S_TEXT/UTF8"))), "MkvDuplicateTrack", check, token);
        string cancelPath = Path.Combine(Path.GetTempPath(), "MatroskaCancel-" + Guid.NewGuid().ToString("N") + ".mkv");
        await System.IO.File.WriteAllBytesAsync(cancelPath, valid, token);
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(token);
        canceled.Cancel();
        try
        {
            await EmbeddedSubtitleReader.LoadAsync(cancelPath, canceled.Token);
            check(false, "MkvCancellation");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { check(true, "MkvCancellation"); }
    }

    private static SubtitleDocument Document(EmbeddedSubtitleTrack track) => track.Document ?? throw new InvalidOperationException(track.UnsupportedReason);
    private static async Task Unsupported(byte[] bytes, string name, Action<bool, string> check, CancellationToken token)
    {
        var tracks = await Load(bytes, token);
        check(tracks.Count == 1 && tracks[0].Document == null && !string.IsNullOrWhiteSpace(tracks[0].UnsupportedReason), name);
    }
    private static async Task Reject(byte[] bytes, string name, Action<bool, string> check, CancellationToken token)
    {
        try { await Load(bytes, token); check(false, name); }
        catch (IOException error) { check(error.InnerException is FormatException && error.Message.Contains("offset", StringComparison.OrdinalIgnoreCase), name); }
    }
    private static async Task<IReadOnlyList<EmbeddedSubtitleTrack>> Load(byte[] bytes, CancellationToken token)
    {
        string path = Path.Combine(Path.GetTempPath(), "MatroskaSubtitle-" + Guid.NewGuid().ToString("N") + ".mkv");
        await System.IO.File.WriteAllBytesAsync(path, bytes, token);
        return await EmbeddedSubtitleReader.LoadAsync(path, token);
    }
    private static byte[] File(byte[] tracks, params byte[][] cluster) => Join(E(0x1A45DFA3, E(0x4282, Encoding.ASCII.GetBytes("matroska"))), E(0x18538067, Join(tracks, Join(cluster))));
    private static byte[] Track(ulong number, string codec, params byte[][] extra) => E(0xAE, U(0xD7, number), U(0x83, 17), E(0x86, Encoding.ASCII.GetBytes(codec)), Join(extra));
    private static byte[] Group(int track, short relative, ulong duration, string text) => E(0xA0, Block(0xA1, track, relative, Encoding.UTF8.GetBytes(text)), U(0x9B, duration));
    private static byte[] Block(ulong id, int track, short relative, byte[] text, byte flags = 0) => E(id, [(byte)(0x80 | track), (byte)(relative >> 8), (byte)relative, flags], text);
    private static byte[] Double(double value)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(bytes, value);
        return bytes;
    }
    private static byte[] U(ulong id, ulong value) => E(id, Id(value));
    private static byte[] Id(ulong value)
    {
        var bytes = new List<byte>();
        do { bytes.Insert(0, (byte)value); value >>= 8; } while (value != 0);
        return bytes.ToArray();
    }
    private static byte[] E(ulong id, params byte[][] children)
    {
        byte[] payload = Join(children);
        int width = 1;
        while ((ulong)payload.Length >= (1UL << (width * 7)) - 1) width++;
        ulong size = (uint)payload.Length | (1UL << (width * 7));
        var encoded = new byte[width];
        for (int i = width - 1; i >= 0; i--) { encoded[i] = (byte)size; size >>= 8; }
        return Join(Id(id), encoded, payload);
    }
    private static byte[] Join(params byte[][] arrays)
    {
        var bytes = new byte[arrays.Sum(array => array.Length)];
        int offset = 0;
        foreach (byte[] array in arrays) { array.CopyTo(bytes, offset); offset += array.Length; }
        return bytes;
    }
}
