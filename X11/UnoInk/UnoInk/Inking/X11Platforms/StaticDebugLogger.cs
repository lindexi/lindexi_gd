using System.Diagnostics;

namespace UnoInk.Inking.X11Platforms;

static class StaticDebugLogger
{
    [Conditional("False")]
    public static void WriteLine(string message)
    {
        Console.WriteLine(message);
    }
}
