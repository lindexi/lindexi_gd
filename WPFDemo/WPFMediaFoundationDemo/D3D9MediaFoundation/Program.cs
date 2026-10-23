using MediaSamples;

namespace D3D9MediaFoundation;

internal static class Program
{
    private static int Main(string[] args) => DesktopRunner.Run(args, SampleKind.SourceReader9);
}
