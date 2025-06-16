using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

var stopwatch = new Stopwatch();

while (true)
{
    stopwatch.Restart();
    var hResult = DwmFlush();
    stopwatch.Stop();

    Console.WriteLine($"Elapsed={stopwatch.ElapsedMilliseconds}ms HResult={hResult}");

    if (hResult != 0)
    {
        // Fail
        break;
    }
}

Console.WriteLine("Hello, World!");

[DllImport("Dwmapi.dll")]
static extern int DwmFlush();