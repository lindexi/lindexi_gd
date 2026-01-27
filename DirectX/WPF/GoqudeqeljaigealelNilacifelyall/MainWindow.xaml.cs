using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.Direct3D9;
using Vortice.DXGI;
using Vortice.Mathematics;

using DXGIFormat = Vortice.DXGI.Format;

namespace GoqudeqeljaigealelNilacifelyall
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        ~MainWindow()
        {
            Dispose();
        }

        private IDirect3DDevice9Ex? _d3D9Device;
        private ID2D1RenderTarget? _d2DRenderTarget;
        private ID3D11Device? _d3D11Device;

        private IDirect3DTexture9? _renderTarget;

        private float _x;

        private float _y;

        private float _xDirection = 1;

        private float _yDirection = 1;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var width = Math.Max((uint) Image.Width, 100);
            var height = Math.Max((uint) Image.Height, 100);

            ID3D11Device device =
                D3D11.D3D11CreateDevice(Vortice.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            _d3D11Device = device;

            var desc = new Texture2DDescription()
            {
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = DXGIFormat.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                MiscFlags = ResourceOptionFlags.Shared,
                CPUAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };
            ID3D11Texture2D renderTarget = device.CreateTexture2D(desc);

            var surface = renderTarget.QueryInterface<IDXGISurface>();
            var d2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory>();
            var renderTargetProperties =
                new RenderTargetProperties(new Vortice.DCommon.PixelFormat(DXGIFormat.B8G8R8A8_UNorm,
                    Vortice.DCommon.AlphaMode.Premultiplied));
            _d2DRenderTarget = d2dFactory.CreateDxgiSurfaceRenderTarget(surface, renderTargetProperties);
            SetRenderTarget(renderTarget);
            device.ImmediateContext.RSSetViewport(0, 0, (float) width, (float) height);

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            if (_d2DRenderTarget is null)
            {
                return;
            }

            _d2DRenderTarget.BeginDraw();

            OnRender(_d2DRenderTarget);

            _d2DRenderTarget.EndDraw();
            _d3D11Device?.ImmediateContext.Flush();

            D3DImage.Lock();

            D3DImage.AddDirtyRect(new Int32Rect(0, 0, D3DImage.PixelWidth, D3DImage.PixelHeight));
            D3DImage.Unlock();

            Image.InvalidateVisual();
        }

        private void OnRender(ID2D1RenderTarget renderTarget)
        {
            using var brush = renderTarget.CreateSolidColorBrush(new Color4(Random.Shared.Next() | 0xFF << 24 /*确保 A 是不透明的*/));

            renderTarget.Clear(null);

            const int size = 10;

            renderTarget.DrawRectangle(new Vortice.RawRectF(left: _x, top: _y, right: _x + size, bottom: _y + size), brush);

            _x = _x + _xDirection * Random.Shared.Next(size);
            _y = _y + _yDirection * Random.Shared.Next(size);

            var minX = 0;
            var maxX = D3DImage.Width - size;
            var minY = 0;
            var maxY = D3DImage.Height - size;
            if (_x >= maxX || _x <= minX)
            {
                _xDirection = -_xDirection;

                _x = (float) Math.Clamp(_x, minX, maxX);
            }

            if (_y >= maxY || _y <= minY)
            {
                _yDirection = -_yDirection;

                _y = (float) Math.Clamp(_y, minY, maxY);
            }
        }

        private static Vortice.Direct3D9.Format TranslateFormat(ID3D11Texture2D texture)
        {
            switch (texture.Description.Format)
            {
                case DXGIFormat.R10G10B10A2_UNorm:
                    return Vortice.Direct3D9.Format.A2B10G10R10;
                case DXGIFormat.R16G16B16A16_Float:
                    return Vortice.Direct3D9.Format.A16B16G16R16F;
                case DXGIFormat.B8G8R8A8_UNorm:
                    return Vortice.Direct3D9.Format.A8R8G8B8;
                default:
                    return Vortice.Direct3D9.Format.Unknown;
            }
        }

        private IntPtr GetSharedHandle(ID3D11Texture2D texture)
        {
            using (var resource = texture.QueryInterface<IDXGIResource>())
            {
                return resource.SharedHandle;
            }
        }

        private static Vortice.Direct3D9.PresentParameters GetPresentParameters()
        {
            var presentParams = new Vortice.Direct3D9.PresentParameters();

            presentParams.Windowed = true;
            presentParams.SwapEffect = Vortice.Direct3D9.SwapEffect.Discard;
            presentParams.DeviceWindowHandle = NativeMethods.GetDesktopWindow();
            presentParams.PresentationInterval = PresentInterval.Default;
            return presentParams;
        }

        private void SetRenderTarget(ID3D11Texture2D target)
        {
            var format = TranslateFormat(target);
            var handle = GetSharedHandle(target);

            var presentParams = GetPresentParameters();
            var createFlags = CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded |
                              CreateFlags.FpuPreserve;

            var d3DContext = D3D9.Direct3DCreate9Ex();
            // 以下代码强行获取第 0 个适配器，可能会在多显卡等情况下导致问题。如设置 CPU 的 CpuAccessFlags 为 Read 等无权限问题
            using IDirect3DDevice9Ex d3DDevice =
                d3DContext.CreateDeviceEx(adapter: 0, DeviceType.Hardware, focusWindow: IntPtr.Zero, createFlags, presentParams);
            _d3D9Device = d3DDevice;

            _renderTarget = d3DDevice.CreateTexture(target.Description.Width, target.Description.Height, 1,
                Vortice.Direct3D9.Usage.RenderTarget, format, Pool.Default, ref handle);

            using var surface = _renderTarget.GetSurfaceLevel(0);
            D3DImage.Lock();
            D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer,
                enableSoftwareFallback: true);
            D3DImage.Unlock();
        }

        public void Dispose()
        {
            // 这里不释放也没问题，在底层处理了对象回收自动释放
            _d3D9Device?.Dispose();
            _d2DRenderTarget?.Dispose();
            _d3D11Device?.Dispose();
            _renderTarget?.Dispose();
        }
    }
}

public static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = false)]
    public static extern IntPtr GetDesktopWindow();
}