using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace AppLauncherWpf;

internal sealed class GlobalHotKey : IDisposable
{
    private const int HotKeyMessage = 0x0312;
    private const uint ShiftModifier = 0x0004;

    private readonly HwndSource source;
    private readonly int identifier;
    private bool disposed;

    public GlobalHotKey(nint windowHandle, int identifier, Key key, Action pressed)
    {
        ArgumentNullException.ThrowIfNull(pressed);

        this.identifier = identifier;
        source = HwndSource.FromHwnd(windowHandle)
            ?? throw new InvalidOperationException("The launcher window handle is not available.");
        Pressed = pressed;

        uint virtualKey = checked((uint)KeyInterop.VirtualKeyFromKey(key));
        if (!RegisterHotKey(windowHandle, identifier, ShiftModifier, virtualKey))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        source.AddHook(WndProc);
    }

    private Action Pressed { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        source.RemoveHook(WndProc);
        UnregisterHotKey(source.Handle, identifier);
        disposed = true;
    }

    private nint WndProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == HotKeyMessage && wParam == identifier)
        {
            Pressed();
            handled = true;
        }

        return 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint windowHandle, int identifier, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint windowHandle, int identifier);
}
