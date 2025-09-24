using Lsj.Util.Win32;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.ComInterfaces;
using Lsj.Util.Win32.DirectX.ComInterfaces;
using Lsj.Util.Win32.DirectX.Structs;
using Lsj.Util.Win32.Enums;
using Lsj.Util.Win32.Extensions.COM;
using Lsj.Util.Win32.Extensions.Winmm;
using Lsj.Util.Win32.MediaFoundation;
using Lsj.Util.Win32.MediaFoundation.ComInterfaces;
using Lsj.Util.Win32.Structs;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using static Lsj.Util.Win32.ComInterfaces.IIDs;
using static Lsj.Util.Win32.Constants;
using static Lsj.Util.Win32.DirectX.Constants;
using static Lsj.Util.Win32.DirectX.D3d9;
using static Lsj.Util.Win32.DirectX.Enums.D3DCREATE;
using static Lsj.Util.Win32.DirectX.Enums.D3DDEVTYPE;
using static Lsj.Util.Win32.DirectX.Enums.D3DFORMAT;
using static Lsj.Util.Win32.DirectX.Enums.D3DMULTISAMPLE_TYPE;
using static Lsj.Util.Win32.DirectX.Enums.D3DPRESENT;
using static Lsj.Util.Win32.DirectX.Enums.D3DSWAPEFFECT;
using static Lsj.Util.Win32.DirectX.Enums.D3DTEXTUREFILTERTYPE;
using static Lsj.Util.Win32.Dxva2;
using static Lsj.Util.Win32.MediaFoundation.Constants;
using static Lsj.Util.Win32.MediaFoundation.Enums.MediaFoundationStartupFlags;
using static Lsj.Util.Win32.MediaFoundation.Enums.MF_SOURCE_READER_FLAG;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.AudioSubtypeGUIDs;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.MajorMediaTypes;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.MediaFoundationAttributes;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.VideoSubtypeGUIDs;
using static Lsj.Util.Win32.MediaFoundation.Mf;
using static Lsj.Util.Win32.MediaFoundation.Mfapi;
using static Lsj.Util.Win32.MediaFoundation.Mfplat;
using static Lsj.Util.Win32.MediaFoundation.Mfreadwrite;
using static Lsj.Util.Win32.UnsafePInvokeExtensions;

