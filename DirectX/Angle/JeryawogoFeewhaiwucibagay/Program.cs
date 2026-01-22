// See https://aka.ms/new-console-template for more information

using System;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

using static Windows.Win32.PInvoke;

namespace JeryawogoFeewhaiwucibagay;

class Program
{
    [STAThread]
    static unsafe void Main(string[] args)
    {
        var window = CreateWindow();
        ShowWindow(window,  SHOW_WINDOW_CMD.SW_NORMAL);

        while (true)
        {
            var msg = new MSG();
            GetMessage(&msg, window, 0, 0);
        }

        Console.ReadLine();
    }

    private static unsafe HWND CreateWindow()
    {
        var style = WNDCLASS_STYLES.CS_OWNDC | WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW;

        var defaultCursor = LoadCursor(
            new HINSTANCE(IntPtr.Zero), new PCWSTR(IDC_ARROW.Value));

        var className = $"lindexi-{Guid.NewGuid().ToString()}";
        var title = "The Title";
        fixed (char* pClassName = className)
        fixed (char* pTitle = title)
        {
            var wndClassEx = new WNDCLASSEXW
            {
                cbSize = (uint) Marshal.SizeOf<WNDCLASSEXW>(),
                style = style,
                lpfnWndProc = new WNDPROC(Wndproc),
                hInstance = new HINSTANCE(GetModuleHandle(null).DangerousGetHandle()),
                hCursor = defaultCursor,
                hbrBackground = new HBRUSH(IntPtr.Zero),
                lpszClassName = new PCWSTR(pClassName)
            };
            ushort atom = RegisterClassEx(in wndClassEx);

            var windowHwnd =  CreateWindowEx
            (0, new PCWSTR((char*)atom), new PCWSTR(pTitle),
                WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_CLIPCHILDREN,
                CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,
                HWND.Null, HMENU.Null, HINSTANCE.Null, null
            );
            return windowHwnd;
        }
    }

    private static LRESULT Wndproc(HWND param0, uint param1, WPARAM param2, LPARAM param3)
    {
        return new LRESULT(0);
    }
}

