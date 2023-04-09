#nullable disable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using Silk.NET.Core.Native;

using D3D11 = Silk.NET.Direct3D11;
using D3D9 = Silk.NET.Direct3D9;
using DXGI = Silk.NET.DXGI;
using D2D = Silk.NET.Direct2D;
using Silk.NET.Direct2D;
using Silk.NET.Maths;
using Silk.NET.Direct3D11;
using Silk.NET.Core.Contexts;
using Silk.NET.DXGI;

namespace RawluharkewalQeaninanel
{
    class X : INativeWindowSource
    {
        public INativeWindow Native => throw new NotImplementedException();
    }

    class X1 : INativeWindow
    {
        public NativeWindowFlags Kind => throw new NotImplementedException();

        public (nint Display, nuint Window)? X11 => throw new NotImplementedException();

        public nint? Cocoa => throw new NotImplementedException();

        public (nint Display, nint Surface)? Wayland => throw new NotImplementedException();

        public nint? WinRT => throw new NotImplementedException();

        public (nint Window, uint Framebuffer, uint Colorbuffer, uint ResolveFramebuffer)? UIKit => throw new NotImplementedException();

        public (nint Hwnd, nint HDC, nint HInstance)? Win32 => throw new NotImplementedException();

        public (nint Display, nint Window)? Vivante => throw new NotImplementedException();

        public (nint Window, nint Surface)? Android => throw new NotImplementedException();

        public nint? Glfw => throw new NotImplementedException();

        public nint? Sdl => throw new NotImplementedException();

        public nint? DXHandle => throw new NotImplementedException();

        public (nint? Display, nint? Surface)? EGL => throw new NotImplementedException();
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public unsafe partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 根据 [Surface sharing between Windows graphics APIs - Win32 apps](https://docs.microsoft.com/en-us/windows/win32/direct3darticles/surface-sharing-between-windows-graphics-apis?WT.mc_id=WD-MVP-5003260 ) 文档

            var width = ImageWidth;
            var height = ImageHeight;

            var texture2DDesc = new D3D11.Texture2DDesc()
            {
                BindFlags = (uint) (D3D11.BindFlag.BindRenderTarget | D3D11.BindFlag.BindShaderResource),
                Format = DXGI.Format.FormatB8G8R8A8Unorm, // 最好使用此格式，否则还需要后续转换
                Width = (uint) width,
                Height = (uint) height,
                MipLevels = 1,
                SampleDesc = new DXGI.SampleDesc(1, 0),
                Usage = D3D11.Usage.UsageDefault,
                MiscFlags = (uint) D3D11.ResourceMiscFlag.ResourceMiscShared,
                // The D3D11_RESOURCE_MISC_FLAG cannot be used when creating resources with D3D11_CPU_ACCESS flags.
                CPUAccessFlags = 0, //(uint) D3D11.CpuAccessFlag.None,
                ArraySize = 1
            };

            D3D11.ID3D11Device* pD3D11Device;
            D3D11.ID3D11DeviceContext* pD3D11DeviceContext;
            D3DFeatureLevel pD3DFeatureLevel = default;
            D3D11.D3D11 d3D11 = D3D11.D3D11.GetApi(new X());

            var hr = d3D11.CreateDevice((DXGI.IDXGIAdapter*) IntPtr.Zero, D3DDriverType.D3DDriverTypeHardware,
                Software: 0,
                Flags: (uint) D3D11.CreateDeviceFlag.CreateDeviceBgraSupport,
                (D3DFeatureLevel*) IntPtr.Zero,
                FeatureLevels: 0, // D3DFeatureLevel 的长度
                SDKVersion: 7,
                (D3D11.ID3D11Device**) &pD3D11Device, // 参阅 [C# 从零开始写 SharpDx 应用 聊聊功能等级](https://blog.lindexi.com/post/C-%E4%BB%8E%E9%9B%B6%E5%BC%80%E5%A7%8B%E5%86%99-SharpDx-%E5%BA%94%E7%94%A8-%E8%81%8A%E8%81%8A%E5%8A%9F%E8%83%BD%E7%AD%89%E7%BA%A7.html )
                ref pD3DFeatureLevel,
                (D3D11.ID3D11DeviceContext**) &pD3D11DeviceContext
            );
            SilkMarshal.ThrowHResult(hr);

            _pD3D11Device = pD3D11Device;
            _pD3D11DeviceContext = pD3D11DeviceContext;

            D3D11.ID3D11Texture2D* pD3D11Texture2D;
            hr = pD3D11Device->CreateTexture2D(texture2DDesc, (D3D11.SubresourceData*) IntPtr.Zero, &pD3D11Texture2D);
            SilkMarshal.ThrowHResult(hr);

            var renderTarget = pD3D11Texture2D;
            _pD3D11Texture2D = pD3D11Texture2D;

            DXGI.IDXGISurface* pDXGISurface;
            var dxgiSurfaceGuid = DXGI.IDXGISurface.Guid;
            renderTarget->QueryInterface(ref dxgiSurfaceGuid, (void**) &pDXGISurface);
            _pDXGISurface = pDXGISurface;

            D2D.ID2D1Factory* pD2D1Factory = (D2D.ID2D1Factory*) IntPtr.Zero;
            var d2D = D2D.D2D.GetApi();
            Guid guid = D2D.ID2D1Factory.Guid;

            hr = d2D.D2D1CreateFactory(D2D.FactoryType.SingleThreaded, ref guid, new D2D.FactoryOptions(D2D.DebugLevel.Error),
                ((void**) &pD2D1Factory));
            SilkMarshal.ThrowHResult(hr);
            _pD2D1Factory = pD2D1Factory;

            // 通过 DXGI 创建
            IDXGIDevice* pDXGIDevice = pD3D11Device->QueryInterface<IDXGIDevice>().Handle;
            ID2D1Device* pD2D1Device;
            var creationProperties = new CreationProperties(ThreadingMode.SingleThreaded, DebugLevel.Error, DeviceContextOptions.None);
            d2D.D2D1CreateDevice(pDXGIDevice, creationProperties, &pD2D1Device);
            pD2D1Device->GetFactory(&pD2D1Factory);

            _pD2D1Factory = pD2D1Factory;

            var renderTargetProperties =
            new D2D.RenderTargetProperties(pixelFormat: new D2D.PixelFormat(DXGI.Format.FormatUnknown,
                D2D.AlphaMode.Premultiplied), type: D2D.RenderTargetType.Hardware, dpiX: 96, dpiY: 96, usage: D2D.RenderTargetUsage.None, minLevel: D2D.FeatureLevel.LevelDefault);

            D2D.ID2D1RenderTarget* pD2D1RenderTarget;
            hr = pD2D1Factory->CreateDxgiSurfaceRenderTarget(pDXGISurface, renderTargetProperties, &pD2D1RenderTarget);
            SilkMarshal.ThrowHResult(hr);

            _pD2D1RenderTarget = pD2D1RenderTarget;

            SetRenderTarget(renderTarget);

            var viewport = new D3D11.Viewport(0, 0, width, height, 0, 1);
            pD3D11DeviceContext->RSSetViewports(NumViewports: 1, viewport);

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            _pD2D1RenderTarget->BeginDraw();

            OnRender();

            //var rect = new Box2D<float>(0,0,1000,1000);
            //var arg1 = (ulong*) &rect;
            var hr = _pD2D1RenderTarget->EndDraw((ulong*) IntPtr.Zero, (ulong*) IntPtr.Zero);
            SilkMarshal.ThrowHResult(hr);

            _pD3D11DeviceContext->Flush();

            D3DImage.Lock();

            D3DImage.AddDirtyRect(new Int32Rect(0, 0, D3DImage.PixelWidth, D3DImage.PixelHeight));

            D3DImage.Unlock();
        }

