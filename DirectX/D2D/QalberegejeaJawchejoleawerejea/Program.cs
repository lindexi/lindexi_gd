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
using Vortice.DCommon;
using Vortice.Mathematics;
using AlphaMode = Vortice.DXGI.AlphaMode;
using D3D = Vortice.Direct3D;
using D3D11 = Vortice.Direct3D11;
using DXGI = Vortice.DXGI;
using D2D = Vortice.Direct2D1;
using System.Drawing;
using Vortice.Direct2D1;
using System.Diagnostics;
using Vortice.WIC;
using SharpGen.Runtime;

namespace QalberegejeaJawchejoleawerejea;

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
        var title = "QalberegejeaJawchejoleawerejea";
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

        // 开始创建工厂创建 D3D 的逻辑
        var dxgiFactory2 = DXGI.DXGI.CreateDXGIFactory1<DXGI.IDXGIFactory2>();

        var hardwareAdapter = GetHardwareAdapter(dxgiFactory2)
            // 这里 ToList 只是想列出所有的 IDXGIAdapter1 在实际代码里，大部分都是获取第一个
            .ToList().FirstOrDefault();
        if (hardwareAdapter == null)
        {
            throw new InvalidOperationException("Cannot detect D3D11 adapter");
        }

        // 功能等级
        // [C# 从零开始写 SharpDx 应用 聊聊功能等级](https://blog.lindexi.com/post/C-%E4%BB%8E%E9%9B%B6%E5%BC%80%E5%A7%8B%E5%86%99-SharpDx-%E5%BA%94%E7%94%A8-%E8%81%8A%E8%81%8A%E5%8A%9F%E8%83%BD%E7%AD%89%E7%BA%A7.html)
        D3D.FeatureLevel[] featureLevels = new[]
        {
            D3D.FeatureLevel.Level_11_1,
            D3D.FeatureLevel.Level_11_0,
            D3D.FeatureLevel.Level_10_1,
            D3D.FeatureLevel.Level_10_0,
            D3D.FeatureLevel.Level_9_3,
            D3D.FeatureLevel.Level_9_2,
            D3D.FeatureLevel.Level_9_1,
        };

        DXGI.IDXGIAdapter1 adapter = hardwareAdapter;
        D3D11.DeviceCreationFlags creationFlags = D3D11.DeviceCreationFlags.BgraSupport;
        var result = D3D11.D3D11.D3D11CreateDevice
        (
            adapter,
            D3D.DriverType.Unknown,
            creationFlags,
            featureLevels,
            out D3D11.ID3D11Device d3D11Device, out D3D.FeatureLevel featureLevel,
            out D3D11.ID3D11DeviceContext d3D11DeviceContext
        );

        if (result.Failure)
        {
            // 如果失败了，那就不指定显卡，走 WARP 的方式
            // http://go.microsoft.com/fwlink/?LinkId=286690
            result = D3D11.D3D11.D3D11CreateDevice(
                IntPtr.Zero,
                D3D.DriverType.Warp,
                creationFlags,
                featureLevels,
                out d3D11Device, out featureLevel, out d3D11DeviceContext);

            // 如果失败，就不能继续
            result.CheckError();
        }

        // 大部分情况下，用的是 ID3D11Device1 和 ID3D11DeviceContext1 类型
        // 从 ID3D11Device 转换为 ID3D11Device1 类型
        var d3D11Device1 = d3D11Device.QueryInterface<D3D11.ID3D11Device1>();
        var d3D11DeviceContext1 = d3D11DeviceContext.QueryInterface<D3D11.ID3D11DeviceContext1>();

        // 后续还要创建 D2D 设备，就先不考虑释放咯
        //// 转换完成，可以减少对 ID3D11Device1 的引用计数
        //// 调用 Dispose 不会释放掉刚才申请的 D3D 资源，只是减少引用计数
        //d3D11Device.Dispose();
        //d3D11DeviceContext.Dispose();

        // 创建设备，接下来就是关联窗口和交换链
        DXGI.Format colorFormat = DXGI.Format.B8G8R8A8_UNorm;

        const int FrameCount = 2;

        DXGI.SwapChainDescription1 swapChainDescription = new()
        {
            Width = (uint) clientSize.Width,
            Height = (uint) clientSize.Height,
            Format = colorFormat,
            BufferCount = FrameCount,
            BufferUsage = DXGI.Usage.RenderTargetOutput,
            SampleDescription = DXGI.SampleDescription.Default,
            Scaling = DXGI.Scaling.Stretch,
            SwapEffect = DXGI.SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Ignore,
        };

        // 设置是否全屏
        DXGI.SwapChainFullscreenDescription fullscreenDescription = new DXGI.SwapChainFullscreenDescription
        {
            Windowed = true
        };

        // 给创建出来的窗口挂上交换链
        DXGI.IDXGISwapChain1 swapChain =
            dxgiFactory2.CreateSwapChainForHwnd(d3D11Device1, hWnd, swapChainDescription, fullscreenDescription);

        // 不要被按下 alt+enter 进入全屏
        dxgiFactory2.MakeWindowAssociation(hWnd, DXGI.WindowAssociationFlags.IgnoreAltEnter);

        D3D11.ID3D11Texture2D backBufferTexture = swapChain.GetBuffer<D3D11.ID3D11Texture2D>(0);

        // 获取到 dxgi 的平面，这个屏幕就约等于窗口渲染内容
        DXGI.IDXGISurface dxgiSurface = backBufferTexture.QueryInterface<DXGI.IDXGISurface>();

        // 对接 D2D 需要创建工厂
        D2D.ID2D1Factory1 d2DFactory = D2D.D2D1.D2D1CreateFactory<D2D.ID2D1Factory1>();

        // 方法1：
        var renderTargetProperties = new D2D.RenderTargetProperties(Vortice.DCommon.PixelFormat.Premultiplied);

        // 在窗口的 dxgi 的平面上创建 D2D 的画布，如此即可让 D2D 绘制到窗口上
        D2D.ID2D1RenderTarget d2D1RenderTarget =
            d2DFactory.CreateDxgiSurfaceRenderTarget(dxgiSurface, renderTargetProperties);

        var renderTarget = d2D1RenderTarget;

        // 方法2：
        // 创建 D2D 设备，通过设置 ID2D1DeviceContext 的 Target 输出为 dxgiSurface 从而让 ID2D1DeviceContext 渲染内容渲染到窗口上
        // 如 https://learn.microsoft.com/en-us/windows/win32/direct2d/images/devicecontextdiagram.png 图
        // 获取 DXGI 设备，用来创建 D2D 设备
        //DXGI.IDXGIDevice dxgiDevice = d3D11Device.QueryInterface<DXGI.IDXGIDevice>();
        //ID2D1Device d2dDevice = d2DFactory.CreateDevice(dxgiDevice);
        //ID2D1DeviceContext d2dDeviceContext = d2dDevice.CreateDeviceContext();

        //ID2D1Bitmap1 d2dBitmap = d2dDeviceContext.CreateBitmapFromDxgiSurface(dxgiSurface);
        //d2dDeviceContext.Target = d2dBitmap;

        //var renderTarget = d2dDeviceContext;

        var stopwatch = Stopwatch.StartNew();
        var count = 0;

        Task.Factory.StartNew(() =>
        {
            while (true)
            {
                // 开始绘制逻辑
                renderTarget.BeginDraw();

                // 清空画布
                renderTarget.Clear(new Color4(0xFFFFFFFF));

                // 随意创建颜色
                var color = new Color4((uint) Random.Shared.Next());
                using var brush = renderTarget.CreateSolidColorBrush(color);
                renderTarget.FillEllipse(new Ellipse(new System.Numerics.Vector2(200, 200), 100, 100), brush);

                renderTarget.EndDraw();

                swapChain.Present(1, DXGI.PresentFlags.None);
                // 等待刷新
                d3D11DeviceContext.Flush();

                // 统计刷新率
                count++;
                if (stopwatch.Elapsed >= TimeSpan.FromSeconds(1))
                {
                    Console.WriteLine($"FPS: {count / stopwatch.Elapsed.TotalSeconds}");
                    stopwatch.Restart();
                    count = 0;
                }
            }
        }, TaskCreationOptions.LongRunning);

        // 开个消息循环等待
        Windows.Win32.UI.WindowsAndMessaging.MSG msg;
        while (true)
        {
            if (PeekMessage(out msg, default, 0, 0, PM_REMOVE) != false)
            {
                _ = TranslateMessage(&msg);
                _ = DispatchMessage(&msg);

                if (msg.message is WM_QUIT or WM_CLOSE)
                {
                    return;
                }
            }
        }
    }

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
}