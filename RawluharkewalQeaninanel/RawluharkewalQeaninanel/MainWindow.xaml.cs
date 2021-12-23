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
using D2D = SharpDX.Direct2D1;
using SharpDXDXGI = SharpDX.DXGI;
using SharpDXMathematics = SharpDX.Mathematics.Interop;

namespace RawluharkewalQeaninanel
{
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

            // 2021.12.23 不能在 x86 下运行，会炸掉。参阅 https://github.com/dotnet/Silk.NET/issues/731

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
            D3D11.D3D11 d3D11 = D3D11.D3D11.GetApi();

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

            Debugger.Launch();
            Debugger.Break();

            _pD3D11Device = pD3D11Device;
            _pD3D11DeviceContext = pD3D11DeviceContext;

            D3D11.ID3D11Texture2D* pD3D11Texture2D;
            hr = pD3D11Device->CreateTexture2D(ref texture2DDesc, (D3D11.SubresourceData*) IntPtr.Zero, &pD3D11Texture2D);
            SilkMarshal.ThrowHResult(hr);

            var renderTarget = pD3D11Texture2D;
            _pD3D11Texture2D = pD3D11Texture2D;

            DXGI.IDXGISurface* pDXGISurface;
            var dxgiSurfaceGuid = DXGI.IDXGISurface.Guid;
            renderTarget->QueryInterface(ref dxgiSurfaceGuid, (void**) &pDXGISurface);
            _pDXGISurface = pDXGISurface;

            var d2DFactory = new D2D.Factory();

            var renderTargetProperties =
                new D2D.RenderTargetProperties(new D2D.PixelFormat(SharpDXDXGI.Format.Unknown, D2D.AlphaMode.Premultiplied));
            var surface = new SharpDXDXGI.Surface(new IntPtr((void*) pDXGISurface));
            _d2DRenderTarget = new D2D.RenderTarget(d2DFactory, surface, renderTargetProperties);

            SetRenderTarget(renderTarget);

            var viewport = new D3D11.Viewport(0, 0, width, height, 0, 1);
            pD3D11DeviceContext->RSSetViewports(NumViewports: 1, ref viewport);

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            _d2DRenderTarget.BeginDraw();

            OnRender(_d2DRenderTarget);

            _d2DRenderTarget.EndDraw();

            D3DImage.Lock();

            D3DImage.AddDirtyRect(new Int32Rect(0, 0, D3DImage.PixelWidth, D3DImage.PixelHeight));

            D3DImage.Unlock();
        }

        private void OnRender(D2D.RenderTarget renderTarget)
        {
            var brush = new D2D.SolidColorBrush(_d2DRenderTarget, new SharpDXMathematics.RawColor4(1, 0, 0, 1));

            renderTarget.Clear(null);

            renderTarget.DrawRectangle(new SharpDXMathematics.RawRectangleF(_x, _y, _x + 10, _y + 10), brush);

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
        private float _dx = 1;
        private float _dy = 1;

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
                SwapEffect = D3D9.Swapeffect.SwapeffectDiscard,
                HDeviceWindow = GetDesktopWindow(),
                PresentationInterval = D3D9.D3D9.PresentIntervalDefault,
            };

            // 设置使用多线程方式，这样的性能才足够
            uint createFlags = D3D9.D3D9.CreateHardwareVertexprocessing | D3D9.D3D9.CreateMultithreaded | D3D9.D3D9.CreateFpuPreserve;

            D3D9.IDirect3DDevice9Ex* pDirect3DDevice9Ex;
            hr = d3DContext->CreateDeviceEx(Adapter: 0, 
                DeviceType: D3D9.Devtype.DevtypeHal,// 使用硬件渲染
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
                D3D9.Format.FmtA8R8G8B8, // 这是必须要求的颜色，不能使用其他颜色
                D3D9.Pool.PoolDefault, 
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

        // 这些字段的另一个作用是防止回收
        private D2D.RenderTarget _d2DRenderTarget;

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
