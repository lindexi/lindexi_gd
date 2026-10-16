using Lsj.Util.Win32;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.ComInterfaces;
using Lsj.Util.Win32.DirectX.ComInterfaces;
using Lsj.Util.Win32.DirectX.Enums;
using Lsj.Util.Win32.DirectX.Structs;
using Lsj.Util.Win32.Enums;
using Lsj.Util.Win32.Extensions.COM;
using Lsj.Util.Win32.Extensions.DirectX.Direct3D11;
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
using static Lsj.Util.Win32.DirectX.Enums.D3D11_BIND_FLAG;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_CREATE_DEVICE_FLAG;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_RESOURCE_MISC_FLAG;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_USAGE;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_VIDEO_FRAME_FORMAT;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_VIDEO_USAGE;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_VPIV_DIMENSION;
using static Lsj.Util.Win32.DirectX.Enums.D3D11_VPOV_DIMENSION;
using static Lsj.Util.Win32.DirectX.Enums.D3DCREATE;
using static Lsj.Util.Win32.DirectX.Enums.D3DDEVTYPE;
using static Lsj.Util.Win32.DirectX.Enums.D3DFORMAT;
using static Lsj.Util.Win32.DirectX.Enums.D3DMULTISAMPLE_TYPE;
using static Lsj.Util.Win32.DirectX.Enums.D3DPRESENT;
using static Lsj.Util.Win32.DirectX.Enums.D3DSWAPEFFECT;
using static Lsj.Util.Win32.DirectX.Enums.DXGI_FORMAT;
using static Lsj.Util.Win32.MediaFoundation.Constants;
using static Lsj.Util.Win32.MediaFoundation.Enums.MediaFoundationStartupFlags;
using static Lsj.Util.Win32.MediaFoundation.Enums.MF_SOURCE_READER_FLAG;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.AudioSubtypeGUIDs;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.MajorMediaTypes;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.MediaFoundationAttributes;
using static Lsj.Util.Win32.MediaFoundation.GUIDs.VideoSubtypeGUIDs;
using static Lsj.Util.Win32.MediaFoundation.Mfapi;
using static Lsj.Util.Win32.MediaFoundation.Mfplat;
using static Lsj.Util.Win32.MediaFoundation.Mfreadwrite;
using static Lsj.Util.Win32.UnsafePInvokeExtensions;

