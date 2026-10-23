using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D9;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;

namespace WPFMediaFoundation;

/// <summary>
/// Plays hardware-decoded video through a D3D9Ex surface hosted by WPF.
/// </summary>
public unsafe partial class MainWindow : Window
{
    private readonly D3DImage _image = new();
    private readonly Stopwatch _clock = new();
    private readonly string _videoPath;
    private IDirect3D9Ex* _direct3D;
    private IDirect3DDevice9Ex* _device;
    private IDirect3DDeviceManager9* _deviceManager;
    private IDirect3DSurface9* _renderTarget;
    private IMFSourceReader* _reader;
    private IMFSample* _pendingSample;
    private long _pendingTimestamp;
    private long? _firstTimestamp;
    private int _width;
    private int _height;
    private bool _mfStarted;
    private bool _initialized;
    private bool _closed;
    private readonly bool _selfTest;
    private readonly Stopwatch _testClock = new();
    private readonly DispatcherTimer _testTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private WriteableBitmap? _bitmap;
    private bool _software;
    private int _frames;
    private int _loops;
    private int _resizes;
    private int _framesAfterResize;
    private int _framesAfterLoop;
    private bool _resizeRequested;
    private bool _seekRequested;
    private long _duration;

    /// <summary>
    /// Uses the bundled video unless a command-line path is supplied.
    /// </summary>
    public MainWindow()
    {
        var arguments = Environment.GetCommandLineArgs().Skip(1).ToArray();
        _selfTest = arguments.Contains("--self-test", StringComparer.Ordinal);
        _videoPath = arguments.FirstOrDefault(argument => argument != "--self-test")
            ?? Path.Combine(AppContext.BaseDirectory, "Video", "视频.mp4");
        InitializeComponent();
        Image.Source = _image;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized || _closed)
        {
            return;
        }

