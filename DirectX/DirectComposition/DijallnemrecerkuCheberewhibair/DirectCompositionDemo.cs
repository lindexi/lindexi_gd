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
        iD3D11DeviceContext.Dispose();

        IDCompositionDevice compositionDevice = DComp.DCompositionCreateDevice3<IDCompositionDevice>(iD3D11Device);
        compositionDevice.CreateTargetForHwnd(window, topmost: true, out IDCompositionTarget compositionTarget);

        RECT windowRect;
        PInvoke.GetClientRect(window, &windowRect);
        var clientSize = new SizeI(windowRect.right - windowRect.left, windowRect.bottom - windowRect.top);

        IDCompositionVisual compositionVisual = compositionDevice.CreateVisual();
        IDCompositionSurface surface = compositionDevice.CreateVirtualSurface((uint) clientSize.Width,
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
        WINDOW_EX_STYLE exStyle = WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP;

        var style = WNDCLASS_STYLES.CS_OWNDC | WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW;

        var defaultCursor = PInvoke.LoadCursor(
            new HINSTANCE(IntPtr.Zero), new PCWSTR(PInvoke.IDC_ARROW.Value));

        var className = $"lindexi-{Guid.NewGuid().ToString()}";
        var title = "The Title";
        _wndProcDelegate = new WNDPROC(WndProc);
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
    private List<D2DRenderInfo>? _renderList;

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