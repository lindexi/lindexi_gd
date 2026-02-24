// See https://aka.ms/new-console-template for more information

using KearjerijarqaloChurharcarwaya.Diagnostics;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

using Vortice.DCommon;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectComposition;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.Win32;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.Pointer;
using Windows.Win32.UI.WindowsAndMessaging;

using static Windows.Win32.PInvoke;

using AlphaMode = Vortice.DXGI.AlphaMode;
using Color = Vortice.Mathematics.Color;
using D2D = Vortice.Direct2D1;

namespace KearjerijarqaloChurharcarwaya;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(8, 1))
        {
            return;
        }

        var demoWindow = new DemoWindow();
        demoWindow.Run();

        Console.ReadLine();
    }
}

[SupportedOSPlatform("windows8.1")]
class DemoWindow
{
    public DemoWindow()
    {
        var window = CreateWindow();
        HWND = window;

        // 让鼠标也引发 WM_Pointer 事件
        EnableMouseInPointer(true);

        // 最大化显示窗口
        ShowWindow(window, SHOW_WINDOW_CMD.SW_MAXIMIZE);

        // 独立渲染线程
        var renderManager = new RenderManager(window);
        _renderManager = renderManager;
        renderManager.StartRenderThread();
    }

    private readonly RenderManager _renderManager;

    public HWND HWND { get; }

    public unsafe void Run()
    {
        while (true)
        {
            var msg = new MSG();
            var getMessageResult = GetMessage(&msg, HWND, 0,
                0);

            if (!getMessageResult)
            {
                break;
            }

            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
    }

    /// <summary>
    /// 仅用于防止被回收
    /// </summary>
    /// <returns></returns>
    private WNDPROC? _wndProcDelegate;

    private unsafe HWND CreateWindow()
    {
        WINDOW_EX_STYLE exStyle = WINDOW_EX_STYLE.WS_EX_APPWINDOW;

        var style = WNDCLASS_STYLES.CS_OWNDC | WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW;

        var defaultCursor = LoadCursor(
            new HINSTANCE(IntPtr.Zero), new PCWSTR(IDC_ARROW.Value));

        var className = $"lindexi-{Guid.NewGuid().ToString()}";
        var title = "The Title";
        fixed (char* pClassName = className)
        fixed (char* pTitle = title)
        {
            _wndProcDelegate = new WNDPROC(WndProc);
            var wndClassEx = new WNDCLASSEXW
            {
                cbSize = (uint) Marshal.SizeOf<WNDCLASSEXW>(),
                style = style,
                lpfnWndProc = _wndProcDelegate,
                hInstance = new HINSTANCE(GetModuleHandle(null).DangerousGetHandle()),
                hCursor = defaultCursor,
                hbrBackground = new HBRUSH(IntPtr.Zero),
                lpszClassName = new PCWSTR(pClassName)
            };
            ushort atom = RegisterClassEx(in wndClassEx);

            WINDOW_STYLE dwStyle = WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_CAPTION | WINDOW_STYLE.WS_SYSMENU | WINDOW_STYLE.WS_MINIMIZEBOX | WINDOW_STYLE.WS_CLIPCHILDREN | WINDOW_STYLE.WS_BORDER | WINDOW_STYLE.WS_DLGFRAME | WINDOW_STYLE.WS_THICKFRAME | WINDOW_STYLE.WS_TABSTOP | WINDOW_STYLE.WS_SIZEBOX;

            var windowHwnd = CreateWindowEx(
                exStyle,
                new PCWSTR((char*) atom),
                new PCWSTR(pTitle),
                dwStyle,
                0, 0, 1900, 1000,
                HWND.Null, HMENU.Null, HINSTANCE.Null, null);

            return windowHwnd;
        }
    }

    private unsafe LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        if (message == WM_POINTERUPDATE /*Pointer Update*/)
        {
            var pointerId = (uint) (ToInt32(wParam) & 0xFFFF);

            global::Windows.Win32.Foundation.RECT pointerDeviceRect = default;
            global::Windows.Win32.Foundation.RECT displayRect = default;

            GetPointerTouchInfo(pointerId, out POINTER_TOUCH_INFO pointerTouchInfo);

            var pointerInfo = pointerTouchInfo.pointerInfo;

            GetPointerDeviceRects(pointerInfo.sourceDevice, &pointerDeviceRect, &displayRect);

            var x =
                pointerInfo.ptHimetricLocationRaw.X / (double) pointerDeviceRect.Width * displayRect.Width +
                displayRect.left;
            var y = pointerInfo.ptHimetricLocationRaw.Y / (double) pointerDeviceRect.Height * displayRect.Height +
                    displayRect.top;

            var screenTranslate = new Point(0, 0);
            ClientToScreen(HWND, ref screenTranslate);

            x -= screenTranslate.X;
            y -= screenTranslate.Y;

            _renderManager.Move(x, y);
        }

        return DefWindowProc(hwnd, message, wParam, lParam);
    }

