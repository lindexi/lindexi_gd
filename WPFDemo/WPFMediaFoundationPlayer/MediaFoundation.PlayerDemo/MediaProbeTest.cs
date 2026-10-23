using System.IO;
using System.Text;

namespace MediaFoundation.PlayerDemo;

internal static class MediaProbeTest
{
    internal static async Task RunAsync(string path)
    {
        static byte[] Join(params byte[][] parts) => parts.SelectMany(p => p).ToArray();
        static byte[] E(string id, byte[] data)
        {
            if (data.Length >= 127) throw new InvalidOperationException();
            return Join(Convert.FromHexString(id), [(byte)(0x80 | data.Length)], data);
        }
        static byte[] Text(string text) => Encoding.UTF8.GetBytes(text);
        byte[] video = E("AE", Join(E("D7", [1]), E("83", [1]), E("86", Text("V_MPEG4/ISO/AVC")), E("63A2", [1, 100, 0, 41]), E("E0", Join(E("B0", [7, 128]), E("BA", [4, 56])))));
        byte[] audio = E("AE", Join(E("D7", [2]), E("83", [2]), E("86", Text("A_DTS")), E("22B59C", Text("jpn")), E("E1", Join(E("9F", [6]), E("B5", Convert.FromHexString("473B8000"))))));
        byte[] fixture = Join(E("1A45DFA3", []), Convert.FromHexString("18538067FF"), E("1F43B675", [0, 0, 0, 0]), E("1654AE6B", Join(video, audio)));
        using var input = new MemoryStream(fixture);
        string tracks = MatroskaTracks.Read(input, CancellationToken.None);
        foreach (string expected in new[] { "track=1", "codec=V_MPEG4/ISO/AVC", "width=1920", "height=1080", "avcProfile=100", "avcLevel=41", "track=2", "codec=A_DTS", "language=jpn", "channels=6", "sampleRate=48000" })
            if (!tracks.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException("Missing fixture metadata: " + expected);
        byte[] hevc = E("AE", Join(E("D7", [1]), E("83", [1]), E("86", Text("V_MPEGH/ISO/HEVC")),
            E("63A2", Convert.FromHexString("01022000000090000000000078F000FCFDFAFA00000F04200001001840010C01"))));
        using var hevcInput = new MemoryStream(Join(E("1A45DFA3", []), Convert.FromHexString("18538067FF"), E("1654AE6B", hevc)));
        string hevcTracks = MatroskaTracks.Read(hevcInput, CancellationToken.None);
        foreach (string expected in new[] { "hevcProfileIdc=2", "hevcLevelIdc=120", "hevcLumaBitDepth=10", "hevcChromaBitDepth=10" })
            if (!hevcTracks.Contains(expected, StringComparison.Ordinal)) throw new InvalidOperationException("Missing HEVC metadata: " + expected);
        using var truncated = new MemoryStream(fixture[..^1]);
        try { MatroskaTracks.Read(truncated, CancellationToken.None); throw new InvalidOperationException("Accepted truncated Tracks"); }
        catch (InvalidDataException) { }
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        input.Position = 0;
        try { MatroskaTracks.Read(input, cancelled.Token); throw new InvalidOperationException("Ignored cancellation"); }
        catch (OperationCanceledException) { }
        using var unknown = new MemoryStream(Join(E("1A45DFA3", []), Convert.FromHexString("18538067FF1F43B675FF")));
        try { MatroskaTracks.Read(unknown, CancellationToken.None); throw new InvalidOperationException("Accepted unknown Cluster boundary"); }
        catch (InvalidDataException) { }
        string native = await MediaProbe.RunAsync(new Uri(Path.GetFullPath(path)), CancellationToken.None);
        if (!native.Contains("MF track=", StringComparison.Ordinal) || !native.Contains("subtype=", StringComparison.Ordinal) || !native.Contains("MFT candidate=", StringComparison.Ordinal))
            throw new InvalidOperationException("Native probe incomplete: " + native);
        await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, "media-probe-result.log"), "PASS: EBML video/audio, AVC private metadata, Cluster skip, truncation, unknown boundary, cancellation; native MF and MFT enumeration\n" + tracks + native);
    }
}
