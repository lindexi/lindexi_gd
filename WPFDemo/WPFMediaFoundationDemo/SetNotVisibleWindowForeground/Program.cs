using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WindowSamples;

var title = "MessageOnly-" + Guid.NewGuid().ToString("N");
using var window = new NativeWindow(title, HWND.HWND_MESSAGE);
var initialParent = PInvoke.GetParent(window.Handle);
var messageParent = PInvoke.GetAncestor(window.Handle, GET_ANCESTOR_FLAGS.GA_PARENT);
var found = PInvoke.FindWindowEx(HWND.HWND_MESSAGE, default, null, title);
Console.WriteLine($"GetParent={initialParent}, GA_PARENT={messageParent}, HWND_MESSAGE={HWND.HWND_MESSAGE}, found={found}, requested={window.Handle}");
// GetParent depends on window styles; HWND_MESSAGE is a creation/search sentinel, not the actual parent handle.
if (!PInvoke.IsWindow(window.Handle) || messageParent.IsNull || found != window.Handle)
    throw new InvalidOperationException("Message-only window creation failed.");
var accepted = PInvoke.SetForegroundWindow(window.Handle);
var foreground = PInvoke.GetForegroundWindow();
Console.WriteLine($"SetForegroundWindow={accepted}, requested={window.Handle}, foreground={foreground}");
if (Array.IndexOf(args, "--self-test") >= 0)
{
    var hiddenTitle = "Hidden-" + Guid.NewGuid().ToString("N");
    using var hiddenWindow = new NativeWindow(hiddenTitle);
    var hiddenFound = PInvoke.FindWindowEx(HWND.HWND_MESSAGE, default, null, hiddenTitle);
    Console.WriteLine($"Hidden GetParent={PInvoke.GetParent(hiddenWindow.Handle)}, message-only search={hiddenFound}");
    if (!PInvoke.IsWindow(hiddenWindow.Handle) || PInvoke.IsWindowVisible(hiddenWindow.Handle) || !hiddenFound.IsNull)
        throw new InvalidOperationException("Hidden top-level window control test failed.");

    PInvoke.SendMessage(window.Handle, PInvoke.WM_KEYDOWN, new(0x41), default);
    if (!PInvoke.IsWindow(window.Handle) || PInvoke.IsWindowVisible(window.Handle) ||
        PInvoke.FindWindowEx(HWND.HWND_MESSAGE, default, null, title) != window.Handle ||
        window.KeyMessages != 1 || window.FocusMessages < 1 || !accepted || foreground != window.Handle ||
        PInvoke.GetForegroundWindow() != window.Handle)
        throw new InvalidOperationException("Message-only window test failed.");
    Console.WriteLine($"PASS message-only window, architecture={RuntimeInformation.ProcessArchitecture}");
}
else NativeWindow.RunMessageLoop();