namespace WPFD3D11MediaFoundation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string VideoPath = @"D:\Samples\4K.mp4";

        DispatcherTimer _dispatcherTimer;
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

            //_dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            //_dispatcherTimer.Interval = TimeSpan.FromMicroseconds(5);
            //_dispatcherTimer.Tick += DispatcherTimer_Tick;
            //_dispatcherTimer.Start();
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

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            //RenderFrame();
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

                CreateBackBuffer();
                CreateVideoProcessor(RenderTargetWidth, RenderTargetHeight);


                //if (Device.Value.CreateRenderTarget(RenderTargetWidth, RenderTargetHeight, D3DFMT_A8R8G8B8, D3DMULTISAMPLE_NONE, 0, false, out var surface, ref NullRef<HANDLE>()))
                //{
                //    RenderTarget = surface;

                //    CreateBackBuffer();
                //    CreateVideoProcessor(RenderTargetWidth, RenderTargetHeight);

                //    //D3DImage.Lock();
                //    //D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, RenderTarget);
                //    //D3DImage.Unlock();
                //}

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

        public P<ID3D11VideoDevice> D3D11VideoDevice { get; private set; }

        public P<ID3D11VideoContext> D3D11VideoContext { get; private set; }

        public P<ID3D11VideoProcessorEnumerator> D3D11VideoProcessorEnumerator { get; private set; }
        public P<ID3D11VideoProcessor> D3D11VideoProcessor { get; private set; }

        public P<ID3D11Texture2D> D3D11BackBuffer { get; private set; }

        public P<IDirect3DTexture9> D3D9BackBuffer { get; private set; }

        public P<IDirect3DSurface9> RenderTarget { get; private set; }

        public int RenderTargetWidth { get; private set; }

        public int RenderTargetHeight { get; private set; }

        public P<IMFSourceReader> Reader { get; private set; }

        public P<IMFDXGIDeviceManager> DXGIDeviceManager { get; private set; }

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

            DeviceHelper.CreateDevice(out var device11, out NullRef<P<ID3D11DeviceContext>>(), D3D11_CREATE_DEVICE_DEBUG);
            Device11 = device11;

            Device11.Value.GetImmediateContext(out var deviceContext);
            DeviceContext = deviceContext;

            User32.GetClientRect(new WindowInteropHelper(this).EnsureHandle(), out var size);
            RenderTargetWidth = size.right - size.left;
            RenderTargetHeight = size.bottom - size.top;
            //if (Device.Value.CreateRenderTarget(RenderTargetWidth, RenderTargetHeight, D3DFMT_A8R8G8B8, D3DMULTISAMPLE_NONE, 0, false, out var surface, ref NullRef<HANDLE>()))
            //{
            //    RenderTarget = surface;

            //    CreateBackBuffer();

            //    D3DImage.Lock();
            //    D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, RenderTarget);
            //    D3DImage.Unlock();
            //}
            //else
            //{
            //    return false;
            //}

            Mfplat.MFStartup(MF_VERSION, MFSTARTUP_FULL);
            var result = MFCreateAttributes(out var attr, 2);
            if (!result)
            {
                return false;
            }

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

            D3D11VideoDevice = Device11.AsIUnknown().ToInterface<ID3D11VideoDevice>(IIDs.IID_ID3D11VideoDevice);
            D3D11VideoContext = DeviceContext.AsIUnknown().ToInterface<ID3D11VideoContext>(IIDs.IID_ID3D11VideoContext);
            D3D11VideoContext.Value.VideoProcessorSetStreamAutoProcessingMode(D3D11VideoProcessor, 0, false);

            var multhread = D3D11VideoContext.AsIUnknown().ToInterface<ID3D11Multithread>(IIDs.IID_ID3D11Multithread);
            multhread.Value.SetMultithreadProtected(true);
            multhread.AsIUnknown().Value.Release();

            CreateBackBuffer();
            CreateVideoProcessor(RenderTargetWidth, RenderTargetHeight);

            PlayerStopwatch.Restart();

            return true;
        }

        private long _lastTimeNs = 0; // ns
        private long _sentAudioTimeNs = 0; // ns
        private long _offsetTimeNs = 0;

        private List<(long start, long end)> _fpsData = new();

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
                    var dxgiBuffer = buffer.AsIUnknown().ToInterface<IMFDXGIBuffer>(IIDs.IID_IMFDXGIBuffer, true, true);
                    dxgiBuffer.Value.GetResource(IIDs.IID_ID3D11Texture2D, out var texturePtr);
                    dxgiBuffer.Value.GetSubresourceIndex(out var index);
                    dxgiBuffer.AsIUnknown().Value.Release();

                    P<ID3D11Texture2D> texture = (nint)texturePtr;
                    //texture.Value.GetDesc(out var desc);

                    var inputDesc = new D3D11_VIDEO_PROCESSOR_INPUT_VIEW_DESC
                    {
                        ViewDimension = D3D11_VPIV_DIMENSION_TEXTURE2D,
                        Texture2D = new D3D11_TEX2D_VPIV
                        {
                            MipSlice = 0,
                            ArraySlice = index,
                        }
                    };

                    D3D11VideoDevice.Value.CreateVideoProcessorInputView(texture.AsP<ID3D11Resource>(), D3D11VideoProcessorEnumerator, inputDesc, out var inputView);




                    //SwapChain.Value.GetBuffer(0, IIDs.IID_ID3D11Texture2D, out var backBufferPtr);
                    //P<ID3D11Texture2D> backBuffer = backBufferPtr;
                    //backBuffer.Value.GetDesc(out var backDesc);
                    //Console.WriteLine($"BackBuffer {backDesc.Width}x{backDesc.Height}");


                    var outputDesc = new D3D11_VIDEO_PROCESSOR_OUTPUT_VIEW_DESC
                    {
                        ViewDimension = D3D11_VPOV_DIMENSION_TEXTURE2D,
                        Texture2D = new D3D11_TEX2D_VPOV
                        {
                            MipSlice = 0,
                        }
                    };
                    D3D11VideoDevice.Value.CreateVideoProcessorOutputView(D3D11BackBuffer.AsP<ID3D11Resource>(), D3D11VideoProcessorEnumerator, outputDesc, out var outputView);

                    var stream = new D3D11_VIDEO_PROCESSOR_STREAM
                    {
                        Enable = true,
                        pInputSurface = inputView,
                    };

                    D3D11VideoContext.Value.VideoProcessorBlt(D3D11VideoProcessor, outputView, 0, 1, [stream]);

                    if (result)
                    {
                        //P<IDirect3DSurface9> surface = (IntPtr)surfacePtr;
                        //surface.Value.GetDesc(out var desc);

                        //result = Device.Value.StretchRect(surface, NullRef<RECT>(), RenderTarget, NullRef<RECT>(), D3DTEXF_NONE);

                        //D3D9BackBuffer.Value.GetSurfaceLevel(0, out var surface);


                        D3DImage.Lock();
                        //D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface);
                        D3DImage.AddDirtyRect(new Int32Rect(0, 0, RenderTargetWidth, RenderTargetHeight));
                        D3DImage.Unlock();

                        //surface.AsIUnknown().Value.Release();
                    }


                    texture.AsIUnknown().Value.Release();
                    inputView.AsIUnknown().Value.Release();
                    outputView.AsIUnknown().Value.Release();

                    //var MR_BUFFER_SERVICE = (GUID)"{A562248C-9AC6-4FFC-9FBA-3AF8F8AD1A4D}";
                    //result = MFGetService(buffer.AsP<IUnknown>(), MR_BUFFER_SERVICE, IID_IDirect3DSurface9, out var surfacePtr);
                    //if (result)
                    //{
                    //    P<IDirect3DSurface9> surface = (IntPtr)surfacePtr;
                    //    surface.Value.GetDesc(out var desc);

                    //    result = Device.Value.StretchRect(surface, NullRef<RECT>(), RenderTarget, NullRef<RECT>(), D3DTEXF_NONE);

                    //    D3DImage.Lock();
                    //    D3DImage.AddDirtyRect(new Int32Rect(0, 0, RenderTargetWidth, RenderTargetHeight));
                    //    D3DImage.Unlock();

                    //    surface.AsIUnknown().Value.Release();
                    //}

                    //buffer.AsIUnknown().Value.Release();
                }

                sample.AsIUnknown().Value.Release();
            }

            var end = PlayerStopwatch.ElapsedMilliseconds;
            //Console.WriteLine($"RenderFrame: Start {start} End {end} Duration {end - start}");

            _fpsData.Add((start, end));
            while (_fpsData.Count > 0 && end - _fpsData[0].start > 1000)
            {
                _fpsData.RemoveAt(0);
            }
            if (_fpsData.Count > 0)
            {

                Console.WriteLine($"FPS: {_fpsData.Count * 1000 / (end - _fpsData[0].start)}");
            }
        }


        private void CreateVideoProcessor(int width, int height)
        {
            var desc = new D3D11_VIDEO_PROCESSOR_CONTENT_DESC
            {
                InputFrameFormat = D3D11_VIDEO_FRAME_FORMAT_PROGRESSIVE,
                InputWidth = VideoWidth,
                InputHeight = VideoHeight,
                OutputWidth = (uint)width,
                OutputHeight = (uint)height,
                Usage = D3D11_VIDEO_USAGE_PLAYBACK_NORMAL,
            };
            Console.WriteLine($"Input {VideoWidth}x{VideoHeight} Ouput {width}x{height}");
            D3D11VideoDevice.Value.CreateVideoProcessorEnumerator(desc, out var enumerator);
            if (D3D11VideoProcessorEnumerator != NULL)
            {
                D3D11VideoProcessorEnumerator.AsIUnknown().Value.Release();
            }
            D3D11VideoProcessorEnumerator = enumerator;

            D3D11VideoDevice.Value.CreateVideoProcessor(D3D11VideoProcessorEnumerator, 0, out var processor);
            if (D3D11VideoProcessor != NULL)
            {
                D3D11VideoProcessor.AsIUnknown().Value.Release();
            }
            D3D11VideoProcessor = processor;

            var sourceRect = new RECT
            {
                left = 0,
                top = 0,
                right = (int)VideoWidth,
                bottom = (int)VideoHeight,
            };
            D3D11VideoContext.Value.VideoProcessorSetStreamSourceRect(D3D11VideoProcessor, 0, true, sourceRect);

            var destRect = new RECT
            {
                left = 0,
                top = 0,
                right = width,
                bottom = height,
            };
            D3D11VideoContext.Value.VideoProcessorSetStreamDestRect(D3D11VideoProcessor, 0, true, destRect);
            D3D11VideoContext.Value.VideoProcessorSetOutputTargetRect(D3D11VideoProcessor, true, destRect);
        }

        private void CreateBackBuffer()
        {
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
    }
}