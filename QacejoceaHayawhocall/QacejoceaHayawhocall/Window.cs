using System.Diagnostics;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using static Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE;
using static Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE;
using static Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX;
using static Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD;
using Vortice.Mathematics;

namespace QacejoceaHayawhocall;

public sealed class Window
{
    public string Title { get; private set; }
    public SizeI ClientSize { get; private set; }

    public unsafe Window(string title, int width = 1280, int height = 720)
    {
        Title = title;
        ClientSize = new(width, height);

        WINDOW_STYLE style = 0;
        const bool resizable = true;
        const bool fullscreen = false;

        // Setup the screen settings depending on whether it is running in full screen or in windowed mode.
        if (fullscreen)
        {
            style = WS_CLIPSIBLINGS | WS_GROUP | WS_TABSTOP;
        }
        else
        {
            style = WS_CAPTION |
                WS_SYSMENU |
                WS_MINIMIZEBOX |
                WS_CLIPSIBLINGS |
                WS_BORDER |
                WS_DLGFRAME |
                WS_THICKFRAME |
                WS_GROUP |
                WS_TABSTOP;

            if (resizable)
            {
                style |= WS_SIZEBOX;
            }
            else
            {
                style |= WS_MAXIMIZEBOX;
            }
        }

        int x = 0;
        int y = 0;
        int windowWidth;
        int windowHeight;

        if (ClientSize.Width > 0 && ClientSize.Height > 0)
        {
            var rect = new RECT
            {
                right = ClientSize.Width,
                bottom = ClientSize.Height
            };

            // Adjust according to window styles
            AdjustWindowRectEx(&rect, style, false, WS_EX_APPWINDOW);

            windowWidth = rect.right - rect.left;
            windowHeight = rect.bottom - rect.top;

            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            // Place the window in the middle of the screen.WS_EX_APPWINDOW
            x = (screenWidth - windowWidth) / 2;
            y = (screenHeight - windowHeight) / 2;
        }
        else
        {
            x = y = windowWidth = windowHeight = CW_USEDEFAULT;
        }

        _hWnd = CreateWindowEx(
            WS_EX_APPWINDOW,
            Application.WindowClassName,
            Title,
            style,
            x,
            y,
            windowWidth,
            windowHeight,
            default,
            default,
            default,
            null
        );

        if (_hWnd.Value == IntPtr.Zero)
        {
            return;
        }

        ShowWindow(_hWnd, SW_NORMAL);
        RECT windowRect;
        GetClientRect(_hWnd, &windowRect);
        ClientSize = new(windowRect.right - windowRect.left, windowRect.bottom - windowRect.top);
    }

    private const int CW_USEDEFAULT = unchecked((int) 0x80000000);
    private HWND _hWnd;

    public nint Handle => _hWnd.Value;

    public unsafe RectI Bounds
    {
        get
        {
            RECT windowBounds;
            GetWindowRect(_hWnd, &windowBounds);

            return RectI.FromLTRB(windowBounds.left, windowBounds.top, windowBounds.right, windowBounds.bottom);
        }
    }
}