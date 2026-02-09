// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using Vortice.DCommon;
using Vortice.DirectComposition;
using static Windows.Win32.PInvoke;
using AlphaMode = Vortice.DXGI.AlphaMode;
using D2D = Vortice.Direct2D1;

namespace DijallnemrecerkuCheberewhibair;

class Program
{
    [STAThread]
    static unsafe void Main(string[] args)
    {
        var window = CreateWindow();
        ShowWindow(window, SHOW_WINDOW_CMD.SW_MAXIMIZE);

        var result = D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.BgraSupport,
            featureLevels: [], out ID3D11Device iD3D11Device, out var feature,
            out ID3D11DeviceContext iD3D11DeviceContext);
        result.CheckError();

        _ = feature;
        iD3D11DeviceContext.Dispose();

        IDCompositionDevice compositionDevice = DComp.DCompositionCreateDevice3<IDCompositionDevice>(iD3D11Device);
        compositionDevice.CreateTargetForHwnd(window, topmost: true, out IDCompositionTarget compositionTarget);

        // 创建视觉对象
        RECT windowRect;
        GetClientRect(window, &windowRect);
        var clientSize = new SizeI(windowRect.right - windowRect.left, windowRect.bottom - windowRect.top);

        IDCompositionVisual compositionVisual = compositionDevice.CreateVisual();
        IDCompositionVirtualSurface surface = compositionDevice.CreateVirtualSurface((uint)clientSize.Width,
            (uint)clientSize.Height, Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied);
        compositionVisual.SetContent(surface);

        compositionTarget.SetRoot(compositionVisual);
        compositionDevice.Commit();

        D2D.ID2D1Factory1 d2DFactory = D2D.D2D1.D2D1CreateFactory<D2D.ID2D1Factory1>();

        var d2DRenderDemo = new D2DRenderDemo();

        while (true)
        {
            using IDXGISurface dxgiSurface = surface.BeginDraw<IDXGISurface>(null, out var updateOffset);
            _ = updateOffset;

            var renderTargetProperties = new D2D.RenderTargetProperties()
            {
                PixelFormat = new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
                Type = D2D.RenderTargetType.Hardware,
            };

            using D2D.ID2D1RenderTarget d2D1RenderTarget =
                d2DFactory.CreateDxgiSurfaceRenderTarget(dxgiSurface, renderTargetProperties);

            D2D.ID2D1RenderTarget renderTarget = d2D1RenderTarget;

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
                var success = PeekMessage(out var message, window, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE);
                if (!success)
                {
                    break;
                }

                TranslateMessage(&message);
                DispatchMessage(&message);
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

        // [Windows 窗口样式 什么是 WS_EX_NOREDIRECTIONBITMAP 样式](https://blog.lindexi.com/post/Windows-%E7%AA%97%E5%8F%A3%E6%A0%B7%E5%BC%8F-%E4%BB%80%E4%B9%88%E6%98%AF-WS_EX_NOREDIRECTIONBITMAP-%E6%A0%B7%E5%BC%8F.html )
        WINDOW_EX_STYLE exStyle = WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP;

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

            var dwStyle = WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_VISIBLE;

            var windowHwnd = CreateWindowEx(
                exStyle,
                new PCWSTR((char*)atom),
                new PCWSTR(pTitle),
                dwStyle,
                0, 0, 1900, 1000,
                HWND.Null, HMENU.Null, HINSTANCE.Null, null);

            return windowHwnd;
        }

        LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
        {
            return DefWindowProc(hwnd, message, wParam, lParam);
        }
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