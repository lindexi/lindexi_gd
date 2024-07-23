namespace SkiaInkCore.Diagnostics;

static class StaticDebugLogger
{
    //[Conditional("DEBUG")]
    public static void WriteLine(string message)
    {
        Console.WriteLine(message);
    }
}