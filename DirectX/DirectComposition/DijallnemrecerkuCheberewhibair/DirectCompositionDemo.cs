using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Vortice.DCommon;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectComposition;
using Vortice.DXGI;
using Vortice.Mathematics;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

using AlphaMode = Vortice.DXGI.AlphaMode;
using D2D = Vortice.Direct2D1;

namespace DijallnemrecerkuCheberewhibair;

[SupportedOSPlatform("windows6.1")]
class DirectCompositionDemo
{
    public unsafe void Run()
    {
        var window = CreateWindow();
        PInvoke.ShowWindow(window, SHOW_WINDOW_CMD.SW_MAXIMIZE);

        var result = D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.BgraSupport,
            featureLevels: [], out ID3D11Device iD3D11Device, out var feature,
            out ID3D11DeviceContext iD3D11DeviceContext);
        result.CheckError();

        _ = feature;
        iD3D11DeviceContext.Dispose(); // 用不着就先释放。释放不代表立刻回收资源，只是表示业务层不需要用到，减少引用计数

        IDCompositionDevice compositionDevice = DComp.DCompositionCreateDevice3<IDCompositionDevice>(iD3D11Device);
        compositionDevice.CreateTargetForHwnd(window, topmost: true, out IDCompositionTarget compositionTarget);
        // 以上的 CreateTargetForHwnd 参数 topmost 不是值窗口置顶，而是：如果视觉树应显示在由 hwnd 参数指定的窗口子元素之上，则为 TRUE；否则，视觉树将显示在子元素之后
        // > TRUE if the visual tree should be displayed on top of the children of the window specified by the hwnd parameter; otherwise, the visual tree is displayed behind the children.
        // https://learn.microsoft.com/en-us/windows/win32/api/dcomp/nf-dcomp-idcompositiondevice-createtargetforhwnd

        // 创建视觉对象
        RECT windowRect;
        PInvoke.GetClientRect(window, &windowRect);
        var clientSize = new SizeI(windowRect.right - windowRect.left, windowRect.bottom - windowRect.top);

        IDCompositionVisual compositionVisual = compositionDevice.CreateVisual();
        IDCompositionVirtualSurface surface = compositionDevice.CreateVirtualSurface((uint) clientSize.Width,
            (uint) clientSize.Height, Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied);
        compositionVisual.SetContent(surface);

        compositionTarget.SetRoot(compositionVisual);
        compositionDevice.Commit();

        Vortice.Direct2D1.ID2D1Factory1 d2DFactory = Vortice.Direct2D1.D2D1.D2D1CreateFactory<Vortice.Direct2D1.ID2D1Factory1>();

        var d2DRenderDemo = new D2DRenderDemo();

