using System.Diagnostics;
using DotNetCampus.Logging;

namespace UnoInk.Inking.X11Platforms;

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
