using SharpDX.Direct3D11;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SharpDX.Direct2D1;
using SharpDX.Direct3D9;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using static NaferfairqeLidajekawnal.Direct3D;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Buffer = System.Buffer;
using Format = SharpDX.DXGI.Format;
using PixelFormat = System.Windows.Media.PixelFormat;

using D2D = SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Mathematics.Interop;
using DXGI = SharpDX.DXGI;
using D3D11 = SharpDX.Direct3D11;
using D3D9 = SharpDX.Direct3D9;

namespace NaferfairqeLidajekawnal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;

            Top = 0;
            Left = 0;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hr = Direct3DCreate9Ex(D3D_SDK_VERSION, out var direct3D9Ex);
            m_d3dEx = direct3D9Ex;

            var adapterMonitor = direct3D9Ex.GetAdapterMonitor(0);
            m_hWnd = GetDesktopWindow();

            var param = new D3DPRESENT_PARAMETERS
            {
                Windowed = 1,
                Flags = ((short) D3DPRESENTFLAG.D3DPRESENTFLAG_VIDEO),
                /*
                D3DFMT_R8G8B8：表示一个24位像素，从左开始，8位分配给红色，8位分配给绿色，8位分配给蓝色。

                D3DFMT_X8R8G8B8：表示一个32位像素，从左开始，8位不用，8位分配给红色，8位分配给绿色，8位分配给蓝色。

                D3DFMT_A8R8G8B8：表示一个32位像素，从左开始，8位为ALPHA通道，8位分配给红色，8位分配给绿色，8位分配给蓝色。

                D3DFMT_A16B16G16R16F：表示一个64位浮点像素，从左开始，16位为ALPHA通道，16位分配给蓝色，16位分配给绿色，16位分配给红色。

                D3DFMT_A32B32G32R32F：表示一个128位浮点像素，从左开始，32位为ALPHA通道，32位分配给蓝色，32位分配给绿色，32位分配给红色。
                 */
                //BackBufferFormat = D3DFORMAT.D3DFMT_X8R8G8B8,

                //SwapEffect = D3DSWAPEFFECT.D3DSWAPEFFECT_COPY
                SwapEffect = D3DSWAPEFFECT.D3DSWAPEFFECT_DISCARD,

                hDeviceWindow = GetDesktopWindow(), // 添加
                PresentationInterval = (int) D3D9.PresentInterval.Default,
            };

            /* The COM pointer to our D3D Device */
            IntPtr dev;
            m_d3dEx.CreateDeviceEx(0, D3DDEVTYPE.D3DDEVTYPE_HAL, m_hWnd,
                Direct3D.CreateFlags.D3DCREATE_HARDWARE_VERTEXPROCESSING | Direct3D.CreateFlags.D3DCREATE_MULTITHREADED
                          | Direct3D.CreateFlags.D3DCREATE_FPU_PRESERVE,
                ref param, IntPtr.Zero, out dev);

            m_device = (IDirect3DDevice9) Marshal.GetObjectForIUnknown(dev);
            // 只是减少引用计数而已，现在换成 m_device 了
            Marshal.Release(dev);

            hr = m_device.TestCooperativeLevel();
            var pDevice = dev;

            D3D11.Texture2D d3d11Texture2D = CreateRenderTarget();
            //SetRenderTarget(d3d11Texture2D);

            var format = TranslateFormat(TranslateFormat(d3d11Texture2D));

            var dxgiResource = d3d11Texture2D.QueryInterface<DXGI.Resource>();
            var pSharedHandle = dxgiResource.SharedHandle;

            hr = m_device.CreateTexture(ImageWidth,
                ImageHeight,
                1,
                1,
                format,
                0,
                out m_privateTexture,
                ref pSharedHandle);

            hr = m_privateTexture.GetSurfaceLevel(0, out m_privateSurface);

            var backBuffer = Marshal.GetIUnknownForObject(m_privateSurface);

            var surface = new D3D9.Surface(backBuffer);
            var queryInterface = surface.QueryInterface<D3D9.Surface>();
            //// 只是减少引用计数而已
            //Marshal.Release(backBuffer);

            //hr = m_device.SetTexture(0, m_privateTexture);

            var texturePtr = Marshal.GetIUnknownForObject(m_privateTexture);
            //Marshal.Release(texturePtr);

            //var byteList = new byte[32 * 10];
            //for (int i = 0; i < byteList.Length; i++)
            //{
            //    byteList[i] = (byte)i;
            //}

            //unsafe
            //{
            //    fixed (void* p = byteList)
            //    {
            //        Buffer.MemoryCopy(p, (void*) texturePtr,0,320);
            //    }
            //}

            //var d2dFactory = new SharpDX.Direct2D1.Factory();

            //Texture2D backBufferTexture2D = new Texture2D(texturePtr);
            //var d2dRenderTarget = new RenderTarget(d2dFactory, new SharpDX.DXGI.Surface(backBuffer),
            //    new RenderTargetProperties(new SharpDX.Direct2D1.PixelFormat(Format.Unknown,AlphaMode.Premultiplied)));

            //d2dRenderTarget.BeginDraw();
            //d2dRenderTarget.Clear(new RawColor4(1,0,0.5f,1));
            //d2dRenderTarget.EndDraw();

            D3DImage.Lock();
            D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, backBuffer, true);
            D3DImage.Unlock();

            Render();

            string s = "123";
            var stringBuilder = new StringBuilder(s);
            stringBuilder.Replace("%", "%25").Replace("#", "%23");
            stringBuilder.Insert(0, "123");
            stringBuilder.Insert("123".Length, "#");
        }

        private async void Render()
        {
            float x = 0;
            float y = 0;
            const float dx = 1;
            const float dy = 1;

            while (Dispatcher.CheckAccess())
            {
                var renderTarget = _d2DRenderTarget;

                renderTarget.BeginDraw();

                renderTarget.Clear(new RawColor4(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle(), 1));
                var brush = new D2D.SolidColorBrush(_d2DRenderTarget, new RawColor4(1, 0, 0, 1));

                renderTarget.DrawRectangle(new RawRectangleF(x, y, x + 10, y + 10), brush);

                x += dx;
                y += dy;
                if (x >= ImageWidth)
                {
                    x = 0;
                }

                if (y >= ImageHeight)
                {
                    y = 0;
                }

                renderTarget.EndDraw();

                D3DImage.Lock();
                D3DImage.AddDirtyRect(new Int32Rect(0, 0, D3DImage.PixelWidth, D3DImage.PixelHeight));
                D3DImage.Unlock();

                Image.InvalidateVisual();

                await Task.Delay(16);
            }
        }

        private void SetRenderTarget(D3D11.Texture2D target)
        {
            var format = TranslateFormat(target);
            var handle = GetSharedHandle(target);

            var presentParams = GetPresentParameters();
            var createFlags = D3D9.CreateFlags.HardwareVertexProcessing | D3D9.CreateFlags.Multithreaded |
                              D3D9.CreateFlags.FpuPreserve;

            var d3DContext = new D3D9.Direct3DEx();
            var d3DDevice = new D3D9.DeviceEx(d3DContext, 0, D3D9.DeviceType.Hardware, IntPtr.Zero, createFlags,
                presentParams);

            _renderTarget = new D3D9.Texture(d3DDevice, target.Description.Width, target.Description.Height, 1,
                D3D9.Usage.RenderTarget, format, D3D9.Pool.Default, ref handle);
        }

        private static D3D9.PresentParameters GetPresentParameters()
        {
            var presentParams = new D3D9.PresentParameters();

            presentParams.Windowed = true;
            presentParams.SwapEffect = D3D9.SwapEffect.Discard;
            presentParams.DeviceWindowHandle = GetDesktopWindow();
            presentParams.PresentationInterval = D3D9.PresentInterval.Default;

            return presentParams;
        }

        private IntPtr GetSharedHandle(D3D11.Texture2D texture)
        {
            using (var resource = texture.QueryInterface<DXGI.Resource>())
            {
                return resource.SharedHandle;
            }
        }

        private static D3DFORMAT TranslateFormat(D3D9.Format format)
         => format switch
         {
             D3D9.Format.A8R8G8B8 => D3DFORMAT.D3DFMT_A8R8G8B8,
             _ => throw new ArgumentException(),
         };

        private static D3D9.Format TranslateFormat(D3D11.Texture2D texture)
        {
            switch (texture.Description.Format)
            {
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
                    return D3D9.Format.A2B10G10R10;
                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                    return D3D9.Format.A16B16G16R16F;
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                    return D3D9.Format.A8R8G8B8;
                default:
                    return D3D9.Format.Unknown;
            }
        }

        private static PixelFormat TranslateFormatToPixelFormat(D3D9.Format format, bool preMultiplied = true)
        {
            return format switch
            {
                D3D9.Format.R8G8B8 => PixelFormats.Bgr24,
                D3D9.Format.A8R8G8B8 => preMultiplied ? PixelFormats.Pbgra32 : PixelFormats.Bgra32,
                D3D9.Format.X8R8G8B8 => PixelFormats.Bgr32,
                //D3D9.Format.R5G6B5 => PixelFormats.Bgr16bpp565,
                //D3D9.Format.X1R5G5B5 => PixelFormats.BGR16bpp555,
                D3D9.Format.P8 => PixelFormats.Indexed8,
                D3D9.Format.L8 => PixelFormats.Gray8,
                D3D9.Format.A2R10G10B10 => PixelFormats.Bgr101010,
                D3D9.Format.A32B32G32R32F => preMultiplied ? PixelFormats.Prgba128Float : PixelFormats.Rgb128Float,
                _ => throw new NotSupportedException(),
            };
        }

        private Texture2D CreateRenderTarget()
        {
            var width = ImageWidth;
            var height = ImageHeight;

            var renderDesc = new D3D11.Texture2DDescription
            {
                BindFlags = D3D11.BindFlags.RenderTarget | D3D11.BindFlags.ShaderResource,
                Format = DXGI.Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                MipLevels = 1,
                SampleDescription = new DXGI.SampleDescription(1, 0),
                Usage = D3D11.ResourceUsage.Default,
                OptionFlags = D3D11.ResourceOptionFlags.Shared,
                CpuAccessFlags = D3D11.CpuAccessFlags.None,
                ArraySize = 1
            };

            var device = new D3D11.Device(DriverType.Hardware, D3D11.DeviceCreationFlags.BgraSupport);
            var renderTarget = new D3D11.Texture2D(device, renderDesc);
            var surface = renderTarget.QueryInterface<DXGI.Surface>();

            var d2DFactory = new D2D.Factory();

            var supportRequired = FormatSupport.RenderTarget;
            var isSupported = device.CheckFormatSupport(DXGI.Format.B8G8R8A8_UNorm).HasFlag(supportRequired);
            if (isSupported)
            {

            }

            var renderTargetProperties =
                new D2D.RenderTargetProperties(new D2D.PixelFormat(DXGI.Format.B8G8R8A8_UNorm, D2D.AlphaMode.Premultiplied));
            _d2DRenderTarget = new D2D.RenderTarget(d2DFactory, surface, renderTargetProperties);

            device.ImmediateContext.Rasterizer.SetViewport(0, 0, ImageWidth, ImageHeight);

            return renderTarget;
        }

        private int ImageWidth => (int) ActualWidth;
        private int ImageHeight => (int) ActualHeight;

        private static IDirect3DDevice9 m_device;
        private static IDirect3D9Ex m_d3dEx;
        private static IntPtr m_hWnd;
        private static IDirect3DTexture9 m_privateTexture;
        private static IDirect3DSurface9 m_privateSurface;
        private D2D.RenderTarget _d2DRenderTarget;
        private D3D9.Texture _renderTarget;

        /// <summary>
        /// The SDK version of D3D we are using
        /// </summary>
        private const ushort D3D_SDK_VERSION = 32;

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();
    }
}