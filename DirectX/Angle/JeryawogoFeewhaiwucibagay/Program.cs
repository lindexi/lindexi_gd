// See https://aka.ms/new-console-template for more information

using JeryawogoFeewhaiwucibagay.Diagnostics;
using JeryawogoFeewhaiwucibagay.OpenGL;
using JeryawogoFeewhaiwucibagay.OpenGL.Angle;
using JeryawogoFeewhaiwucibagay.OpenGL.Egl;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectComposition;
using Vortice.DXGI;
using Vortice.Mathematics;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

using static Windows.Win32.PInvoke;

namespace JeryawogoFeewhaiwucibagay;

class Program
{
    [STAThread]
    static unsafe void Main(string[] args)
    {
        var demoWindow = new DemoWindow();
        demoWindow.Run();

        Console.ReadLine();
    }
}

class DemoWindow
{
    public DemoWindow()
    {
        var window = CreateWindow();
        HWND = window;
        ShowWindow(window, SHOW_WINDOW_CMD.SW_MAXIMIZE);

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

            if (!getMessageResult || getMessageResult < 0)
            {
                break;
            }

            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
    }

    private unsafe HWND CreateWindow()
    {
        DwmIsCompositionEnabled(out var compositionEnabled);

        if (!compositionEnabled)
        {
            Console.WriteLine($"无法启用透明窗口效果");
        }

        // 要求 Layered 窗口仅仅是为了显示透明窗口，详细请参阅 [Vortice 使用 DirectComposition 显示透明窗口 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/19541356 )
        WINDOW_EX_STYLE exStyle = WINDOW_EX_STYLE.WS_EX_OVERLAPPEDWINDOW
                                  | WINDOW_EX_STYLE.WS_EX_LAYERED; // Layered 是透明窗口的最关键

        // 如果你想做无边框：
        //exStyle |= WINDOW_EX_STYLE.WS_EX_TOOLWINDOW; // 可选
        //exStyle |= WINDOW_EX_STYLE.WS_EX_TRANSPARENT; // 点击穿透可选

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
                lpfnWndProc = new WNDPROC(WndProc),
                hInstance = new HINSTANCE(GetModuleHandle(null).DangerousGetHandle()),
                hCursor = defaultCursor,
                hbrBackground = new HBRUSH(IntPtr.Zero),
                lpszClassName = new PCWSTR(pClassName)
            };
            ushort atom = RegisterClassEx(in wndClassEx);

            var dwStyle = WINDOW_STYLE.WS_OVERLAPPEDWINDOW;
            // 去掉最大化按钮和可调边框
            //dwStyle &= ~(WINDOW_STYLE.WS_MAXIMIZEBOX | WINDOW_STYLE.WS_THICKFRAME | WINDOW_STYLE.WS_CAPTION);
            // 保留最小化按钮
            //dwStyle |= WINDOW_STYLE.WS_MINIMIZEBOX;

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

    private LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        switch ((WindowsMessage) message)
        {
            case WindowsMessage.WM_NCCALCSIZE:
                {
                    return new LRESULT(0);
                }
            case WindowsMessage.WM_SIZE:
                {
                    _renderManager?.ReSize();
                    break;
                }
        }

        return DefWindowProc(hwnd, message, wParam, lParam);
    }
}

unsafe class RenderManager(HWND hwnd) : IDisposable
{
    public HWND HWND => hwnd;

    public void StartRenderThread()
    {
        var thread = new Thread(() =>
        {
            RenderCore();
        })
        {
            IsBackground = true,
            Name = "Render"
        };

        thread.Start();
    }

