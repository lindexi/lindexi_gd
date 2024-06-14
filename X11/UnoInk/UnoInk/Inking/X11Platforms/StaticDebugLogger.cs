using System.Diagnostics;

namespace UnoInk.Inking.X11Platforms;

static class StaticDebugLogger
{
    [Conditional("DEBUG")]
    public static void WriteLine(string message)
    {
        Console.WriteLine(message);
    }
}
