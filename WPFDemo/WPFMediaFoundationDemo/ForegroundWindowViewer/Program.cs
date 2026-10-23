using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;

bool selfTest = Array.IndexOf(args, "--self-test") >= 0;
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
do
{
    HWND window = PInvoke.GetForegroundWindow();
    Console.WriteLine($"{window} {ReadTitle(window)}");
    if (selfTest)
    {
        using var probe = new WindowSamples.NativeWindow("ForegroundTitleProbe");
        if (ReadTitle(probe.Handle) != "ForegroundTitleProbe")
            throw new InvalidOperationException("Window title round-trip failed.");
        Console.WriteLine($"PASS foreground/title, architecture={RuntimeInformation.ProcessArchitecture}");
        break;
    }
} while (!cancellation.Token.WaitHandle.WaitOne(100));

static unsafe string ReadTitle(HWND window)
{
    int length = PInvoke.GetWindowTextLength(window);
    var buffer = new char[Math.Max(length + 1, 256)];
    fixed (char* text = buffer)
    {
        int copied = PInvoke.GetWindowText(window, text, buffer.Length);
        return new string(text, 0, copied);
    }
}
