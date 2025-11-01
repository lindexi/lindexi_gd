using System.Diagnostics;
using DotNetCampus.Logging;

namespace DotNetCampus.Inking.Diagnostics;

static class StaticDebugLogger
{
    [Conditional("False")]
    public static void WriteLine(string message)
    {
        //if (!message.Contains("X11DeviceInputManager"))
        //{
        //    return;
        //}

        Log.Debug($"[InkCore] {message}");

        //Console.WriteLine(message);
    }
}