        while (!_isMainWindowClosed)
        {
            using IDXGISurface dxgiSurface = surface.BeginDraw<IDXGISurface>(null, out var updateOffset);
            _ = updateOffset;

            var renderTargetProperties = new Vortice.Direct2D1.RenderTargetProperties()
            {
                PixelFormat = new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
                Type = Vortice.Direct2D1.RenderTargetType.Hardware,
            };

            using Vortice.Direct2D1.ID2D1RenderTarget d2D1RenderTarget =
                d2DFactory.CreateDxgiSurfaceRenderTarget(dxgiSurface, renderTargetProperties);

            Vortice.Direct2D1.ID2D1RenderTarget renderTarget = d2D1RenderTarget;

            renderTarget.BeginDraw();

            // 在这里编写绘制逻辑
            renderTarget.Clear(new Color4(0f));
            d2DRenderDemo.Draw(renderTarget, clientSize);

            renderTarget.EndDraw();
            surface.EndDraw();

            compositionDevice.Commit();
            compositionDevice.WaitForCommitCompletion();

            while (true)
            {
                var success = PInvoke.PeekMessage(out var message, window, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE);
                if (!success)
                {
                    break;
                }

                PInvoke.TranslateMessage(&message);
                PInvoke.DispatchMessage(&message);
            }
        }
    }

    private bool _isMainWindowClosed;

    private unsafe HWND CreateWindow()
    {
        PInvoke.DwmIsCompositionEnabled(out var compositionEnabled);

        if (!compositionEnabled)
        {
            Console.WriteLine($"无法启用透明窗口效果");
        }

        // [Windows 窗口样式 什么是 WS_EX_NOREDIRECTIONBITMAP 样式](https://blog.lindexi.com/post/Windows-%E7%AA%97%E5%8F%A3%E6%A0%B7%E5%BC%8F-%E4%BB%80%E4%B9%88%E6%98%AF-WS_EX_NOREDIRECTIONBITMAP-%E6%A0%B7%E5%BC%8F.html )
        WINDOW_EX_STYLE exStyle = WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP;

        var style = WNDCLASS_STYLES.CS_OWNDC | WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW;

        var defaultCursor = PInvoke.LoadCursor(
            new HINSTANCE(IntPtr.Zero), new PCWSTR(PInvoke.IDC_ARROW.Value));

        var className = $"lindexi-{Guid.NewGuid().ToString()}";
        var title = "The Title";
        _wndProcDelegate = new WNDPROC(WndProc); // 仅用于防止 GC 回收。详细请看 https://github.com/lindexi/lindexi_gd/pull/85
        fixed (char* pClassName = className)
        fixed (char* pTitle = title)
        {
            var wndClassEx = new WNDCLASSEXW
            {
                cbSize = (uint) Marshal.SizeOf<WNDCLASSEXW>(),
                style = style,
                lpfnWndProc = _wndProcDelegate,
                hInstance = new HINSTANCE(PInvoke.GetModuleHandle(null).DangerousGetHandle()),
                hCursor = defaultCursor,
                hbrBackground = new HBRUSH(IntPtr.Zero),
                lpszClassName = new PCWSTR(pClassName)
            };
            ushort atom = PInvoke.RegisterClassEx(in wndClassEx);

            var dwStyle = WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_VISIBLE;

            var windowHwnd = PInvoke.CreateWindowEx(
                exStyle,
                new PCWSTR((char*) atom),
                new PCWSTR(pTitle),
                dwStyle,
                0, 0, 1900, 1000,
                HWND.Null, HMENU.Null, HINSTANCE.Null, null);

            return windowHwnd;
        }
    }

    private WNDPROC? _wndProcDelegate;

    private LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        if (message == PInvoke.WM_CLOSE)
        {
            _isMainWindowClosed = true;
        }

        return PInvoke.DefWindowProc(hwnd, message, wParam, lParam);
    }
}

class D2DRenderDemo
{
    // 此为调试代码，绘制一些矩形条
    private List<D2DRenderInfo>? _renderList;

    public void OnReSize()
    {
        _renderList = null;
    }

    public void Draw(D2D.ID2D1RenderTarget renderTarget, SizeI clientSize)
    {
        var rectWeight = 10;
        var rectHeight = 20;

        var margin = 5;

        if (_renderList is null)
        {
            _renderList = new List<D2DRenderInfo>();

            for (int top = margin; top < clientSize.Height - rectHeight - margin; top += rectHeight + margin)
            {
                Rect rect = new Rect(margin, top, rectWeight, rectHeight);

                var color = new Color4(Random.Shared.NextSingle(), Random.Shared.NextSingle(),
                    Random.Shared.NextSingle());
                var step = Random.Shared.Next(1, 20);

                var renderInfo = new D2DRenderInfo(rect, step, color);
                _renderList.Add(renderInfo);
            }
        }

        for (var i = 0; i < _renderList.Count; i++)
        {
            var renderInfo = _renderList[i];
            using var brush = renderTarget.CreateSolidColorBrush(renderInfo.Color);

            renderTarget.FillRectangle(renderInfo.Rect, brush);

            var nextRect = renderInfo.Rect with
            {
                Width = renderInfo.Rect.Width + renderInfo.Step
            };

            if (nextRect.Width > clientSize.Width - margin * 2)
            {
                nextRect = nextRect with
                {
                    Width = rectWeight
                };
            }

            _renderList[i] = renderInfo with
            {
                Rect = nextRect
            };
        }
    }

    private readonly record struct D2DRenderInfo(Rect Rect, int Step, Color4 Color);
}