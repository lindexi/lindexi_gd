// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

using Vortice.DCommon;
using Vortice.Direct2D1;
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

namespace JecekelbereLaiwharhowhelli;

class Program
{
    [STAThread]
    static unsafe void Main(string[] args)
    {
        HWND window;

        #region 创建窗口

        WINDOW_EX_STYLE exStyle = WINDOW_EX_STYLE.WS_EX_APPWINDOW;

        var style = WNDCLASS_STYLES.CS_OWNDC | WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW;

        var defaultCursor = LoadCursor(
            new HINSTANCE(IntPtr.Zero), new PCWSTR(IDC_ARROW.Value));
        var wndProcDelegate = new WNDPROC(WndProc);

        var className = $"lindexi-{Guid.NewGuid().ToString()}";
        var title = "The Title";
        fixed (char* pClassName = className)
        fixed (char* pTitle = title)
        {
            var wndClassEx = new WNDCLASSEXW
            {
                cbSize = (uint) Marshal.SizeOf<WNDCLASSEXW>(),
                style = style,
                lpfnWndProc = wndProcDelegate,
                hInstance = new HINSTANCE(GetModuleHandle(null).DangerousGetHandle()),
                hCursor = defaultCursor,
                hbrBackground = new HBRUSH(IntPtr.Zero),
                lpszClassName = new PCWSTR(pClassName)
            };
            ushort atom = RegisterClassEx(in wndClassEx);

            WINDOW_STYLE dwStyle = WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_CAPTION | WINDOW_STYLE.WS_SYSMENU | WINDOW_STYLE.WS_MINIMIZEBOX | WINDOW_STYLE.WS_CLIPCHILDREN | WINDOW_STYLE.WS_BORDER | WINDOW_STYLE.WS_DLGFRAME | WINDOW_STYLE.WS_THICKFRAME | WINDOW_STYLE.WS_TABSTOP | WINDOW_STYLE.WS_SIZEBOX;

            HWND windowHwnd = CreateWindowEx(
                exStyle,
                new PCWSTR((char*) atom),
                new PCWSTR(pTitle),
                dwStyle,
                0, 0, 1900, 1000,
                HWND.Null, HMENU.Null, HINSTANCE.Null, null);
            window = windowHwnd;
        }

        // 防止委托对象被回收，导致注册进去的方法指针失效
        GC.KeepAlive(wndProcDelegate); // 保稳来说，这句话应该放在方法末尾

        #endregion

        // 显示窗口
        ShowWindow(window, SHOW_WINDOW_CMD.SW_NORMAL);

        RECT windowRect;
        GetClientRect(window, &windowRect);
        GetClientRect(window, &windowRect);
        var clientSize = new SizeI(windowRect.right - windowRect.left, windowRect.bottom - windowRect.top);

        #region 初始化 DX 相关

        DeviceCreationFlags creationFlags = DeviceCreationFlags.BgraSupport;
        var result = D3D11.D3D11CreateDevice
        (
            null,
            DriverType.Hardware,
            creationFlags,
            null,
            out ID3D11Device? d3D11Device
        );

        result.CheckError();
        Debug.Assert(d3D11Device != null);

        // 缓存的数量，包括前缓存。大部分应用来说，至少需要两个缓存，这个玩过游戏的伙伴都知道
        const int FrameCount = 2;
        Format colorFormat = Format.B8G8R8A8_UNorm;
        SwapChainDescription1 swapChainDescription = new()
        {
            Width = (uint) clientSize.Width,
            Height = (uint) clientSize.Height,
            Format = colorFormat,
            BufferCount = FrameCount,
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

        using var dxgiFactory2 = DXGI.CreateDXGIFactory1<IDXGIFactory2>();
        using IDXGISwapChain1 swapChain =
            dxgiFactory2.CreateSwapChainForHwnd(d3D11Device, window, swapChainDescription, fullscreenDescription);

        // 不要被按下 alt+enter 进入全屏
        dxgiFactory2.MakeWindowAssociation(window,
            WindowAssociationFlags.IgnoreAltEnter | WindowAssociationFlags.IgnorePrintScreen);
        #endregion

        #region 对接 D2D 渲染

        using var d3D11Texture2D = swapChain.GetBuffer<ID3D11Texture2D>(0);
        using var dxgiSurface = d3D11Texture2D.QueryInterface<IDXGISurface>();

        var renderTargetProperties = new D2D.RenderTargetProperties()
        {
            PixelFormat = new PixelFormat(colorFormat, Vortice.DCommon.AlphaMode.Premultiplied),
            Type = D2D.RenderTargetType.Hardware,
        };

        using D2D.ID2D1Factory1 d2DFactory = D2D.D2D1.D2D1CreateFactory<D2D.ID2D1Factory1>();
        D2D.ID2D1RenderTarget d2D1RenderTarget =
            d2DFactory.CreateDxgiSurfaceRenderTarget(dxgiSurface, renderTargetProperties);
        D2D.ID2D1RenderTarget renderTarget = d2D1RenderTarget;
        #endregion

        var maxCount = 100;
        var stepTimeList = new List<TimeSpan>();
        var stopwatch = new Stopwatch();

        while (true)
        {
            renderTarget.BeginDraw();

            renderTarget.Clear(new Color4((uint) Random.Shared.Next()));

            renderTarget.EndDraw();

            stopwatch.Restart();
            swapChain.Present(1, 0);
            stopwatch.Stop();

            if (stepTimeList.Count < maxCount)
            {
                stepTimeList.Add(stopwatch.Elapsed);
            }
            else
            {
                var stringBuilder = new StringBuilder();

                for (var i = 0; i < stepTimeList.Count; i++)
                {
                    var timeSpan = stepTimeList[i];
                    stringBuilder.AppendLine($"[{i:D3}] {timeSpan.TotalMilliseconds:0.000} ms");
                }

                var costText = stringBuilder.ToString();
                Console.WriteLine(costText);

                /*
                   [000] 0.173 ms
                   [001] 0.171 ms
                   [002] 0.591 ms
                   [003] 7.008 ms
                   [004] 16.074 ms
                   [005] 17.490 ms
                   [006] 10.339 ms
                   [007] 18.348 ms
                   [008] 12.287 ms
                   [009] 15.211 ms
                   [010] 16.125 ms
                   [011] 16.110 ms
                   [012] 16.100 ms
                   [013] 2.887 ms
                   [014] 2.171 ms
                   [015] 2.223 ms
                   [016] 2.365 ms
                   [017] 15.942 ms
                   [018] 15.915 ms
                   [019] 15.896 ms
                   [020] 15.930 ms
                   [021] 15.950 ms
                   [022] 15.903 ms
                   [023] 15.602 ms
                   [024] 16.525 ms
                   [025] 15.597 ms
                   [026] 15.878 ms
                   [027] 15.908 ms
                   [028] 15.912 ms
                   [029] 16.046 ms
                   [030] 15.938 ms
                 */

                Console.ReadLine();
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

        LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
        {
            return DefWindowProc(hwnd, message, wParam, lParam);
        }
    }
}