        private int _renderCount = 0;

        private Stopwatch _stopwatch;
        private void OnRender()
        {
            _stopwatch ??= Stopwatch.StartNew();

            _renderCount++;

            if (_stopwatch.Elapsed.TotalSeconds > 1)
            {
                Title = $"FPS: {_renderCount / _stopwatch.Elapsed.TotalSeconds}";
                _renderCount = 0;
                _stopwatch.Restart();
            }

            ID2D1RenderTarget* renderTarget = _pD2D1RenderTarget;

            var d3Dcolorvalue = new DXGI.D3Dcolorvalue(r: Random.Shared.NextSingle(), g: Random.Shared.NextSingle(), b: 0, a: 1);
            var brushProperties = new BrushProperties()
            {
                Opacity = 1f,
                Transform = Matrix3X2<float>.Identity,
            };
            ID2D1SolidColorBrush* pSolidColorBrush;
            var hr = renderTarget->CreateSolidColorBrush(d3Dcolorvalue, brushProperties, &pSolidColorBrush);
            SilkMarshal.ThrowHResult(hr);
            ID2D1Brush* pBrush = (ID2D1Brush*) pSolidColorBrush;

            renderTarget->Clear(null);

            ID2D1StrokeStyle* pStrokeStyle;
            var strokeStyleProperties = new StrokeStyleProperties()
            {
                StartCap = CapStyle.Square,
                EndCap = CapStyle.Square,
                LineJoin = LineJoin.Bevel
            };

            var pDashes = (float*) IntPtr.Zero;
            hr = _pD2D1Factory->CreateStrokeStyle(strokeStyleProperties, pDashes, 0, &pStrokeStyle);
            SilkMarshal.ThrowHResult(hr);

            for (int i = 0; i < 100; i++)
            {
                const int w = 100;
                var x = _x + Random.Shared.Next(w * 2) - w;
                var y = _y + Random.Shared.Next(w * 2) - w;
                var rect = new Box2D<float>(x, y, x + 10, y + 10);
                var pRect = &rect;

                renderTarget->DrawRectangle(pRect, pBrush, 1, pStrokeStyle);
            }

            pSolidColorBrush->Release();
            pStrokeStyle->Release();

            _x = _x + _dx;
            _y = _y + _dy;
            if (_x >= ActualWidth - 10 || _x <= 0)
            {
                _dx = -_dx;
            }

            if (_y >= ActualHeight - 10 || _y <= 0)
            {
                _dy = -_dy;
            }
        }

