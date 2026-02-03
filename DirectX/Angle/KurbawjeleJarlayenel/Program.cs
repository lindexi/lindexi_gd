using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using KurbawjeleJarlayenel.Diagnostics;
using KurbawjeleJarlayenel.OpenGL;
using KurbawjeleJarlayenel.OpenGL.Angle;
using KurbawjeleJarlayenel.OpenGL.Egl;
using SkiaSharp;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Windows.Win32.PInvoke;

namespace KurbawjeleJarlayenel;

class Program
{
    [STAThread]
    static unsafe void Main(string[] args)
    {
        // 创建窗口
        var window = CreateWindow();
        // 显示窗口
        ShowWindow(window, SHOW_WINDOW_CMD.SW_NORMAL);

        // 初始化渲染
        // 初始化 DX 相关
        #region 初始化 DX 相关

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

        result.CheckError();

        // 大部分情况下，用的是 ID3D11Device1 和 ID3D11DeviceContext1 类型
        // 从 ID3D11Device 转换为 ID3D11Device1 类型
        ID3D11Device1 d3D11Device1 = d3D11Device.QueryInterface<ID3D11Device1>();
        var d3D11DeviceContext1 = d3D11DeviceContext.QueryInterface<ID3D11DeviceContext1>();
        _ = d3D11DeviceContext1;

        // 获取到了新的两个接口，就可以减少 `d3D11Device` 和 `d3D11DeviceContext` 的引用计数。调用 Dispose 不会释放掉刚才申请的 D3D 资源，只是减少引用计数
        d3D11Device.Dispose();
        d3D11DeviceContext.Dispose();

        RECT windowRect;
        GetClientRect(window, &windowRect);
        var clientSize = new SizeI(windowRect.right - windowRect.left, windowRect.bottom - windowRect.top);

        // 颜色格式有要求，才能和 Angle 正确交互
        Format colorFormat = Format.B8G8R8A8_UNorm;

        // 缓存的数量，包括前缓存。大部分应用来说，至少需要两个缓存，这个玩过游戏的伙伴都知道
        const int frameCount = 2;
        SwapChainDescription1 swapChainDescription = new()
        {
            Width = (uint)clientSize.Width,
            Height = (uint)clientSize.Height,
            Format = colorFormat, // B8G8R8A8_UNorm
            BufferCount = frameCount,
            BufferUsage = Usage.RenderTargetOutput,
            SampleDescription = SampleDescription.Default,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.None,
        };

        var fullscreenDescription = new SwapChainFullscreenDescription()
        {
            Windowed = true,
        };

        IDXGISwapChain1 swapChain =
            dxgiFactory2.CreateSwapChainForHwnd(d3D11Device1, window, swapChainDescription, fullscreenDescription);

        // 不要被按下 alt+enter 进入全屏
        dxgiFactory2.MakeWindowAssociation(window,
            WindowAssociationFlags.IgnoreAltEnter | WindowAssociationFlags.IgnorePrintScreen);

        #endregion

        // 初始化 Angle 和 OpenGL 相关

        #region 初始化 Angle 相关

        var egl = new Win32AngleEglInterface();
        // 传入 ID3D11Device1 的指针，将 D3D11 设备和 AngleDevice 绑定
        var angleDevice = egl.CreateDeviceANGLE(EglConsts.EGL_D3D11_DEVICE_ANGLE, d3D11Device1.NativePointer, null);
        var display = egl.GetPlatformDisplayExt(EglConsts.EGL_PLATFORM_DEVICE_EXT, angleDevice, null);

        var angleWin32EglDisplay = new AngleWin32EglDisplay(display, egl);

        EglContext eglContext = angleWin32EglDisplay.CreateContext();

        var makeCurrent = eglContext.MakeCurrent();

        var grGlInterface = GRGlInterface.CreateGles(proc =>
        {
            var procAddress = eglContext.GlInterface.GetProcAddress(proc);
            return procAddress;
        });

        var grContext = GRContext.CreateGl(grGlInterface, new GRContextOptions()
        {
            AvoidStencilBuffers = true
        });
        makeCurrent.Dispose();

        #endregion

        // 通过 Angle 关联 DX 和 OpenGL 纹理

        #region 关联 DX 和 OpenGL 纹理

        // 先从交换链取出渲染目标纹理
        ID3D11Texture2D d3D11Texture2D = swapChain.GetBuffer<ID3D11Texture2D>(0);
        Debug.Assert(d3D11Texture2D.Description.Width == clientSize.Width);
        Debug.Assert(d3D11Texture2D.Description.Height == clientSize.Height);

        // 关键代码： 通过 eglCreatePbufferFromClientBuffer 将 D3D11 纹理包装为 EGLSurface
        // 这一步的前置是在 eglCreateDeviceANGLE 里面将 ID3D11Texture2D 所在的 D3D11 设备关联： `egl.CreateDeviceANGLE(EglConsts.EGL_D3D11_DEVICE_ANGLE, d3D11Device1.NativePointer, null)`
        EglSurface eglSurface =
            angleWin32EglDisplay.WrapDirect3D11Texture(d3D11Texture2D.NativePointer, 0, 0,
                (int)d3D11Texture2D.Description.Width, (int)d3D11Texture2D.Description.Height);

        // 后续 Skia 也许会使用 Graphite 的 Dawn 支持 D3D 而不是 EGL 的方式
        // > Current plans for Graphite are to support D3D11 and D3D12 through the Dawn backend.
        // 详细请看
        // https://groups.google.com/g/skia-discuss/c/WY7yzRjGGFA
        // > Ganesh和Graphite是两组技术，Ganesh更老更稳定，Graphite更新、更快（多线程支持更好）、更不稳定，但它是趋势，是Skia团队的主攻方向。Chrome已经在个别地方使用Graphite了
        // https://zhuanlan.zhihu.com/p/20265941170

        #endregion

        SkiaRenderDemo renderDemo = new(clientSize);

        while (true)
        {
            // 界面渲染
            using var step = StepPerformanceCounter.RenderThreadCounter.StepStart("Render");

            // 以下是每次画面渲染时都要执行的逻辑
            // 将 EGLSurface 绑定到 Skia 上
            using (eglContext.MakeCurrent(eglSurface))
            {
                EglInterface eglInterface = angleWin32EglDisplay.EglInterface;
                Debug.Assert(ReferenceEquals(egl, eglInterface));

                eglInterface.WaitClient();
                eglInterface.WaitGL();
                eglInterface.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);

                eglContext.GlInterface.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, 0);

                eglContext.GlInterface.GetIntegerv(GlConsts.GL_FRAMEBUFFER_BINDING, out var fb);
                // 颜色格式和前面定义的 Format colorFormat = Format.B8G8R8A8_UNorm; 相对应
                var colorType = SKColorType.Bgra8888;
                // 当然，写成 SKColorType.Rgba8888 也是能被兼容的
                // https://github.com/AvaloniaUI/Avalonia/discussions/20559
                grContext.ResetContext();

                var maxSamples = grContext.GetMaxSurfaceSampleCount(colorType);

                EglDisplay eglDisplay = angleWin32EglDisplay;

                var glInfo = new GRGlFramebufferInfo((uint)fb, colorType.ToGlSizedFormat());
                // 从 OpenGL 对接到 Skia 上
                using (var renderTarget = new GRBackendRenderTarget(clientSize.Width,
                           clientSize.Height, maxSamples, eglDisplay.StencilSize, glInfo))
                {
                    var surfaceProperties = new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal);

                    using (var skSurface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.TopLeft,
                               colorType,
                               surfaceProperties))
                    {
                        using (SKCanvas skCanvas = skSurface.Canvas)
                        {
                            // 随便画内容
                            skCanvas.Clear();
                            renderDemo.Draw(skCanvas);
                        }
                    }
                }

                // 如果开启渲染同步等待，则会在这里等待
                grContext.Flush(); // 把当前挂在这个 GRContext 上、还没真正提交到 GPU 后端的绘制操作，全部提交下去

                // 让 OpenGL 层刷出去
                eglContext.GlInterface.Flush();
                // 等待这些命令在 EGL/ANGLE 后端（D3D）里真正执行完
                eglInterface.WaitGL();
                eglSurface.SwapBuffers();

                eglInterface.WaitClient();
                eglInterface.WaitGL();
                eglInterface.WaitNative(EglConsts.EGL_CORE_NATIVE_ENGINE);

                // 让交换链推送
                swapChain.Present(1, PresentFlags.None);
            }

            // 以下只是为了防止窗口无响应而已
            var success = PeekMessage(out var msg, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE);
            if (success)
            {
                // 处理窗口消息
                TranslateMessage(&msg);
                DispatchMessage(&msg);
            }
        }

        Console.ReadLine();
    }

    private static unsafe HWND CreateWindow()
    {
        DwmIsCompositionEnabled(out var compositionEnabled);

        if (!compositionEnabled)
        {
            Console.WriteLine($"无法启用透明窗口效果");
        }

        WINDOW_EX_STYLE exStyle = WINDOW_EX_STYLE.WS_EX_OVERLAPPEDWINDOW | WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP;

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
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                style = style,
                lpfnWndProc = new WNDPROC(WndProc),
                hInstance = new HINSTANCE(GetModuleHandle(null).DangerousGetHandle()),
                hCursor = defaultCursor,
                hbrBackground = new HBRUSH(IntPtr.Zero),
                lpszClassName = new PCWSTR(pClassName)
            };
            ushort atom = RegisterClassEx(in wndClassEx);

            var dwStyle = WINDOW_STYLE.WS_OVERLAPPEDWINDOW;

            var windowHwnd = CreateWindowEx(
                exStyle,
                new PCWSTR((char*)atom),
                new PCWSTR(pTitle),
                dwStyle,
                0, 0, 1900, 1000,
                HWND.Null, HMENU.Null, HINSTANCE.Null, null);

            return windowHwnd;
        }

        static LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
        {
            WindowsMessage windowsMessage = (WindowsMessage)message;
            if (windowsMessage == WindowsMessage.WM_CLOSE)
            {
                Environment.Exit(0);
            }

            return DefWindowProc(hwnd, message, wParam, lParam);
        }
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

            yield return adapter;
        }
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

            for (int top = margin; top < ClientSize.Height - rectHeight - margin; top += rectHeight + margin)
            {
                var skRect = new SKRect(margin, top, margin + rectWeight, top + rectHeight);
                var color = new SKColor((uint)Random.Shared.Next()).WithAlpha(0xFF);
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
}