    private static int ToInt32(WPARAM wParam) => ToInt32((IntPtr) wParam.Value);
    private static int ToInt32(IntPtr ptr) => IntPtr.Size == 4 ? ptr.ToInt32() : (int) (ptr.ToInt64() & 0xffffffff);
}

[SupportedOSPlatform("windows8.1")]
unsafe class RenderManager(HWND hwnd) : IDisposable
{
    public HWND HWND => hwnd;
    private readonly Format _colorFormat = Format.B8G8R8A8_UNorm;
    private Format D2DColorFormat => _colorFormat;

    /// <summary>
    /// 缓存的数量，包括前缓存。大部分应用来说，至少需要两个缓存，这个玩过游戏的伙伴都知道
    /// </summary>
    private const int FrameCount = 2;

    public void StartRenderThread()
    {
        var thread = new Thread(() => { RenderCore(); })
        {
            IsBackground = true,
            Name = "Render"
        };
        thread.Priority = ThreadPriority.Highest;
        thread.Start();
    }

    private void RenderCore()
    {
        Init();

        using D2D.ID2D1Factory1 d2DFactory = D2D.D2D1.D2D1CreateFactory<D2D.ID2D1Factory1>();

        IDXGISwapChain2 swapChain2 = _renderContext.SwapChain;
        var d3D11Texture2D = swapChain2.GetBuffer<ID3D11Texture2D>(0);
        using var dxgiSurface = d3D11Texture2D.QueryInterface<IDXGISurface>();
        var renderTargetProperties = new D2D.RenderTargetProperties()
        {
            PixelFormat = new PixelFormat(D2DColorFormat, Vortice.DCommon.AlphaMode.Premultiplied),
            Type = D2D.RenderTargetType.Hardware,
        };

        D2D.ID2D1RenderTarget d2D1RenderTarget =
            d2DFactory.CreateDxgiSurfaceRenderTarget(dxgiSurface, renderTargetProperties);

        var waitableObject = swapChain2.FrameLatencyWaitableObject;

        using var brush = d2D1RenderTarget.CreateSolidColorBrush(Colors.Yellow);

        while (!_isDisposed)
        {
            using (StepPerformanceCounter.RenderThreadCounter.StepStart("FrameLatencyWaitableObject"))
            {
                WaitForSingleObjectEx(new HANDLE(waitableObject), 1000, true);
            }

            // 渲染代码写在这里

            using (StepPerformanceCounter.RenderThreadCounter.StepStart("Render"))
            {
                D2D.ID2D1RenderTarget renderTarget = d2D1RenderTarget;

                renderTarget.BeginDraw();

                renderTarget.Clear(Colors.White);

                var position = _position;

                renderTarget.FillRectangle(new Rect((float) position.X, (float) position.Y, 20, 20), brush);

                renderTarget.EndDraw();
            }

            using (StepPerformanceCounter.RenderThreadCounter.StepStart("SwapChain"))
            {
                swapChain2.Present(1, PresentFlags.None);
            }
        }
    }

    private void Init()
    {
        RECT windowRect;
        GetClientRect(HWND, &windowRect);
        var clientSize = new SizeI(windowRect.right - windowRect.left, windowRect.bottom - windowRect.top);

        var dxgiFactory2 = DXGI.CreateDXGIFactory1<IDXGIFactory2>();

        IDXGIAdapter1? hardwareAdapter = GetHardwareAdapter(dxgiFactory2)
            // 这里 ToList 只是想列出所有的 IDXGIAdapter1 在实际代码里，大部分都是获取第一个
            .ToList().FirstOrDefault();
        if (hardwareAdapter == null)
        {
            throw new InvalidOperationException("Cannot detect D3D11 adapter");
        }

        FeatureLevel[] featureLevels = new[]
        {
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1,
            FeatureLevel.Level_10_0,
            FeatureLevel.Level_9_3,
            FeatureLevel.Level_9_2,
            FeatureLevel.Level_9_1,
        };

        IDXGIAdapter1 adapter = hardwareAdapter;
        DeviceCreationFlags creationFlags = DeviceCreationFlags.BgraSupport;
        var result = D3D11.D3D11CreateDevice
        (
            adapter,
            DriverType.Unknown,
            creationFlags,
            featureLevels,
            out ID3D11Device d3D11Device, out FeatureLevel featureLevel,
            out ID3D11DeviceContext d3D11DeviceContext
        );
        _ = featureLevel;

        if (result.Failure)
        {
            // 如果失败了，那就不指定显卡，走 WARP 的方式
            // http://go.microsoft.com/fwlink/?LinkId=286690
            result = D3D11.D3D11CreateDevice(
                IntPtr.Zero,
                DriverType.Warp,
                creationFlags,
                featureLevels,
                out d3D11Device, out featureLevel, out d3D11DeviceContext);

            // 如果失败，就不能继续
            result.CheckError();
        }

        // 大部分情况下，用的是 ID3D11Device1 和 ID3D11DeviceContext1 类型
        // 从 ID3D11Device 转换为 ID3D11Device1 类型
        ID3D11Device1 d3D11Device1 = d3D11Device.QueryInterface<ID3D11Device1>();
        var d3D11DeviceContext1 = d3D11DeviceContext.QueryInterface<ID3D11DeviceContext1>();

        // 获取到了新的两个接口，就可以减少 `d3D11Device` 和 `d3D11DeviceContext` 的引用计数。调用 Dispose 不会释放掉刚才申请的 D3D 资源，只是减少引用计数
        d3D11Device.Dispose();
        d3D11DeviceContext.Dispose();

        SwapChainDescription1 swapChainDescription = new()
        {
            Width = (uint) clientSize.Width,
            Height = (uint) clientSize.Height,
            Format = _colorFormat,
            BufferCount = FrameCount,
            BufferUsage = Usage.RenderTargetOutput,
            SampleDescription = SampleDescription.Default,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential, // 使用 FlipSequential 配合 Composition
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.FrameLatencyWaitableObject, // 核心设置
        };

        var fullscreenDescription = new SwapChainFullscreenDescription()
        {
            Windowed = true,
        };
        IDXGISwapChain1 swapChain1 = dxgiFactory2.CreateSwapChainForHwnd(d3D11Device1, HWND, swapChainDescription, fullscreenDescription);

        IDXGISwapChain2 swapChain2 = swapChain1.QueryInterface<IDXGISwapChain2>();
        swapChain1.Dispose();

        swapChain2.MaximumFrameLatency = 1;
        var waitableObject = swapChain2.FrameLatencyWaitableObject;
        _ = waitableObject;
        // 可以通过 WaitForSingleObjectEx 进行等待

        // 不要被按下 alt+enter 进入全屏
        dxgiFactory2.MakeWindowAssociation(HWND,
            WindowAssociationFlags.IgnoreAltEnter | WindowAssociationFlags.IgnorePrintScreen);

        _renderContext = _renderContext with
        {
            DXGIFactory2 = dxgiFactory2,
            HardwareAdapter = hardwareAdapter,
            D3D11Device1 = d3D11Device1,
            D3D11DeviceContext1 = d3D11DeviceContext1,
            SwapChain = swapChain2,

            WindowWidth = swapChainDescription.Width,
            WindowHeight = swapChainDescription.Height
        };
    }

