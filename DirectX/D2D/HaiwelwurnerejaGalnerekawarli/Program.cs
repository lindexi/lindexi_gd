using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using static Windows.Win32.UI.WindowsAndMessaging.PEEK_MESSAGE_REMOVE_TYPE;
using static Windows.Win32.UI.WindowsAndMessaging.WNDCLASS_STYLES;
using static Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE;
using static Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE;
using static Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX;
using static Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD;
using Vortice.Mathematics;
using AlphaMode = Vortice.DXGI.AlphaMode;
using D3D = Vortice.Direct3D;
using D3D11 = Vortice.Direct3D11;
using DXGI = Vortice.DXGI;
using D2D = Vortice.Direct2D1;
using System.Drawing;
using Vortice.Direct2D1;
using System.Numerics;
using Windows.Win32;
using Windows.Win32.UI.Input.Pointer;
using Vortice.DCommon;

namespace HaiwelwurnerejaGalnerekawarli;

class Program
{
    // 设置可以支持 Win7 和以上版本。如果用到 WinRT 可以设置为支持 win10 和以上。这个特性只是给 VS 看的，没有实际影响运行的逻辑
    [SupportedOSPlatform("Windows7.0")]
    static unsafe void Main(string[] args)
    {
        // 准备创建窗口
        // 使用 Win32 创建窗口需要很多参数，这些参数系列不是本文的重点，还请自行了解
        SizeI clientSize = new SizeI(1000, 600);

        // 窗口标题
        var title = "HaiwelwurnerejaGalnerekawarli";
        var windowClassName = "lindexi doubi";

        // 窗口样式，窗口样式含义请执行参阅官方文档，样式只要不离谱，自己随便写，影响不大
        WINDOW_STYLE style = WS_CAPTION |
                             WS_SYSMENU |
                             WS_MINIMIZEBOX |
                             WS_CLIPSIBLINGS |
                             WS_BORDER |
                             WS_DLGFRAME |
                             WS_THICKFRAME |
                             WS_GROUP |
                             WS_TABSTOP |
                             WS_SIZEBOX;

        var rect = new RECT
        {
            right = clientSize.Width,
            bottom = clientSize.Height
        };

        // Adjust according to window styles
        AdjustWindowRectEx(&rect, style, false, WS_EX_APPWINDOW);

        // 决定窗口在哪显示，这个不影响大局
        int x = 0;
        int y = 0;
        int windowWidth = rect.right - rect.left;
        int windowHeight = rect.bottom - rect.top;

        // 随便，放在屏幕中间好了。多个显示器？忽略
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);

        x = (screenWidth - windowWidth) / 2;
        y = (screenHeight - windowHeight) / 2;

        var hInstance = GetModuleHandle((string?) null);

        fixed (char* lpszClassName = windowClassName)
        {
            PCWSTR szCursorName = new((char*) IDC_ARROW);

            var wndClassEx = new WNDCLASSEXW
            {
                cbSize = (uint) Unsafe.SizeOf<WNDCLASSEXW>(),
                style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC,
                // 核心逻辑，设置消息循环
                lpfnWndProc = new WNDPROC(WndProc),
                hInstance = (HINSTANCE) hInstance.DangerousGetHandle(),
                hCursor = LoadCursor((HINSTANCE) IntPtr.Zero, szCursorName),
                hbrBackground = (Windows.Win32.Graphics.Gdi.HBRUSH) IntPtr.Zero,
                hIcon = (HICON) IntPtr.Zero,
                lpszClassName = lpszClassName
            };

            ushort atom = RegisterClassEx(wndClassEx);

            if (atom == 0)
            {
                throw new InvalidOperationException(
                    $"Failed to register window class. Error: {Marshal.GetLastWin32Error()}"
                );
            }
        }

        // 创建窗口
        var hWnd = CreateWindowEx
        (
            WS_EX_APPWINDOW,
            windowClassName,
            title,
            style,
            x,
            y,
            windowWidth,
            windowHeight,
            hWndParent: default,
            hMenu: default,
            hInstance: default,
            lpParam: null
        );

        // 创建完成，那就显示
        ShowWindow(hWnd, SW_NORMAL);
        RECT windowRect;
        GetClientRect(hWnd, &windowRect);
        clientSize = new SizeI(windowRect.right - windowRect.left, windowRect.bottom - windowRect.top);

        var result = D2D1.D2D1CreateFactory(FactoryType.SingleThreaded, out ID2D1Factory? d2D1Factory);
        result.CheckError();
        Debug.Assert(d2D1Factory != null, nameof(d2D1Factory) + " != null");

        var d2D1HwndRenderTarget = d2D1Factory.CreateHwndRenderTarget(new RenderTargetProperties(PixelFormat.Premultiplied), new HwndRenderTargetProperties()
        {
            Hwnd = hWnd.Value,
            PixelSize = clientSize,
            PresentOptions = PresentOptions.Immediately
        });
        var renderTarget = d2D1HwndRenderTarget;