namespace WPFMediaFoundation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string VideoPath = @"D:\Samples\lg-uhd-arctic.mkv";

        DispatcherTimer _dispatcherTimer;
        public MainWindow()
        {
            InitializeComponent();
            var d3dImage = new D3DImage();
            D3DImage = d3dImage;
            Image.Source = d3dImage;

            //_dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            //_dispatcherTimer.Interval = TimeSpan.FromMicroseconds(5);
            //_dispatcherTimer.Tick += DispatcherTimer_Tick;
            //_dispatcherTimer.Start();
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            RenderFrame();
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            RenderFrame();
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitD3D(new WindowInteropHelper(this).EnsureHandle());
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (RenderTarget != NULL)
            {
                RenderTarget.AsIUnknown().Value.Release();
            }

            if (Device != NULL)
            {
                User32.GetClientRect(new WindowInteropHelper(this).EnsureHandle(), out var size);
                RenderTargetWidth = size.right - size.left;
                RenderTargetHeight = size.bottom - size.top;
                if (Device.Value.CreateRenderTarget(RenderTargetWidth, RenderTargetHeight, D3DFMT_A8R8G8B8, D3DMULTISAMPLE_NONE, 0, false, out var surface, ref NullRef<HANDLE>()))
                {
                    RenderTarget = surface;

                    D3DImage.Lock();
                    D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, RenderTarget);
                    D3DImage.Unlock();
                }
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

        public P<IDirect3DSurface9> RenderTarget { get; private set; }

        public int RenderTargetWidth { get; private set; }

        public int RenderTargetHeight { get; private set; }

        public P<IMFSourceReader> Reader { get; private set; }

        public P<IDirect3DDeviceManager9> DeviceManager { get; private set; }

        public uint VideoWidth { get; private set; }

        public uint VideoHeight { get; private set; }

        public Stopwatch PlayerStopwatch { get; } = new Stopwatch();

        public WaveOutPlayer? AudioPlayer { get; private set; }

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

            User32.GetClientRect(new WindowInteropHelper(this).EnsureHandle(), out var size);
            RenderTargetWidth = size.right - size.left;
            RenderTargetHeight = size.bottom - size.top;
            if (Device.Value.CreateRenderTarget(RenderTargetWidth, RenderTargetHeight, D3DFMT_A8R8G8B8, D3DMULTISAMPLE_NONE, 0, false, out var surface, ref NullRef<HANDLE>()))
            {
                RenderTarget = surface;

                D3DImage.Lock();
                D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, RenderTarget);
                D3DImage.Unlock();
            }
            else
            {
                return false;
            }

            Mfplat.MFStartup(MF_VERSION, MFSTARTUP_FULL);
            var result = MFCreateAttributes(out var attr, 2);
            if (!result)
            {
                return false;
            }

            if (!DXVA2CreateDirect3DDeviceManager9(out var resetToken, out var deviceManager))
            {
                return false;
            }

            if (!deviceManager.Value.ResetDevice(Device, resetToken))
            {
                deviceManager.AsIUnknown().Value.Release();
                return false;
            }

            DeviceManager = deviceManager;


            attr.Value.SetUINT32(MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1);
            attr.Value.SetUnknown(MF_SOURCE_READER_D3D_MANAGER, deviceManager.AsIUnknown());

            result = MFCreateSourceReaderFromURL(VideoPath, attr, out var reader);
            attr.AsIUnknown().Value.Release();

            if (!result)
            {
                return false;
            }

            Reader = reader;

            result = MFCreateMediaType(out var videoTypeOut);
            if (!result)
            {
                return false;
            }

            result = MFCreateMediaType(out var audioTypeOut);
            if (!result)
            {
                return false;
            }

            videoTypeOut.AsP<IMFAttributes>().Value.SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
            videoTypeOut.AsP<IMFAttributes>().Value.SetGUID(MF_MT_SUBTYPE, MFVideoFormat_NV12);
            Reader.Value.SetCurrentMediaType(MF_SOURCE_READER_FIRST_VIDEO_STREAM, ref NullRef<DWORD>(), videoTypeOut);
            videoTypeOut.AsIUnknown().Value.Release();

            audioTypeOut.AsP<IMFAttributes>().Value.SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio);
            audioTypeOut.AsP<IMFAttributes>().Value.SetGUID(MF_MT_SUBTYPE, MFAudioFormat_PCM);
            audioTypeOut.AsP<IMFAttributes>().Value.SetUINT32(MF_MT_AUDIO_BITS_PER_SAMPLE, 16);
            audioTypeOut.AsP<IMFAttributes>().Value.SetUINT32(MF_MT_AUDIO_SAMPLES_PER_SECOND, 44100);
            audioTypeOut.AsP<IMFAttributes>().Value.SetUINT32(MF_MT_AUDIO_NUM_CHANNELS, 2);
            Reader.Value.SetCurrentMediaType(MF_SOURCE_READER_FIRST_AUDIO_STREAM, ref NullRef<DWORD>(), audioTypeOut);
            audioTypeOut.AsIUnknown().Value.Release();

            Reader.Value.GetCurrentMediaType(MF_SOURCE_READER_FIRST_VIDEO_STREAM, out var actualType);
            MFGetAttributeSize(actualType.AsP<IMFAttributes>(), MF_MT_FRAME_SIZE, out var width, out var height);
            actualType.AsIUnknown().Value.Release();
            VideoWidth = width;
            VideoHeight = height;

            PlayerStopwatch.Restart();

            return true;
        }

        private long _lastTimeNs = 0; // ns
        private long _sentAudioTimeNs = 0; // ns
        private long _offsetTimeNs = 0;

        private void RenderFrame()
        {
            if (Reader == NULL)
            {
                return;
            }

            var start = PlayerStopwatch.ElapsedMilliseconds;

            var elapsedNs = PlayerStopwatch.Elapsed.TotalNanoseconds + _offsetTimeNs;
            if (elapsedNs < (_lastTimeNs - 5000000 /* 5ms */))
            {
                return;
            }
            var result = Reader.Value.ReadSample(MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0, out var actualStreamIndex, out var flags, out var timestamp, out var sample);
            _lastTimeNs = timestamp * 100;
            AudioPlayer?.SyncCurrentTime(_lastTimeNs);

            if (flags.HasFlag(MF_SOURCE_READERF_ENDOFSTREAM))
            {
                var propVariant = new PROPVARIANT();
                propVariant.vt = VARENUM.VT_I8;
                propVariant.lVal = 0;
                _sentAudioTimeNs = 0;
                AudioPlayer?.Reset();
                result = Reader.Value.SetCurrentPosition(new GUID(), propVariant);
                PlayerStopwatch.Restart();
                _offsetTimeNs = 0;
                return;
            }

            if (_lastTimeNs < elapsedNs)
            {
                _offsetTimeNs += _lastTimeNs - (long)elapsedNs;
                Console.WriteLine($"Update _offsetTimeNs {_offsetTimeNs}");
            }

            if (sample != NULL)
            {
                result = sample.Value.ConvertToContiguousBuffer(out var buffer);

                if (result)
                {
                    var MR_BUFFER_SERVICE = (GUID)"{A562248C-9AC6-4FFC-9FBA-3AF8F8AD1A4D}";
                    result = MFGetService(buffer.AsP<IUnknown>(), MR_BUFFER_SERVICE, IID_IDirect3DSurface9, out var surfacePtr);
                    if (result)
                    {
                        P<IDirect3DSurface9> surface = (IntPtr)surfacePtr;
                        surface.Value.GetDesc(out var desc);

                        result = Device.Value.StretchRect(surface, NullRef<RECT>(), RenderTarget, NullRef<RECT>(), D3DTEXF_NONE);

                        D3DImage.Lock();
                        D3DImage.AddDirtyRect(new Int32Rect(0, 0, RenderTargetWidth, RenderTargetHeight));
                        D3DImage.Unlock();

                        surface.AsIUnknown().Value.Release();
                    }

                    buffer.AsIUnknown().Value.Release();
                }

                sample.AsIUnknown().Value.Release();
            }

            var end = PlayerStopwatch.ElapsedMilliseconds;
            Console.WriteLine($"RenderFrame: Start {start} End {end} Duration {end - start}");
        }
    }
}