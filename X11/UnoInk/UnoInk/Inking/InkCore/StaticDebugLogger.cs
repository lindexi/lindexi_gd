using System.Diagnostics;

namespace UnoInk.Inking.InkCore;

static class StaticDebugLogger
{
    [Conditional("DEBUG")]
    public static void WriteLine(string message)
    {
        Console.WriteLine(message);
    }
}