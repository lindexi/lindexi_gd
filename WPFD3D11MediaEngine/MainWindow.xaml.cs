using Lsj.Util.Win32;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.ComInterfaces;
using Lsj.Util.Win32.DirectX.ComInterfaces;
using Lsj.Util.Win32.DirectX.Enums;
using Lsj.Util.Win32.DirectX.Structs;
using Lsj.Util.Win32.Extensions.COM;
using Lsj.Util.Win32.Extensions.DirectX.Direct3D11;
using Lsj.Util.Win32.MediaFoundation;
using Lsj.Util.Win32.MediaFoundation.ComInterfaces;
using Lsj.Util.Win32.MediaFoundation.Structs;
using Lsj.Util.Win32.Structs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using static Lsj.Util.Win32.ComInterfaces.IIDs;
using static Lsj.Util.Win32.Constants;
using static Lsj.Util.Win32.DirectX.Constants;
using static Lsj.Util.Win32.DirectX.D3d9;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_BIND_FLAG;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_CREATE_DEVICE_FLAG;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_RESOURCE_MISC_FLAG;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_USAGE;
using static Lsj.Util.Win32.DirectX.Enums.D3DCREATE;
using static Lsj.Util.Win32.DirectX.Enums.D3DDEVTYPE;
using static Lsj.Util.Win32.DirectX.Enums.D3DFORMAT;
using static Lsj.Util.Win32.DirectX.Enums.D3DPRESENT;
using static Lsj.Util.Win32.DirectX.Enums.D3DSWAPEFFECT;
using static Lsj.Util.Win32.DirectX.Enums.DXGI_FORMAT;
using static Lsj.Util.Win32.MediaFoundation.Constants;
using static Lsj.Util.Win32.MediaFoundation.Enums.MediaFoundationStartupFlags;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.MediaFoundationAttributes;
using static Lsj.Util.Win32.MediaFoundation.Mfplat;
using static Lsj.Util.Win32.UnsafePInvokeExtensions;

