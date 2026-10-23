using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using WindowSamples;

using var first = new NativeWindow("TestWin32Window");
using var second = new NativeWindow("Win32Window");
first.Show();
second.Show();
if (args.Contains("--self-test"))
{
    int before = first.FocusMessages;
    PInvoke.SendMessage(first.Handle, PInvoke.WM_SETFOCUS, new WPARAM(0), default);
    PInvoke.SendMessage(first.Handle, PInvoke.WM_KILLFOCUS, new WPARAM(0), default);
    if (first.FocusMessages != before + 2 || !PInvoke.IsWindowVisible(first.Handle) || !PInvoke.IsWindowVisible(second.Handle))
        throw new InvalidOperationException("Window/focus test failed.");
    Console.WriteLine($"PASS windows/focus, architecture={RuntimeInformation.ProcessArchitecture}");
}
else NativeWindow.RunMessageLoop();