        private float _x;
        private float _y;
        private float _dx = 10;
        private float _dy = 10;

        private void SetRenderTarget(D3D11.ID3D11Texture2D* target)
        {
            DXGI.IDXGIResource* pDXGIResource;
            var dxgiResourceGuid = DXGI.IDXGIResource.Guid;
            target->QueryInterface(ref dxgiResourceGuid, (void**) &pDXGIResource);

            D3D11.Texture2DDesc texture2DDescription = default;
            target->GetDesc(ref texture2DDescription);

            void* sharedHandle;
            var hr = pDXGIResource->GetSharedHandle(&sharedHandle);
            SilkMarshal.ThrowHResult(hr);

            var d3d9 = D3D9.D3D9.GetApi();
            D3D9.IDirect3D9Ex* pDirect3D9Ex;
            hr = d3d9.Direct3DCreate9Ex(SDKVersion: 32, &pDirect3D9Ex);
            SilkMarshal.ThrowHResult(hr);
            var d3DContext = pDirect3D9Ex;
            _pDirect3D9Ex = pDirect3D9Ex;

            var presentParameters = new D3D9.PresentParameters()
            {
                Windowed = 1,// true
                SwapEffect = D3D9.Swapeffect.Discard,
                HDeviceWindow = GetDesktopWindow(),
                PresentationInterval = D3D9.D3D9.PresentIntervalDefault,
            };

            // 设置使用多线程方式，这样的性能才足够
            uint createFlags = D3D9.D3D9.CreateHardwareVertexprocessing | D3D9.D3D9.CreateMultithreaded | D3D9.D3D9.CreateFpuPreserve;

            D3D9.IDirect3DDevice9Ex* pDirect3DDevice9Ex;
            hr = d3DContext->CreateDeviceEx(Adapter: 0,
                DeviceType: D3D9.Devtype.Hal,// 使用硬件渲染
                hFocusWindow: IntPtr.Zero,
                createFlags,
                ref presentParameters,
                pFullscreenDisplayMode: (D3D9.Displaymodeex*) IntPtr.Zero,
                &pDirect3DDevice9Ex);
            SilkMarshal.ThrowHResult(hr);

            var d3DDevice = pDirect3DDevice9Ex;

            D3D9.IDirect3DTexture9* pDirect3DTexture9;
            hr = d3DDevice->CreateTexture(texture2DDescription.Width, texture2DDescription.Height, Levels: 1,
                D3D9.D3D9.UsageRendertarget,
                D3D9.Format.A8R8G8B8, // 这是必须要求的颜色，不能使用其他颜色
                D3D9.Pool.Default,
                &pDirect3DTexture9,
                &sharedHandle);
            SilkMarshal.ThrowHResult(hr);
            _renderTarget = pDirect3DTexture9;

            D3D9.IDirect3DSurface9* pDirect3DSurface9;
            _renderTarget->GetSurfaceLevel(0, &pDirect3DSurface9);
            _pDirect3DSurface9 = pDirect3DSurface9;

            D3DImage.Lock();
            D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, new IntPtr(pDirect3DSurface9));
            D3DImage.Unlock();
        }

        private D2D.ID2D1RenderTarget* _pD2D1RenderTarget;
        private D2D.ID2D1Factory* _pD2D1Factory;

        private D3D11.ID3D11Device* _pD3D11Device;
        private D3D11.ID3D11DeviceContext* _pD3D11DeviceContext;
        private D3D11.ID3D11Texture2D* _pD3D11Texture2D;
        private DXGI.IDXGISurface* _pDXGISurface;

        private D3D9.IDirect3D9Ex* _pDirect3D9Ex;
        private D3D9.IDirect3DTexture9* PDirect3DTexture9 => _renderTarget;
        private D3D9.IDirect3DTexture9* _renderTarget;
        private D3D9.IDirect3DSurface9* _pDirect3DSurface9;

        private int ImageWidth => (int) ActualWidth;
        private int ImageHeight => (int) ActualHeight;

        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr GetDesktopWindow();
    }
}
