using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

var stopwatch = new Stopwatch();
var totalTimeStopwatch = Stopwatch.StartNew();

int count = 0;
while (count < int.MaxValue - 1)
{
    stopwatch.Restart();
    var hResult = DwmFlush();
    stopwatch.Stop();
    count++;

    Console.WriteLine($"Elapsed={stopwatch.Elapsed.TotalMilliseconds:0.00}ms Ave={totalTimeStopwatch.Elapsed.TotalMilliseconds / count:0.00}ms HResult={hResult}");

    if (hResult != 0)
    {
        throw new Win32Exception();
    }
}

Console.WriteLine("Hello, World!");

[DllImport("Dwmapi.dll")]
static extern int DwmFlush();