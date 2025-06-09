using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lsj.Util.Win32.Structs;
using Lsj.Util.Win32.Enums;
using static Lsj.Util.Win32.User32;
using static Lsj.Util.Win32.Gdi32;
using static Lsj.Util.Win32.Gdiplus;
using static Lsj.Util.Win32.Structs.BLENDFUNCTION;
using Lsj.Util.Win32.Marshals;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.Callbacks;


namespace DotNetCampus.Installer.Boost;

public class SplashImage
{
    /// <summary>
    /// Splash File Name
    /// </summary>
    private const string SplashFile = "SplashScreen.png";

    /// <summary>
    /// Image Width
    /// </summary>
    private const int ImageWidth = 780;

    /// <summary>
    /// Image Height
    /// </summary>
    private const int ImageHeight = 522;

    /// <summary>
    /// Window Class
    /// </summary>
    private const string WindowClass = "Splash Image";

    /// <summary>
    /// Window Name
    /// </summary>
    private const string WindowName = "Splash Image";


    /// <summary>
    /// Window Handle
    /// </summary>
    private static IntPtr _window;

    /// <summary>
    /// Window Rectangle
    /// </summary>
    private static RECT _windowRectangle;

    /// <summary>
    /// Window Size
    /// </summary>
    private static SIZE _windowSize;

    /// <summary>
    /// Screen DC
    /// </summary>
    private static readonly IntPtr _screenDC = GetDC(IntPtr.Zero);

    /// <summary>
    /// Window DC
    /// </summary>
    private static IntPtr _windowDC;

    /// <summary>
    /// Memory DC
    /// </summary>
    private static IntPtr _memoryDC = IntPtr.Zero;

    /// <summary>
    /// GDI+ Splash Image
    /// </summary>
    private static IntPtr _splashImage;

    /// <summary>
    /// GDI+ Graphics
    /// </summary>
    private static IntPtr _graphics;

    /// <summary>
    /// Memory Bitmap For SplashImage
    /// </summary>
    private static IntPtr _memoryBitmap = IntPtr.Zero;

    /// <summary>
    /// GDI+ Token
    /// </summary>
    private static UIntPtr _gdipToken = UIntPtr.Zero;