    private void RenderCore()
    {
        Init();

        SkiaRenderDemo? skiaRenderDemo = null;
        while (!_isDisposed)
        {
            if (_isReSize)
            {
                // 处理窗口大小变化
                _isReSize = false;

                if (_renderInfo is not null)
                {
                    _renderInfo.Value.Dispose();
                    _renderInfo = null;
                }

                GetClientRect(HWND, out var pClientRect);
                var clientSize = new SizeI(pClientRect.right - pClientRect.left, pClientRect.bottom - pClientRect.top);

                var swapChain = _renderContext.SwapChain;

                swapChain.ResizeBuffers(2,
                    (uint) (clientSize.Width),
                    (uint) (clientSize.Height),
                   Format.B8G8R8A8_UNorm,
                    SwapChainFlags.None
                );

                _renderContext = _renderContext with
                {
                    WindowWidth = (uint) clientSize.Width,
                    WindowHeight = (uint) clientSize.Height
                };
            }

            if (_renderInfo is null)
            {
                var d3D11Texture2D = _renderContext.SwapChain.GetBuffer<ID3D11Texture2D>(0);

                EglSurface surface =
                _renderContext.AngleWin32EglDisplay.WrapDirect3D11Texture(d3D11Texture2D.NativePointer, 0, 0,
                    (int) _renderContext.WindowWidth, (int) _renderContext.WindowHeight);

                _renderInfo = new RenderInfo()
                {
                    EglSurface = surface,
                    D3D11Texture2D = d3D11Texture2D,
                };

                skiaRenderDemo = null;
            }

            // 渲染代码写在这里

            using (StepPerformanceCounter.RenderThreadCounter.StepStart("Render"))
            {
                var eglInterface = _renderContext.AngleWin32EglDisplay.EglInterface;
                var eglDisplay = _renderContext.AngleWin32EglDisplay;

                EglSurface eglSurface = _renderInfo.Value.EglSurface;

                using var makeCurrent = _renderContext.EglContext.MakeCurrent(eglSurface);

                eglInterface.WaitClient();
                eglInterface.WaitGL();
                eglInterface.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);

                var eglContext = _renderContext.EglContext;
                eglContext.GlInterface.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, 0);

                using (StepPerformanceCounter.RenderThreadCounter.StepStart("RenderCore"))
                {
                    eglContext.GlInterface.GetIntegerv(GlConsts.GL_FRAMEBUFFER_BINDING, out var fb);

                    var colorType = SKColorType.Bgra8888;

                    var grContext = _renderContext.GRContext;
                    grContext.ResetContext();

                    var maxSamples = grContext.GetMaxSurfaceSampleCount(colorType);

                    var glInfo = new GRGlFramebufferInfo((uint) fb, colorType.ToGlSizedFormat());

                    using (var renderTarget = new GRBackendRenderTarget((int) _renderContext.WindowWidth,
                               (int) _renderContext.WindowHeight, maxSamples, eglDisplay.StencilSize, glInfo))
                    {
                        var surfaceProperties = new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal);

                        using (var skSurface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.TopLeft,
                                   colorType,
                                   surfaceProperties))
                        {
                            using (var skCanvas = skSurface.Canvas)
                            {
                                //skCanvas.Clear(new SKColor((uint) Random.Shared.Next()).WithAlpha(0x2C));
                                //skCanvas.Clear(SKColors.Red);
                                skCanvas.Clear();

                                if (skiaRenderDemo is null)
                                {
                                    skiaRenderDemo = new SkiaRenderDemo(new SizeI((int)_renderContext.WindowWidth,
                                        (int)_renderContext.WindowHeight));
                                }

                                skiaRenderDemo.Draw(skCanvas);
                            }
                        }
                    }

                    using (StepPerformanceCounter.RenderThreadCounter.StepStart("GR Context.Flush"))
                    {
                        // 将会在这里等待垂直同步
                        grContext.Flush();
                    }
                }

                eglContext.GlInterface.Flush();
                eglInterface.WaitGL();
                eglSurface.SwapBuffers();

                eglInterface.WaitClient();
                eglInterface.WaitGL();
                eglInterface.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);
            }

            using (StepPerformanceCounter.RenderThreadCounter.StepStart("SwapChain"))
            {
                _renderContext.SwapChain.Present(1, PresentFlags.None);
            }
            //result.CheckError();
        }
    }

    public void ReSize()
    {
        _isReSize = true;
    }

    private bool _isReSize;

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

        Format colorFormat = Format.B8G8R8A8_UNorm;

        // 缓存的数量，包括前缓存。大部分应用来说，至少需要两个缓存，这个玩过游戏的伙伴都知道
        const int FrameCount = 2;
        SwapChainDescription1 swapChainDescription = new()
        {
            Width = (uint) clientSize.Width,
            Height = (uint) clientSize.Height,
            Format = colorFormat,                       // B8G8R8A8_UNorm
            BufferCount = FrameCount,
            BufferUsage = Usage.RenderTargetOutput,
            SampleDescription = SampleDescription.Default,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential, // 使用 FlipSequential 配合 Composition
            AlphaMode = AlphaMode.Premultiplied,
            Flags = SwapChainFlags.None,
        };

        // 使用 CreateSwapChainForComposition 创建支持预乘 Alpha 的 SwapChain
        IDXGISwapChain1 swapChain =
            dxgiFactory2.CreateSwapChainForComposition(d3D11Device1, swapChainDescription);
        // 和直接 CreateSwapChainForHwnd 的不同仅仅是为了 AlphaMode.Premultiplied 支持背景透明才引入 DirectComposition 的支持
        // 详细请参阅 [Vortice 使用 DirectComposition 显示透明窗口 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/19541356 )

        // 创建 DirectComposition 设备和目标
        IDXGIDevice dxgiDevice = d3D11Device1.QueryInterface<IDXGIDevice>();
        IDCompositionDevice compositionDevice = DComp.DCompositionCreateDevice<IDCompositionDevice>(dxgiDevice);
        compositionDevice.CreateTargetForHwnd(HWND, true, out IDCompositionTarget compositionTarget);

        // 创建视觉对象并设置 SwapChain 作为内容
        IDCompositionVisual compositionVisual = compositionDevice.CreateVisual();
        compositionVisual.SetContent(swapChain);
        compositionTarget.SetRoot(compositionVisual);
        compositionDevice.Commit();

        dxgiDevice.Dispose();

        // 不要被按下 alt+enter 进入全屏
        dxgiFactory2.MakeWindowAssociation(HWND, WindowAssociationFlags.IgnoreAltEnter | WindowAssociationFlags.IgnorePrintScreen);

        var egl = new Win32AngleEglInterface();
        var angleDevice = egl.CreateDeviceANGLE(EglConsts.EGL_D3D11_DEVICE_ANGLE, d3D11Device1.NativePointer, null);
        var display = egl.GetPlatformDisplayExt(EglConsts.EGL_PLATFORM_DEVICE_EXT, angleDevice, null);

        var angleWin32EglDisplay = new AngleWin32EglDisplay(display, egl);

        EglContext eglContext = angleWin32EglDisplay.CreateContext();

        using var makeCurrent = eglContext.MakeCurrent();

        var grGlInterface = GRGlInterface.CreateGles(proc =>
        {
            var procAddress = eglContext.GlInterface.GetProcAddress(proc);
            return procAddress;
        });

        var grContext = GRContext.CreateGl(grGlInterface, new GRContextOptions()
        {
            AvoidStencilBuffers = true
        });

        _renderContext = new RenderContext()
        {
            DXGIFactory2 = dxgiFactory2,
            HardwareAdapter = hardwareAdapter,
            D3D11Device1 = d3D11Device1,
            D3D11DeviceContext1 = d3D11DeviceContext1,
            SwapChain = swapChain,
            CompositionDevice = compositionDevice,
            CompositionTarget = compositionTarget,
            CompositionVisual = compositionVisual,

            AngleDevice = angleDevice,
            AngleDisplay = display,
            AngleWin32EglDisplay = angleWin32EglDisplay,
            EglContext = eglContext,
            GRGlInterface = grGlInterface,
            GRContext = grContext,

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

    private RenderInfo? _renderInfo;

    public void Dispose()
    {
        _renderContext.Dispose();
        _isDisposed = true;
    }

    private bool _isDisposed;
}

