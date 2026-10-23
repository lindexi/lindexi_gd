using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows6.0")]

namespace WindowSamples;

internal sealed unsafe class NativeWindow : IDisposable
{
    private readonly string className = "CsWin32Sample" + Guid.NewGuid().ToString("N");
    private readonly WNDPROC procedure;
    private readonly HINSTANCE module;
    internal HWND Handle { get; }
    internal int FocusMessages { get; private set; }
    internal int KeyMessages { get; private set; }

    internal NativeWindow(string title, HWND parent = default)
    {
        procedure = WindowProc;
        module = (HINSTANCE)PInvoke.GetModuleHandle((Windows.Win32.Foundation.PCWSTR)default);
        fixed (char* name = className)
        fixed (char* text = title)
        {
            var windowClass = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = procedure,
                hInstance = module,
                lpszClassName = name
            };
            if (PInvoke.RegisterClassEx(in windowClass) == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
            Handle = PInvoke.CreateWindowEx(0, name, text, WINDOW_STYLE.WS_OVERLAPPEDWINDOW,
                100, 100, 640, 480, parent, default, module, null);
            if (Handle.IsNull)
            {
                int error = Marshal.GetLastPInvokeError();
                PInvoke.UnregisterClass(name, module);
                throw new Win32Exception(error);
            }
        }
    }

    internal void Show() => PInvoke.ShowWindow(Handle, SHOW_WINDOW_CMD.SW_SHOW);

    internal void SetStyles(WINDOW_STYLE style, WINDOW_EX_STYLE extendedStyle)
    {
        // SetWindowLong can return zero on success; inspect the saved error explicitly.
        Marshal.SetLastPInvokeError(0);
        if (PInvoke.SetWindowLong(Handle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, unchecked((int)style)) == 0 && Marshal.GetLastPInvokeError() != 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        Marshal.SetLastPInvokeError(0);
        if (PInvoke.SetWindowLong(Handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, unchecked((int)extendedStyle)) == 0 && Marshal.GetLastPInvokeError() != 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    internal int ResizeToMinimum()
    {
        if (!PInvoke.SetWindowPos(Handle, default, 0, 0, 1, 480,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        RECT rect;
        if (!PInvoke.GetWindowRect(Handle, &rect)) throw new Win32Exception(Marshal.GetLastPInvokeError());
        int width = rect.right - rect.left;
        if (width < 1) throw new InvalidOperationException("Invalid window width.");
        Console.WriteLine($"Width={width}");
        return width;
    }

    internal static void RunMessageLoop()
    {
        MSG message;
        int result;
        while ((result = PInvoke.GetMessage(&message, default, 0, 0)) > 0)
        {
            PInvoke.TranslateMessage(&message);
            PInvoke.DispatchMessage(&message);
        }
        if (result == -1) throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    private LRESULT WindowProc(HWND window, uint message, WPARAM wParam, LPARAM lParam)
    {
        if (message == PInvoke.WM_SETFOCUS || message == PInvoke.WM_KILLFOCUS)
        {
            FocusMessages++;
            Console.WriteLine($"Focus message={message}, other={wParam.Value}");
        }
        if (message == PInvoke.WM_KEYDOWN)
        {
            KeyMessages++;
            Console.WriteLine("Key Down");
        }
        if (message == PInvoke.WM_DESTROY) PInvoke.PostQuitMessage(0);
        return PInvoke.DefWindowProc(window, message, wParam, lParam);
    }

    public void Dispose()
    {
        if (PInvoke.IsWindow(Handle) && !PInvoke.DestroyWindow(Handle)) throw new Win32Exception(Marshal.GetLastPInvokeError());
        fixed (char* name = className)
        {
            if (!PInvoke.UnregisterClass(name, module)) throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
        GC.KeepAlive(procedure);
    }
}