    private static IEnumerable<IDXGIAdapter1> GetHardwareAdapter(IDXGIFactory2 factory)
    {
        using IDXGIFactory6? factory6 = factory.QueryInterfaceOrNull<IDXGIFactory6>();
        if (factory6 != null)
        {
            // 这个系统的 DX 支持 IDXGIFactory6 类型
            // 先告诉系统，要高性能的显卡
            for (uint adapterIndex = 0;
                 factory6.EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance,
                     out IDXGIAdapter1? adapter).Success;
                 adapterIndex++)
            {
                if (adapter == null)
                {
                    continue;
                }

                AdapterDescription1 desc = adapter.Description1;
                if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
                {
                    // Don't select the Basic Render Driver adapter.
                    adapter.Dispose();
                    continue;
                }

                Console.WriteLine($"枚举到 {adapter.Description1.Description} 显卡");
                yield return adapter;
            }
        }
        else
        {
            // 不支持就不支持咯，用旧版本的方式获取显示适配器接口
        }

        // 如果枚举不到，那系统返回啥都可以
        for (uint adapterIndex = 0;
             factory.EnumAdapters1(adapterIndex, out IDXGIAdapter1? adapter).Success;
             adapterIndex++)
        {
            AdapterDescription1 desc = adapter.Description1;

            if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
            {
                // Don't select the Basic Render Driver adapter.
                adapter.Dispose();

                continue;
            }

            Console.WriteLine($"枚举到 {adapter.Description1.Description} 显卡");
            yield return adapter;
        }
    }

    private RenderContext _renderContext;

    public void Dispose()
    {
        _renderContext.Dispose();
        _isDisposed = true;
    }

    private bool _isDisposed;

    public void Move(double x, double y)
    {
        _position = new Position(x, y);
    }

    private Position _position = new Position(0, 0);

    /// <summary>
    /// 表示当前的位置
    /// </summary>
    /// <param name="X"></param>
    /// <param name="Y"></param>
    /// <remarks>
    /// 为什么需要选用 record 引用 class 类型，而不是 struct 结构体值类型？这是为了在渲染线程和 UI 线程之间共享这个位置数据。由于 record class 是引用类型，所以在两个线程之间共享时，不需要担心值类型的复制问题，完全原子化，不存在多线程安全问题
    /// </remarks>
    record Position(double X, double Y);
}


readonly record struct RenderContext(
    IDXGIFactory2 DXGIFactory2,
    IDXGIAdapter1 HardwareAdapter,
    ID3D11Device1 D3D11Device1,
    ID3D11DeviceContext1 D3D11DeviceContext1,
    IDXGISwapChain2 SwapChain) : IDisposable
{
    public uint WindowWidth { get; init; }
    public uint WindowHeight { get; init; }

    public void Dispose()
    {
        DXGIFactory2.Dispose();
        HardwareAdapter.Dispose();
        D3D11Device1.Dispose();
        D3D11DeviceContext1.Dispose();
        SwapChain.Dispose();
    }
}