        var pointList = new List<Point2D>();

        var screenTranslate = new Point(0, 0);
        PInvoke.ClientToScreen(hWnd, ref screenTranslate);

        var color = new Color4(0xFF0000FF);
        var brush = renderTarget.CreateSolidColorBrush(color);

        // 开个消息循环等待
        Windows.Win32.UI.WindowsAndMessaging.MSG msg;
        while (true)
        {
            if (GetMessage(out msg, hWnd, 0, 0))
            {
                if (msg.message is PInvoke.WM_POINTERDOWN or PInvoke.WM_POINTERUPDATE or PInvoke.WM_POINTERUP)
                {
                    var wparam = msg.wParam;
                    var pointerId = (uint) (ToInt32((IntPtr) wparam.Value) & 0xFFFF);
                    PInvoke.GetPointerTouchInfo(pointerId, out var info);
                    POINTER_INFO pointerInfo = info.pointerInfo;

                    global::Windows.Win32.Foundation.RECT pointerDeviceRect = default;
                    global::Windows.Win32.Foundation.RECT displayRect = default;

                    PInvoke.GetPointerDeviceRects(pointerInfo.sourceDevice, &pointerDeviceRect, &displayRect);

                    var point2D = new Point2D(
                        pointerInfo.ptHimetricLocationRaw.X / (double) pointerDeviceRect.Width * displayRect.Width +
                        displayRect.left,
                        pointerInfo.ptHimetricLocationRaw.Y / (double) pointerDeviceRect.Height * displayRect.Height +
                        displayRect.top);

                    point2D = new Point2D(point2D.X - screenTranslate.X, point2D.Y - screenTranslate.Y);

                    lock (pointList)
                    {
                        pointList.Add(point2D);
                        if (pointList.Count > 300)
                        {
                            // 不要让点太多，导致绘制速度太慢
                            pointList.RemoveRange(0, 150);
                        }

                        _pointListUpdated = 1;
                    }

                    renderTarget.BeginDraw();
                    renderTarget.Clear(new Color4(0xFFFFFFFF));

                    for (var i = 1; i < pointList.Count && pointList.Count > 1; i++)
                    {
                        var previousPoint = pointList[i - 1];
                        var currentPoint = pointList[i];

                        renderTarget.DrawLine(new Vector2((float) previousPoint.X, (float) previousPoint.Y),
                            new Vector2((float) currentPoint.X, (float) currentPoint.Y), brush, 5);
                    }
                    renderTarget.EndDraw();
                }

                _ = TranslateMessage(&msg);
                _ = DispatchMessage(&msg);

                if (msg.message is WM_QUIT or WM_CLOSE or WM_DESTROY)
                {
                    return;
                }
            }
        }
    }

    private static int _pointListUpdated;

    private static int ToInt32(IntPtr ptr) => IntPtr.Size == 4 ? ptr.ToInt32() : (int) (ptr.ToInt64() & 0xffffffff);

    private static IEnumerable<DXGI.IDXGIAdapter1> GetHardwareAdapter(DXGI.IDXGIFactory2 factory)
    {
        DXGI.IDXGIFactory6? factory6 = factory.QueryInterfaceOrNull<DXGI.IDXGIFactory6>();
        if (factory6 != null)
        {
            // 先告诉系统，要高性能的显卡
            for (uint adapterIndex = 0;
                 factory6.EnumAdapterByGpuPreference(adapterIndex, DXGI.GpuPreference.Unspecified,
                     out DXGI.IDXGIAdapter1? adapter).Success;
                 adapterIndex++)
            {
                if (adapter == null)
                {
                    continue;
                }

                DXGI.AdapterDescription1 desc = adapter.Description1;

                if ((desc.Flags & DXGI.AdapterFlags.Software) != DXGI.AdapterFlags.None)
                {
                    // Don't select the Basic Render Driver adapter.
                    adapter.Dispose();
                    continue;
                }

                Console.WriteLine($"枚举到 {adapter.Description1.Description} 显卡");
                yield return adapter;
            }

            factory6.Dispose();
        }

        // 如果枚举不到，那系统返回啥都可以
        for (uint adapterIndex = 0;
             factory.EnumAdapters1(adapterIndex, out DXGI.IDXGIAdapter1? adapter).Success;
             adapterIndex++)
        {
            DXGI.AdapterDescription1 desc = adapter.Description1;

            if ((desc.Flags & DXGI.AdapterFlags.Software) != DXGI.AdapterFlags.None)
            {
                // Don't select the Basic Render Driver adapter.
                adapter.Dispose();

                continue;
            }

            Console.WriteLine($"枚举到 {adapter.Description1.Description} 显卡");
            yield return adapter;
        }
    }

    private static LRESULT WndProc(HWND hWnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        return DefWindowProc(hWnd, message, wParam, lParam);
    }

    readonly record struct Point2D(double X, double Y);
}