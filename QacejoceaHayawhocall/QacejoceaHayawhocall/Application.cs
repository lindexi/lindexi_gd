using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using static Windows.Win32.UI.WindowsAndMessaging.PEEK_MESSAGE_REMOVE_TYPE;
using static Windows.Win32.UI.WindowsAndMessaging.WNDCLASS_STYLES;
using static Windows.Win32.UI.Input.KeyboardAndMouse.VIRTUAL_KEY;

namespace QacejoceaHayawhocall;


internal class Application
{
    public const string WindowClassName = "VorticeWindow";

    public unsafe Application()
    {
        var hInstance = GetModuleHandle((string) null);


        fixed (char* lpszClassName = WindowClassName)
        {
            PCWSTR szCursorName = new((char*) IDC_ARROW);

            var wndClassEx = new WNDCLASSEXW
            {
                cbSize = (uint) Unsafe.SizeOf<WNDCLASSEXW>(),
                style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC,
                lpfnWndProc = &WndProc,
                hInstance = (HINSTANCE) hInstance.DangerousGetHandle(),
                hCursor = LoadCursor((HINSTANCE) IntPtr.Zero, szCursorName),
                hbrBackground = (Windows.Win32.Graphics.Gdi.HBRUSH) IntPtr.Zero,
                hIcon = (HICON) IntPtr.Zero,
                lpszClassName = lpszClassName
            };

            ushort atom = RegisterClassEx(&wndClassEx);

            if (atom == 0)
            {
                throw new InvalidOperationException(
                    $"Failed to register window class. Error: {Marshal.GetLastWin32Error()}"
                );
            }
        }

        MainWindow = new Window("Demo");
        _graphicsDevice = new GraphicsDevice(this);
    }

    public unsafe void Run()
    {
        Windows.Win32.UI.WindowsAndMessaging.MSG msg;

        while (true)
        {
            if (PeekMessage(out msg, default, 0, 0, PM_REMOVE) != false)
            {
                _ = TranslateMessage(&msg);
                _ = DispatchMessage(&msg);

                if (msg.message == WM_QUIT)
                {
                    return;
                }
            }

            Tick();
        }
    }

    public Window MainWindow { get; }

    public void Tick()
    {
        _graphicsDevice.DrawFrame();
    }

    private readonly IGraphicsDevice _graphicsDevice;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static LRESULT WndProc(HWND hWnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        if (message == WM_ACTIVATEAPP)
        {
            //if (wParam != 0)
            //{
            //    Current?.OnActivated();
            //}
            //else
            //{
            //    Current?.OnDeactivated();
            //}

            return DefWindowProc(hWnd, message, wParam, lParam);
        }

        switch (message)
        {
            case WM_KEYDOWN:
            case WM_KEYUP:
            case WM_SYSKEYDOWN:
            case WM_SYSKEYUP:
                //OnKey(message, wParam, lParam);
                break;

            case WM_DESTROY:
                PostQuitMessage(0);
                break;
        }

        return DefWindowProc(hWnd, message, wParam, lParam);
    }
}

public interface IGraphicsDevice : IDisposable
{
    void DrawFrame();
}