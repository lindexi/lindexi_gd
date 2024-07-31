using System.Diagnostics;

namespace SkiaInkCore.Diagnostics;

static class StaticDebugLogger
{
    [Conditional("FALSE")]
    public static void WriteLine(string message)
    {
        Console.WriteLine(message);
    }
}