        try
        {
            Console.WriteLine($"START architecture={RuntimeInformation.ProcessArchitecture} selfTest={_selfTest} video={_videoPath}");
            try
            {
                InitializeVideo(new HWND(new WindowInteropHelper(this).EnsureHandle()));
            }
            catch (COMException error)
            {
                Console.WriteLine($"D3D9 unavailable HRESULT=0x{error.HResult:X8}; retrying CPU RGB32 rendering.");
                ReleaseResources();
                _software = true;
                InitializeVideo(new HWND(new WindowInteropHelper(this).EnsureHandle()));
            }
            _initialized = true;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            if (_selfTest)
            {
                _testClock.Restart();
                _testTimer.Tick += SelfTest_Tick;
                _testTimer.Start();
            }
        }
        catch
        {
            ReleaseResources();
            throw;
        }
    }

    private void InitializeVideo(HWND window)
    {
        if (!_software)
        {
        IDirect3D9Ex* direct3D = null;
        PInvoke.Direct3DCreate9Ex(PInvoke.D3D_SDK_VERSION, &direct3D).ThrowOnFailure();
        _direct3D = direct3D;
        var parameters = new D3DPRESENT_PARAMETERS
        {
            Windowed = true,
            hDeviceWindow = window,
            SwapEffect = D3DSWAPEFFECT.D3DSWAPEFFECT_DISCARD,
            BackBufferFormat = D3DFORMAT.D3DFMT_UNKNOWN,
            BackBufferWidth = 1,
            BackBufferHeight = 1,
            BackBufferCount = 1,
            PresentationInterval = unchecked((uint)PInvoke.D3DPRESENT_INTERVAL_IMMEDIATE)
        };
        IDirect3DDevice9Ex* device = null;
        _direct3D->CreateDeviceEx(0, D3DDEVTYPE.D3DDEVTYPE_HAL, window,
            PInvoke.D3DCREATE_HARDWARE_VERTEXPROCESSING | PInvoke.D3DCREATE_MULTITHREADED | PInvoke.D3DCREATE_FPU_PRESERVE,
            &parameters, null, &device).ThrowOnFailure();
        _device = device;
        ResizeRenderTarget();
        }

        PInvoke.MFStartup(PInvoke.MF_VERSION, 0).ThrowOnFailure();
        _mfStarted = true;
        uint resetToken;
        IDirect3DDeviceManager9* manager = null;
        if (!_software)
        {
        PInvoke.DXVA2CreateDirect3DDeviceManager9(&resetToken, &manager).ThrowOnFailure();
        _deviceManager = manager;
        manager->ResetDevice((IDirect3DDevice9*)_device, resetToken).ThrowOnFailure();
        }

        IMFAttributes* attributes = null;
        IMFMediaType* mediaType = null;
        try
        {
            PInvoke.MFCreateAttributes(&attributes, 2).ThrowOnFailure();
            attributes->SetUINT32(PInvoke.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1).ThrowOnFailure();
            if (_software)
            {
                attributes->SetUINT32(PInvoke.MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, 1).ThrowOnFailure();
            }
            else
            {
                attributes->SetUnknown(PInvoke.MF_SOURCE_READER_D3D_MANAGER, (IUnknown*)manager).ThrowOnFailure();
            }
            IMFSourceReader* reader = null;
            fixed (char* path = _videoPath)
            {
                PInvoke.MFCreateSourceReaderFromURL(path, attributes, &reader).ThrowOnFailure();
            }
            _reader = reader;
            reader->SetStreamSelection(unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_ALL_STREAMS), false).ThrowOnFailure();
            reader->SetStreamSelection(unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_VIDEO_STREAM), true).ThrowOnFailure();
            PInvoke.MFCreateMediaType(&mediaType).ThrowOnFailure();
            mediaType->SetGUID(PInvoke.MF_MT_MAJOR_TYPE, PInvoke.MFMediaType_Video).ThrowOnFailure();
            mediaType->SetGUID(PInvoke.MF_MT_SUBTYPE, _software ? PInvoke.MFVideoFormat_RGB32 : PInvoke.MFVideoFormat_NV12).ThrowOnFailure();
            reader->SetCurrentMediaType(unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_VIDEO_STREAM), null, mediaType).ThrowOnFailure();
            PROPVARIANT duration = default;
            reader->GetPresentationAttribute(unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_MEDIASOURCE), PInvoke.MF_PD_DURATION, out duration).ThrowOnFailure();
            _duration = (long)duration.Anonymous.Anonymous.Anonymous.uhVal;
            if (_software)
            {
                Release(ref mediaType);
                reader->GetCurrentMediaType(unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_VIDEO_STREAM), &mediaType).ThrowOnFailure();
                ulong size;
                mediaType->GetUINT64(PInvoke.MF_MT_FRAME_SIZE, out size).ThrowOnFailure();
                _bitmap = new WriteableBitmap((int)(size >> 32), (int)(size & uint.MaxValue), 96, 96, PixelFormats.Bgr32, null);
                Image.Source = _bitmap;
            }
            Console.WriteLine($"READY renderer={(_software ? "CPU RGB32" : "D3D9")} durationTicks={_duration}");
        }
        finally
        {
            Release(ref mediaType);
            Release(ref attributes);
        }
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_initialized && !_closed)
        {
            _resizes++;
            Console.WriteLine($"RESIZE count={_resizes} width={ActualWidth} height={ActualHeight}");
        }
        if (_device != null && !_closed)
        {
            ResizeRenderTarget();
        }
    }

    private void ResizeRenderTarget()
    {
        if (!PInvoke.GetClientRect(new HWND(new WindowInteropHelper(this).EnsureHandle()), out var rect))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        int width = rect.right - rect.left;
        int height = rect.bottom - rect.top;
        if (width <= 0 || height <= 0 || (width == _width && height == _height))
        {
            return;
        }

        IDirect3DSurface9* target = null;
        _device->CreateRenderTarget((uint)width, (uint)height, D3DFORMAT.D3DFMT_A8R8G8B8,
            D3DMULTISAMPLE_TYPE.D3DMULTISAMPLE_NONE, 0, false, &target, null).ThrowOnFailure();
        try
        {
            _image.Lock();
            try
            {
                _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, (nint)target, true);
                Release(ref _renderTarget);
                _renderTarget = target;
                target = null;
                _width = width;
                _height = height;
            }
            finally
            {
                _image.Unlock();
            }
        }
        finally
        {
            Release(ref target);
        }
    }

    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        try
        {
            RenderFrame();
        }
        catch (COMException error) when (!_software &&
            (error.HResult == unchecked((int)0xC00D36B4) || error.HResult == unchecked((int)0x80004002) ||
             error.HResult == unchecked((int)0x8876086A)))
        {
            Console.WriteLine($"D3D9 decode/surface unsupported HRESULT=0x{error.HResult:X8}; retrying CPU RGB32 rendering.");
            ReleaseResources();
            _software = true;
            Window_Loaded(this, new RoutedEventArgs());
        }
        catch
        {
            ReleaseResources();
            throw;
        }
    }

    private void RenderFrame()
    {
        if (!_initialized || (!_software && (_renderTarget == null || !_image.IsFrontBufferAvailable)))
        {
            return;
        }

        if (_pendingSample == null)
        {
            uint stream;
            uint flags;
            long timestamp;
            IMFSample* sample = null;
            var result = _reader->ReadSample(unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_VIDEO_STREAM), 0,
                &stream, &flags, &timestamp, &sample);
            _pendingSample = sample;
            result.ThrowOnFailure();
            if ((flags & (uint)MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_ERROR) != 0)
            {
                throw new COMException(null, unchecked((int)0x80004005));
            }
            if ((flags & (uint)MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_ENDOFSTREAM) != 0)
            {
                Release(ref _pendingSample);
                var position = new PROPVARIANT();
                position.Anonymous.Anonymous.vt = VARENUM.VT_I8;
                // A zero-initialized VT_I8 seeks to the beginning without allocating memory.
                _reader->SetCurrentPosition(Guid.Empty, in position).ThrowOnFailure();
                _firstTimestamp = null;
                _clock.Reset();
                _loops++;
                Console.WriteLine($"LOOP count={_loops} frames={_frames}");
                return;
            }
            if (sample == null)
            {
                return;
            }
            _pendingTimestamp = timestamp;
            if (_firstTimestamp == null)
            {
                _firstTimestamp = timestamp;
                _clock.Restart();
            }
        }

        // Media Foundation timestamps and TimeSpan ticks are both in 100 ns units.
        if (_clock.Elapsed.Ticks < _pendingTimestamp - _firstTimestamp.GetValueOrDefault())
        {
            return;
        }

        IMFMediaBuffer* buffer = null;
        IDirect3DSurface9* surface = null;
        try
        {
            if (_software)
            {
                _pendingSample->ConvertToContiguousBuffer(&buffer).ThrowOnFailure();
                byte* data;
                uint length;
                buffer->Lock(&data, null, &length).ThrowOnFailure();
                try
                {
                    var bitmap = _bitmap ?? throw new InvalidOperationException();
                    bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), (nint)data,
                        checked((int)length), checked(bitmap.PixelWidth * 4));
                }
                finally
                {
                    buffer->Unlock().ThrowOnFailure();
                }
                CountFrame();
                return;
            }
            _pendingSample->GetBufferByIndex(0, &buffer).ThrowOnFailure();
            void* service = null;
            PInvoke.MFGetService((IUnknown*)buffer, PInvoke.MR_BUFFER_SERVICE,
                typeof(IDirect3DSurface9).GUID, out service).ThrowOnFailure();
            surface = (IDirect3DSurface9*)service;
            _image.Lock();
            try
            {
                _device->StretchRect(surface, null, _renderTarget, null,
                    D3DTEXTUREFILTERTYPE.D3DTEXF_NONE).ThrowOnFailure();
                _image.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
                CountFrame();
            }
            finally
            {
                _image.Unlock();
            }
        }
        finally
        {
            Release(ref surface);
            Release(ref buffer);
            Release(ref _pendingSample);
        }
    }

    private void CountFrame()
    {
        _frames++;
        if (_resizes > 0) _framesAfterResize++;
        if (_loops > 0) _framesAfterLoop++;
    }

    private void SelfTest_Tick(object? sender, EventArgs e)
    {
        if (!_resizeRequested && _frames >= 10)
        {
            _resizeRequested = true;
            Width = 640;
            Height = 360;
        }
        if (!_seekRequested && _framesAfterResize >= 10 && _duration > 0)
        {
            _seekRequested = true;
            Release(ref _pendingSample);
            var position = new PROPVARIANT();
            position.Anonymous.Anonymous.vt = VARENUM.VT_I8;
            position.Anonymous.Anonymous.Anonymous.hVal = Math.Max(0, _duration - TimeSpan.TicksPerSecond);
            _reader->SetCurrentPosition(Guid.Empty, in position).ThrowOnFailure();
            _firstTimestamp = null;
            _clock.Reset();
            Console.WriteLine("SEEK near EOF to exercise real end-of-stream looping");
        }
        if (_testClock.Elapsed < TimeSpan.FromSeconds(15)) return;
        bool passed = _frames >= 30 && _resizes > 0 && _framesAfterResize >= 10 && _loops > 0 && _framesAfterLoop >= 10;
        Console.WriteLine($"SELF-TEST {(passed ? "PASS" : "FAIL")} frames={_frames} resizes={_resizes} framesAfterResize={_framesAfterResize} loops={_loops} framesAfterLoop={_framesAfterLoop} elapsedMs={_testClock.ElapsedMilliseconds}");
        Environment.ExitCode = passed ? 0 : 1;
        Close();
        Application.Current.Shutdown(Environment.ExitCode);
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        try
        {
            ReleaseResources();
            Console.WriteLine("CLEANUP complete");
        }
        finally
        {
            base.OnClosed(e);
        }
    }

    private void ReleaseResources()
    {
        _testTimer.Stop();
        _testTimer.Tick -= SelfTest_Tick;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
        _initialized = false;
        _clock.Reset();
        try
        {
            _image.Lock();
            try
            {
                _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, nint.Zero);
            }
            finally
            {
                _image.Unlock();
            }
        }
        finally
        {
            Release(ref _pendingSample);
            Release(ref _reader);
            Release(ref _renderTarget);
            Release(ref _deviceManager);
            Release(ref _device);
            Release(ref _direct3D);
            if (_mfStarted)
            {
                _mfStarted = false;
                PInvoke.MFShutdown().ThrowOnFailure();
            }
        }
    }

    private static void Release<T>(ref T* value) where T : unmanaged
    {
        var pointer = value;
        value = null;
        if (pointer != null)
        {
            ((IUnknown*)pointer)->Release();
        }
    }
}