    public static void Main(string[] args)
    {
        try
        {
            var hInstance = Process.GetCurrentProcess().Handle;

            using var marshal = new StringToIntPtrMarshaler(WindowClass);
            var wndclass = new WNDCLASSEX
            {
                cbSize = (uint) Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = ClassStyles.CS_DBLCLKS,
                lpfnWndProc = new WNDPROC(),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = hInstance,
                hIcon = LoadIcon(hInstance, SystemIcons.IDI_APPLICATION),
                hCursor = LoadCursor(IntPtr.Zero, SystemCursors.IDC_ARROW),
                hbrBackground = new HBRUSH(),
                lpszMenuName = IntPtr.Zero,
                lpszClassName = marshal.GetPtr(),
            };

            if (RegisterClassEx(ref wndclass) != 0)
            {
                _window = CreateWindowEx(WindowStylesEx.WS_EX_LAYERED, WindowClass, WindowName, WindowStyles.WS_OVERLAPPED, CW_USEDEFAULT, CW_USEDEFAULT,
                    CW_USEDEFAULT, CW_USEDEFAULT, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);
                if (_window != IntPtr.Zero)
                {
                    _windowDC = GetDC(_window);

                    SetPositionAndSize();
                    ShowWindow(_window, ShowWindowCommands.SW_SHOWNORMAL);

                    var startupInput = new GdiplusStartupInput
                    {
                        GdiplusVersion = 1,
                        DebugEventCallback = IntPtr.Zero,
                        SuppressBackgroundThread = false,
                        SuppressExternalCodecs = false,
                    };
                    if (GdiplusStartup(out _gdipToken, ref startupInput, out _) == GpStatus.Ok && GdipLoadImageFromFile(SplashFile, out _splashImage) == GpStatus.Ok)
                    {
                        DrawImage();
                        while (GetMessage(out var msg, IntPtr.Zero, 0, 0) != 0)
                        {
                            try
                            {
                                TranslateMessage(ref msg);
                                DispatchMessage(ref msg);
                            }
                            catch (Exception)
                            {
                                throw;
                            }
                        }
                    }
                    else
                    {
                        //TODO: throw GDI+ Error
                    }
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

        }
        finally
        {
            ReleaseAllResource();
        }
    }

    /// <summary>
    /// Set Window Position And Size
    /// </summary>
    /// <param name="dpi"></param>
    private static void SetPositionAndSize(int dpi = 0)
    {
        var screenLeft = 0;
        var screenTop = 0;
        var screenWidth = GetSystemMetrics(SystemMetric.SM_CXSCREEN);
        var screenHeight = GetSystemMetrics(SystemMetric.SM_CYSCREEN);

        var monitor = MonitorFromWindow(_window, MonitorDefaultFlags.MONITOR_DEFAULTTONULL);
        if (monitor != null)
        {
            var info = new MONITORINFOEX();
            info.cbSize = (uint) Marshal.SizeOf(info);
            if (GetMonitorInfo(monitor, ref info))
            {
                screenLeft = info.rcMonitor.left;
                screenTop = info.rcMonitor.top;
                screenWidth = info.rcMonitor.right - info.rcMonitor.left;
                screenHeight = info.rcMonitor.bottom - info.rcMonitor.top;
            }
        }

        if (dpi == 0)
        {
            var osVersion = Environment.OSVersion.Version;
            if (osVersion > new Version(10, 0, 1607))
            {
                dpi = (int) GetDpiForWindow(_window);
            }
            else
            {
                dpi = GetDeviceCaps(_windowDC, DeviceCapIndexes.LOGPIXELSX);
            }
        }


        var windowWidth = ImageWidth * dpi / 96;
        var windowHeight = ImageHeight * dpi / 96;

        SetWindowPos(_window, HWND_TOPMOST, (screenWidth - windowWidth) / 2 + screenLeft, (screenHeight - windowHeight) / 2 + screenTop, windowWidth, windowHeight, 0);
        GetWindowRect(_window, out _windowRectangle);
        _windowSize = new SIZE { cx = _windowRectangle.right - _windowRectangle.left, cy = _windowRectangle.bottom - _windowRectangle.top };
    }

    /// <summary>
    /// Draw Image
    /// </summary>
    private static void DrawImage()
    {
        if (_memoryDC != IntPtr.Zero)
        {
            DeleteDC(_memoryDC);
        }
        if (_memoryBitmap != IntPtr.Zero)
        {
            DeleteObject(_memoryBitmap);
        }

        _memoryDC = CreateCompatibleDC(_windowDC);
        _memoryBitmap = CreateCompatibleBitmap(_windowDC, _windowSize.cx, _windowSize.cy);
        SelectObject(_memoryDC, _memoryBitmap);
        if (GdipCreateFromHDC(_memoryDC, out _graphics) == GpStatus.Ok && GdipDrawImageRectI(_graphics, _splashImage, 0, 0, _windowSize.cx, _windowSize.cy) == GpStatus.Ok)
        {
            var ptSrc = new POINT
            {
                x = 0,
                y = 0,
            };
            var ptDes = new POINT
            {
                x = _windowRectangle.left,
                y = _windowRectangle.top,
            };
            var blendFunction = new BLENDFUNCTION
            {
                AlphaFormat = AC_SRC_ALPHA,
                BlendFlags = 0,
                BlendOp = AC_SRC_OVER,
                SourceConstantAlpha = 255,
            };
            if (UpdateLayeredWindow(_window, _screenDC, ref ptDes, ref _windowSize, _memoryDC, ref ptSrc, 0, ref blendFunction, UpdateLayeredWindowFlags.ULW_ALPHA))
            {
                return;
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    /// <summary>
    /// Release All Resource
    /// </summary>
    private static void ReleaseAllResource()
    {
        if (_gdipToken != null)
        {
            GdiplusShutdown(_gdipToken);
        }
        DeleteObject(_memoryBitmap);
        DeleteDC(_memoryDC);
        ReleaseDC(_window, _windowDC);
        ReleaseDC(IntPtr.Zero, _screenDC);
    }

    /// <summary>
    /// Window Proc
    /// </summary>
    /// <param name="hWnd"></param>
    /// <param name="msg"></param>
    /// <param name="wParam"></param>
    /// <param name="lParam"></param>
    /// <returns></returns>
    private static IntPtr WindowProc(IntPtr hWnd, WindowsMessages msg, UIntPtr wParam, IntPtr lParam)
    {
        try
        {
            switch (msg)
            {
                case WindowsMessages.WM_DESTROY:
                    PostQuitMessage(0);
                    return IntPtr.Zero;
                case WindowsMessages.WM_DPICHANGED:
                    SetPositionAndSize((int) (wParam.ToUInt32() >> 16));
                    DrawImage();
                    return IntPtr.Zero;
                case WindowsMessages.WM_NCHITTEST:
                    var result = DefWindowProc(hWnd, msg, wParam, lParam);
                    return result == (IntPtr) HitTestResults.HTCAPTION ? (IntPtr) HitTestResults.HTNOWHERE : result;
                default:
                    return DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }
        catch
        {
            //The finally in Main won't run if exception is thrown in this method.
            //This may be because this method was called by system code.
            //So we must handle exception here.
            DestroyWindow(_window);
            return IntPtr.Zero;
        }
    }
}