namespace WPFD3D11MediaEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string VideoPath = @"D:\Samples\4K.mp4";

        public MainWindow()
        {
            if (Environment.GetCommandLineArgs().Skip(1).FirstOrDefault() is string s)
            {
                VideoPath = s;
            }

            InitializeComponent();
            var d3dImage = new D3DImage();
            D3DImage = d3dImage;
            Image.Source = d3dImage;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private TimeSpan _last = TimeSpan.Zero;
        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            if (e is RenderingEventArgs args)
            {
                if (args.RenderingTime == _last)
                {
                    return;
                }
                _last = args.RenderingTime;
            }
            Kernel32.QueryPerformanceFrequency(out var frequency);
            Kernel32.QueryPerformanceCounter(out var count);

            //Console.WriteLine(count.QuadPart / (frequency.QuadPart / 1000));
            RenderFrame();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitD3D(new WindowInteropHelper(this).EnsureHandle());
            if (Device != NULL)
            {
                CreateBackBuffer();
            }
        }

        private void ImageBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Device != NULL)
            {
                CreateBackBuffer();
            }
        }

        public D3DImage D3DImage { get; private set; }

        /// <summary>
        /// The device
        /// </summary>
        public P<IDirect3DDevice9Ex> DeviceEx { get; private set; }

        /// <summary>
        /// The device as <see cref="IDirect3DDevice9"/>
        /// </summary>
        public P<IDirect3DDevice9> Device => DeviceEx.AsP<IDirect3DDevice9>();

        public P<ID3D11Device> Device11 { get; private set; }

        public P<ID3D11DeviceContext> DeviceContext { get; private set; }

        public P<IMFDXGIDeviceManager> DXGIDeviceManager { get; private set; }

        public P<IMFMediaEngine> MFMediaEngine { get; private set; }

        public P<ID3D11Texture2D> D3D11BackBuffer { get; private set; }

        public P<IDirect3DTexture9> D3D9BackBuffer { get; private set; }

        public int RenderTargetWidth { get; private set; }

        public int RenderTargetHeight { get; private set; }

        private bool InitD3D(HWND hWnd)
        {
            if (!Direct3DCreate9Ex(D3D_SDK_VERSION, out var iDirect3D9Ex))
            {
                return false;
            }

            var presentParameters = new D3DPRESENT_PARAMETERS
            {
                Windowed = true,
                SwapEffect = D3DSWAPEFFECT_FLIPEX,
                BackBufferFormat = D3DFMT_A8R8G8B8,
                BackBufferWidth = 0,
                BackBufferHeight = 0,
                BackBufferCount = 2,
                PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE,
            };

            if (!iDirect3D9Ex.Value.CreateDeviceEx(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, hWnd, D3DCREATE_HARDWARE_VERTEXPROCESSING | D3DCREATE_MULTITHREADED,
                ref presentParameters, ref NullRef<D3DDISPLAYMODEEX>(), out var device))
            {
                return false;
            }

            DeviceEx = device;

            DeviceHelper.CreateDevice(out var device11, out NullRef<P<ID3D11DeviceContext>>(), D3D11_CREATE_DEVICE_DEBUG);
            Device11 = device11;

            Device11.Value.GetImmediateContext(out var deviceContext);
            DeviceContext = deviceContext;

            Mfplat.MFStartup(MF_VERSION, MFSTARTUP_FULL);
            if (!MFCreateDXGIDeviceManager(out var resetToken, out var deviceManager))
            {
                return false;
            }

            if (!deviceManager.Value.ResetDevice(Device11.AsIUnknown(), resetToken))
            {
                deviceManager.AsIUnknown().Value.Release();
                return false;
            }

            DXGIDeviceManager = deviceManager;

            var multhread = DeviceContext.AsIUnknown().ToInterface<ID3D11Multithread>(IIDs.IID_ID3D11Multithread);
            multhread.Value.SetMultithreadProtected(true);
            multhread.AsIUnknown().Value.Release();

            var factory = COMExtensions.CreateInstance<IMFMediaEngineClassFactory>(CLSIDs.CLSID_MFMediaEngineClassFactory, IIDs.IID_IMFMediaEngineClassFactory);
            MFCreateAttributes(out var mediaEngineAttributes, 3);

            var callback = new MediaEngineCallback();
            mediaEngineAttributes.Value.SetUnknown(MF_MEDIA_ENGINE_CALLBACK, callback.GetNativePointer().AsIUnknown());
            mediaEngineAttributes.Value.SetUnknown(MF_MEDIA_ENGINE_DXGI_MANAGER, DXGIDeviceManager.AsIUnknown());
            mediaEngineAttributes.Value.SetUINT32(MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT, (uint)DXGI_FORMAT_B8G8R8A8_UNORM);

            factory.Value.CreateInstance(0, mediaEngineAttributes, out var engine);

            mediaEngineAttributes.AsIUnknown().Value.Release();
            factory.AsIUnknown().Value.Release();

            MFMediaEngine = engine;


            var url = OleAut32.SysAllocString(VideoPath);
            engine.Value.SetSource(url);
            OleAut32.SysFreeString(url);
            engine.Value.SetLoop(true);
            engine.Value.Play();

            return true;
        }

        private void CreateBackBuffer()
        {
            var transform = (PresentationSource.FromVisual(this) as HwndSource)?.CompositionTarget.TransformToDevice ?? Matrix.Identity;

            var vector = transform.Transform(new Vector(ImageBorder.ActualWidth, ImageBorder.ActualHeight));

            RenderTargetWidth = (int)Math.Round(vector.X);
            RenderTargetHeight = (int)Math.Round(vector.Y);

            D3D11_TEXTURE2D_DESC desc = new D3D11_TEXTURE2D_DESC
            {
                Width = RenderTargetWidth,
                Height = RenderTargetHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI_FORMAT_B8G8R8A8_UNORM,
                Usage = D3D11_USAGE_DEFAULT,
                BindFlags = D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_RENDER_TARGET,
                MiscFlags = D3D11_RESOURCE_MISC_SHARED,
                SampleDesc = new DXGI_SAMPLE_DESC
                {
                    Count = 1,
                }
            };

            var result = Device11.Value.CreateTexture2D(desc, null, out var d3d11BackBuffer);
            if (result)
            {
                if (D3D11BackBuffer != NULL)
                {
                    D3D11BackBuffer.AsIUnknown().Value.Release();
                }
                D3D11BackBuffer = d3d11BackBuffer;


                var resource = D3D11BackBuffer.AsIUnknown().ToInterface<IDXGIResource>(IID_IDXGIResource);
                resource.Value.GetSharedHandle(out var handle);
                resource.AsIUnknown().Value.Release();


                HANDLE sharedHandle = handle;
                result = Device.Value.CreateTexture(RenderTargetWidth, RenderTargetHeight, 1, D3DUSAGE.D3DUSAGE_RENDERTARGET, D3DFMT_A8R8G8B8,
                    D3DPOOL.D3DPOOL_DEFAULT, out var d3d9texture, ref sharedHandle);

                if (result)
                {
                    if (D3D9BackBuffer != NULL)
                    {
                        D3D9BackBuffer.AsIUnknown().Value.Release();
                    }

                    D3D9BackBuffer = d3d9texture;

                    D3D9BackBuffer.Value.GetSurfaceLevel(0, out var surface);
                    D3DImage.Lock();
                    D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface);
                    D3DImage.AddDirtyRect(new Int32Rect(0, 0, RenderTargetWidth, RenderTargetHeight));
                    D3DImage.Unlock();

                    surface.AsIUnknown().Value.Release();
                }
            }
        }

        private void RenderFrame()
        {
            if (MFMediaEngine != NULL)
            {
                var result = MFMediaEngine.Value.OnVideoStreamTick(out var pts);

                if (result == HRESULT.S_OK)
                {
                    result = MFMediaEngine.Value.TransferVideoFrame(D3D11BackBuffer.AsIUnknown(), NullRef<MFVideoNormalizedRect>(), NullRef<RECT>(), NullRef<MFARGB>());
                    if (result)
                    {
                        D3DImage.Lock();
                        D3DImage.AddDirtyRect(new Int32Rect(0, 0, RenderTargetWidth, RenderTargetHeight));
                        D3DImage.Unlock();
                    }
                }
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (MFMediaEngine != NULL)
            {
                MFMediaEngine.Value.Play();
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (MFMediaEngine != NULL)
            {
                MFMediaEngine.Value.PAu();
            }
        }
    }
}