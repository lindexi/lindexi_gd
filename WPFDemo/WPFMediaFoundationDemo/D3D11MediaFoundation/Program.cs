using MediaSamples;

namespace D3D11MediaFoundation;

internal static class Program
{
    private static int Main(string[] args) => DesktopRunner.Run(args, SampleKind.SourceReader11);
}