readonly record struct RenderInfo(ID3D11Texture2D D3D11Texture2D, EglSurface EglSurface) : IDisposable
{
    public void Dispose()
    {
        EglSurface.Dispose();
        D3D11Texture2D.Dispose();
    }
};

readonly record struct RenderContext(IDXGIFactory2 DXGIFactory2, IDXGIAdapter1 HardwareAdapter, ID3D11Device1 D3D11Device1, ID3D11DeviceContext1 D3D11DeviceContext1, IDXGISwapChain1 SwapChain,
    IDCompositionDevice CompositionDevice,
    IDCompositionTarget CompositionTarget,
    IDCompositionVisual CompositionVisual, IntPtr AngleDevice, IntPtr AngleDisplay, AngleWin32EglDisplay AngleWin32EglDisplay, EglContext EglContext, GRGlInterface GRGlInterface, GRContext GRContext) : IDisposable
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

        CompositionVisual.Dispose();
        CompositionTarget.Dispose();
        CompositionDevice.Dispose();
    }
}

record SkiaRenderDemo(SizeI ClientSize)
{
    // 此为调试代码，绘制一些矩形条
    private List<RenderInfo>? _renderList;

    public void Draw(SKCanvas canvas)
    {
        var rectWeight = 10;
        var rectHeight = 20;

        var margin = 5;

        if (_renderList is null)
        {
            // 如果是空，那就执行初始化
            _renderList = new List<RenderInfo>();
            using var colorEnumerator = GetColor().GetEnumerator();

            for (int top = margin; top < ClientSize.Height - rectHeight - margin; top += rectHeight + margin)
            {
                var skRect = new SKRect(margin, top, margin + rectWeight, top + rectHeight);

                colorEnumerator.MoveNext();
                var color = colorEnumerator.Current;
                var step = Random.Shared.Next(1, 20);
                var renderInfo = new RenderInfo(skRect, step, color);

                _renderList.Add(renderInfo);
            }
        }

        using var skPaint = new SKPaint();
        skPaint.Style = SKPaintStyle.Fill;
        for (var i = 0; i < _renderList.Count; i++)
        {
            var renderInfo = _renderList[i];
            skPaint.Color = renderInfo.Color;

            canvas.DrawRect(renderInfo.Rect, skPaint);

            var nextRect = renderInfo.Rect with
            {
                Right = renderInfo.Rect.Right + renderInfo.Step
            };
            if (nextRect.Right > ClientSize.Width - margin)
            {
                nextRect = nextRect with
                {
                    Right = nextRect.Left + rectWeight
                };
            }

            _renderList[i] = renderInfo with
            {
                Rect = nextRect
            };
        }
    }

    private readonly record struct RenderInfo(SKRect Rect, int Step, SKColor Color);

    private IEnumerable<SKColor> GetColor()
    {
        yield return SKColors.Red;
        yield return SKColors.Green;
        yield return SKColors.Blue;

        while (true)
        {
            var color = new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF);
            yield return color;
        }
    }
}