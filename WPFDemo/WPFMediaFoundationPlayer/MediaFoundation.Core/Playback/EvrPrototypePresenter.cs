using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D9;
using Windows.Win32.Media.DirectShow;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;

namespace MediaFoundation.Core;

[ComVisible(true), ClassInterface(ClassInterfaceType.None)]
internal sealed unsafe class EVRPrototypePresenter : IMFVideoPresenter.Interface, IMFClockStateSink.Interface,
    IMFTopologyServiceLookupClient.Interface, IMFGetService.Interface, IMFVideoDeviceID.Interface,
    IMFVideoDisplayControl.Interface, IMFRateSupport.Interface, IDisposable
{
    private readonly object _gate = new();
    private IDirect3D9Ex* _api;
    private IDirect3DDevice9Ex* _device;
    private IDirect3DDeviceManager9* _manager;
    private IMFVideoSampleAllocator* _allocator;
    private int _targetWidth;
    private int _targetHeight;
    private int _mixerWidth;
    private int _mixerHeight;
    private int _displayWidth;
    private int _displayHeight;
    private long _generation;
    private double _displayAspectRatio;
    internal VideoScalingMode ScalingMode { get; }

    internal VideoSurfaceInfo SurfaceInfo
    {
        get
        {
            lock (_gate)
                return new
                (
                    Width, Height, _displayAspectRatio, _mixerWidth, _mixerHeight, _displayWidth, _displayHeight, _generation,
                    ScalingMode
                );
        }
    }

    private HWND _videoWindow;
    private uint _aspectRatio = 1;
    private MFVideoNormalizedRect _sourceRect = new() { right = 1, bottom = 1 };
    private RECT _destination;
    private COLORREF _border;
    private IMFTransform* _mixer;
    private IMFClock* _clock;
    private IMediaEventSink* _events;
    private IMFMediaType* _mediaType;
    private IDirect3DSurface9* _display;
    private IDirect3DSurface9* _latest;
    private IDirect3DQuery9* _fence;
    private readonly Timer _scheduler;
    private long _displayedFrames;
    private readonly nint[] _samples = new nint[3];
    private readonly Queue<int> _ready = new();
    private readonly Queue<int> _free = new();
    private bool _input;
    private bool _running;
    private bool _preroll;
    private bool _eos;
    private bool _closed;
    private float _rate = 1;
    private uint _steps;
    private long _inputNotifications;
    private long _outputAttempts;
    private long _nextDiagnostic;
    private Exception? _failure;
    internal int Width { get; private set; }
    internal int Height { get; private set; }
    internal long Frames { get; private set; }

    internal EVRPrototypePresenter(VideoScalingMode scalingMode = VideoScalingMode.MixerOutput)
    {
        ScalingMode = scalingMode;
        _scheduler = new Timer(_ => Pump(), null, Timeout.Infinite, Timeout.Infinite);
        try
        {
            IDirect3D9Ex* a = null;
            Check(PInvoke.Direct3DCreate9Ex(PInvoke.D3D_SDK_VERSION, &a), "Direct3DCreate9Ex");
            _api = a;
            var window = PInvoke.GetDesktopWindow();
            var pp = new D3DPRESENT_PARAMETERS
            {
                Windowed = true, hDeviceWindow = window,
                BackBufferWidth = 1, BackBufferHeight = 1, BackBufferCount = 1,
                BackBufferFormat = D3DFORMAT.D3DFMT_X8R8G8B8, SwapEffect = D3DSWAPEFFECT.D3DSWAPEFFECT_DISCARD
            };
            IDirect3DDevice9Ex* d = null;
            Check
            (
                _api->CreateDeviceEx
                (
                    0, D3DDEVTYPE.D3DDEVTYPE_HAL, window,
                    PInvoke.D3DCREATE_HARDWARE_VERTEXPROCESSING | PInvoke.D3DCREATE_MULTITHREADED |
                    PInvoke.D3DCREATE_FPU_PRESERVE,
                    &pp, null, &d
                ), "CreateDeviceEx"
            );
            _device = d;
            uint token = 0;
            IDirect3DDeviceManager9* m = null;
            Check(PInvoke.DXVA2CreateDirect3DDeviceManager9(&token, &m), "DXVA2CreateDirect3DDeviceManager9");
            _manager = m;
            Check(_manager->ResetDevice((IDirect3DDevice9*)_device, token), "ResetDevice");
            Guid allocatorId = IMFVideoSampleAllocator.IID_Guid;
            IMFVideoSampleAllocator* allocated = null;
            Check
            (
                PInvoke.MFCreateVideoSampleAllocator(&allocatorId, (void**)&allocated), "MFCreateVideoSampleAllocator"
            );
            _allocator = allocated;
            Check(_allocator->SetDirectXManager((IUnknown*)_manager), "allocator.SetDirectXManager");
            IDirect3DQuery9* query = null;
            Check(_device->CreateQuery(D3DQUERYTYPE.D3DQUERYTYPE_EVENT, &query), "CreateQuery EVENT");
            _fence = query;
            _scheduler.Change(0, 8);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal static void Check(HRESULT hr, string stage)
    {
        if (hr.Failed) Trace.WriteLine($"EVR prototype stage={stage} HRESULT=0x{hr.Value:X8}", "MediaFoundation.Core");
        if (hr.Failed) throw new COMException(string.Format(SR.Get("NativeStageFailure"), stage, hr.Value), hr.Value);
    }

    private HRESULT Guard(Action action)
    {
        lock (_gate)
        {
            if (_closed) return HRESULT.MF_E_SHUTDOWN;
            try
            {
                action();
                return new(0);
            }
            catch (Exception error) when (error is ExternalException or InvalidOperationException or ArgumentException)
            {
                if (_failure == null && _events != null)
                {
                    var notification = _events->Notify((int)PInvoke.EC_ERRORABORT, error.HResult, 0);
                    Trace.WriteLine
                    (
                        $"EVR prototype EC_ERRORABORT error=0x{error.HResult:X8} notifyHRESULT=0x{notification.Value:X8}",
                        "MediaFoundation.Core"
                    );
                }

                _failure = error;
                Trace.WriteLine($"EVR prototype failure {error}", "MediaFoundation.Core");
                return new(error.HResult);
            }
        }
    }

    public HRESULT InitServicePointers(IMFTopologyServiceLookup* lookup)
    {
        nint address = (nint)lookup;
        return Guard
        (() =>
            {
                ReleaseServices();
                _mixer = (IMFTransform*)Lookup
                    ((IMFTopologyServiceLookup*)address, PInvoke.MR_VIDEO_MIXER_SERVICE, IMFTransform.IID_Guid);
                _events = (IMediaEventSink*)Lookup
                    ((IMFTopologyServiceLookup*)address, PInvoke.MR_VIDEO_RENDER_SERVICE, IMediaEventSink.IID_Guid);
                // EVR can initialize before a presentation clock has been assigned.
                Guid clockService = PInvoke.MR_VIDEO_RENDER_SERVICE, clockId = IMFClock.IID_Guid;
                void* clockResult = null;
                uint clockCount = 1;
                var clockHr = ((IMFTopologyServiceLookup*)address)->LookupService
                (
                    MF_SERVICE_LOOKUP_TYPE.MF_SERVICE_LOOKUP_GLOBAL, 0,
                    &clockService, &clockId, &clockResult, &clockCount
                );
                if (clockHr.Succeeded) _clock = (IMFClock*)clockResult;
                else if (clockHr.Value == unchecked((int)0x80004002))
                    Trace.WriteLine
                    (
                        "EVR prototype clock unavailable during initialization; waiting for clock service",
                        "MediaFoundation.Core"
                    );
                else Check(clockHr, "LookupService IMFClock");
                Guid deviceId = IMFVideoDeviceID.IID_Guid;
                IMFVideoDeviceID* mixerDevice = null;
                Check(_mixer->QueryInterface(&deviceId, (void**)&mixerDevice), "mixer.IMFVideoDeviceID");
                try
                {
                    Guid actual = default;
                    Check(mixerDevice->GetDeviceID(&actual), "mixer.GetDeviceID");
                    if (actual != IDirect3DDevice9.IID_Guid)
                        throw new InvalidOperationException($"Unsupported mixer device {actual}");
                }
                finally
                {
                    mixerDevice->Release();
                }

                Check
                (
                    _mixer->ProcessMessage(MFT_MESSAGE_TYPE.MFT_MESSAGE_SET_D3D_MANAGER, (nuint)_manager),
                    "mixer.SET_D3D_MANAGER"
                );
            }
        );
    }

    private static void* Lookup(IMFTopologyServiceLookup* lookup, Guid service, Guid iid)
    {
        void* result = null;
        uint count = 1;
        Check
        (
            lookup->LookupService(MF_SERVICE_LOOKUP_TYPE.MF_SERVICE_LOOKUP_GLOBAL, 0, &service, &iid, &result, &count),
            $"LookupService {iid}"
        );
        return result;
    }

    public HRESULT ReleaseServicePointers() => Guard(ReleaseServices);

    private void ReleaseServices()
    {
        Flush();
        ReleaseType();
        if (_mixer != null)
        {
            _mixer->Release();
            _mixer = null;
        }

        if (_clock != null)
        {
            _clock->Release();
            _clock = null;
        }

        if (_events != null)
        {
            _events->Release();
            _events = null;
        }
    }

    public HRESULT GetDeviceID(Guid* value)
    {
        if (value == null) return new(unchecked((int)0x80004003));
        *value = IDirect3DDevice9.IID_Guid;
        Trace.WriteLine($"EVR prototype GetDeviceID={*value} HRESULT=0x00000000", "MediaFoundation.Core");
        return new(0);
    }

    public HRESULT GetService(Guid* service, Guid* iid, void** result)
    {
        if (service == null || iid == null || result == null) return new(unchecked((int)0x80004003));
        *result = null;
        Trace.WriteLine($"EVR prototype GetService service={*service} iid={*iid}", "MediaFoundation.Core");
        lock (_gate)
        {
            if (_closed) return HRESULT.MF_E_SHUTDOWN;
            if ((*service == PInvoke.MF_RATE_CONTROL_SERVICE || *service == PInvoke.MR_VIDEO_RENDER_SERVICE) &&
                *iid == IMFRateSupport.IID_Guid)
            {
                *result = (void*)Marshal.GetComInterfaceForObject(this, typeof(IMFRateSupport.Interface));
                return new(0);
            }

            if (*service == PInvoke.MR_VIDEO_RENDER_SERVICE && *iid == IDirect3DDeviceManager9.IID_Guid)
                return _manager->QueryInterface(iid, result);
            if (*service == PInvoke.MR_VIDEO_ACCELERATION_SERVICE)
            {
                if (*iid == IMFVideoSampleAllocator.IID_Guid) return _allocator->QueryInterface(iid, result);
                return _manager->QueryInterface(iid, result);
            }

            if (*service == PInvoke.MR_VIDEO_RENDER_SERVICE && *iid == IMFVideoDisplayControl.IID_Guid)
            {
                *result = (void*)Marshal.GetComInterfaceForObject(this, typeof(IMFVideoDisplayControl.Interface));
                return new(0);
            }

            Trace.WriteLine($"EVR prototype unsupported service={*service} iid={*iid}", "MediaFoundation.Core");
            return new(unchecked((int)0x80004002));
        }
    }

    public HRESULT GetNativeVideoSize(SIZE* size, SIZE* aspect)
    {
        lock (_gate)
        {
            if (size != null) *size = new SIZE { cx = Width, cy = Height };
            if (aspect != null) *aspect = new SIZE { cx = Width, cy = Height };
            return _mediaType == null ? HRESULT.MF_E_TRANSFORM_TYPE_NOT_SET : new(0);
        }
    }

    public HRESULT GetIdealVideoSize(SIZE* minimum, SIZE* maximum)
    {
        if (minimum != null) *minimum = new SIZE { cx = 1, cy = 1 };
        if (maximum != null) *maximum = new SIZE { cx = 4096, cy = 4096 };
        return new(0);
    }

    public HRESULT SetVideoWindow(HWND window) => Guard
    (() =>
        {
            if (!PInvoke.IsWindow(window)) throw new ArgumentException("Invalid video HWND");
            _videoWindow = window;
        }
    );

    public HRESULT GetVideoWindow(HWND* window)
    {
        if (window == null) return new(unchecked((int)0x80004003));
        *window = _videoWindow;
        return new(0);
    }

    public HRESULT SetVideoPosition(MFVideoNormalizedRect* source, RECT* target)
    {
        // Cropping/scaling belongs to the WPF Image in this prototype.
        if (source != null && (source->left != 0 || source->top != 0 || source->right != 1 || source->bottom != 1))
            return new(unchecked((int)0x80004001));
        lock (_gate)
        {
            if (source != null) _sourceRect = *source;
            if (target != null) _destination = *target;
            return new(0);
        }
    }

    public HRESULT GetVideoPosition(MFVideoNormalizedRect* source, RECT* target)
    {
        lock (_gate)
        {
            if (source != null) *source = _sourceRect;
            if (target != null) *target = _destination;
            return new(0);
        }
    }

    public HRESULT SetAspectRatioMode(uint mode)
    {
        if (mode != 1) return new(unchecked((int)0x80004001));
        _aspectRatio = mode;
        return new(0);
    }

    public HRESULT GetAspectRatioMode(uint* mode)
    {
        if (mode == null) return new(unchecked((int)0x80004003));
        *mode = _aspectRatio;
        return new(0);
    }

    public HRESULT RepaintVideo() => Frames == 0 ? HRESULT.MF_E_INVALIDREQUEST : new(0);

    public HRESULT GetCurrentImage
        (Windows.Win32.Graphics.Gdi.BITMAPINFOHEADER* header, byte** data, uint* length, long* timestamp)
        => new(unchecked((int)0x80004001));

    public HRESULT SetBorderColor(COLORREF color)
    {
        if (color.Value != 0) return new(unchecked((int)0x80004001));
        _border = color;
        return new(0);
    }

    public HRESULT GetBorderColor(COLORREF* color)
    {
        if (color == null) return new(unchecked((int)0x80004003));
        *color = _border;
        return new(0);
    }

    public HRESULT SetRenderingPrefs(uint flags) => flags == 0 ? new(0) : new(unchecked((int)0x80004001));

    public HRESULT GetRenderingPrefs(uint* flags)
    {
        if (flags == null) return new(unchecked((int)0x80004003));
        *flags = 0;
        return new(0);
    }

    public HRESULT SetFullscreen(BOOL fullscreen) => fullscreen ? new(unchecked((int)0x80004001)) : new(0);

    public HRESULT GetFullscreen(BOOL* fullscreen)
    {
        if (fullscreen == null) return new(unchecked((int)0x80004003));
        *fullscreen = false;
        return new(0);
    }

    public HRESULT OnClockStart(long systemTime, long offset) => Guard
    (() =>
        {
            Trace.WriteLine
                ($"EVR prototype OnClockStart systemTime={systemTime} offset={offset}", "MediaFoundation.Core");
            _running = true;
        }
    );

    public HRESULT OnClockStop(long systemTime) => Guard
    (() =>
        {
            _running = false;
            Flush();
        }
    );

    public HRESULT OnClockPause(long systemTime) => Guard(() => _running = false);

    public HRESULT OnClockRestart(long systemTime) => Guard
    (() =>
        {
            Trace.WriteLine($"EVR prototype OnClockRestart systemTime={systemTime}", "MediaFoundation.Core");
            _running = true;
        }
    );

    public HRESULT GetSlowestRate(MFRATE_DIRECTION direction, BOOL thin, float* value)
    {
        if (value == null) return new(unchecked((int)0x80004003));
        *value = 0;
        return direction == MFRATE_DIRECTION.MFRATE_REVERSE ? HRESULT.MF_E_REVERSE_UNSUPPORTED : new(0);
    }

    public HRESULT GetFastestRate(MFRATE_DIRECTION direction, BOOL thin, float* value)
    {
        if (value == null) return new(unchecked((int)0x80004003));
        *value = 1;
        return direction == MFRATE_DIRECTION.MFRATE_REVERSE ? HRESULT.MF_E_REVERSE_UNSUPPORTED : new(0);
    }

    public HRESULT IsRateSupported(BOOL thin, float value, float* nearest)
    {
        if (nearest != null) *nearest = float.IsFinite(value) ? Math.Clamp(value, 0, 1) : 1;
        if (value < 0) return HRESULT.MF_E_REVERSE_UNSUPPORTED;
        return float.IsFinite(value) && value <= 1 ? new(0) : HRESULT.MF_E_UNSUPPORTED_RATE;
    }

    public HRESULT OnClockSetRate(long systemTime, float value)
    {
        Trace.WriteLine($"EVR prototype OnClockSetRate systemTime={systemTime} rate={value}", "MediaFoundation.Core");
        if (IsRateSupported(false, value, null).Failed) return HRESULT.MF_E_UNSUPPORTED_RATE;
        return Guard(() => _rate = value);
    }

    public HRESULT GetCurrentMediaType(IMFVideoMediaType** result)
    {
        if (result == null) return new(unchecked((int)0x80004003));
        *result = null;
        lock (_gate)
        {
            if (_mediaType == null) return HRESULT.MF_E_TRANSFORM_TYPE_NOT_SET;
            Guid iid = IMFVideoMediaType.IID_Guid;
            return _mediaType->QueryInterface(&iid, (void**)result);
        }
    }

    public HRESULT ProcessMessage(MFVP_MESSAGE_TYPE message, nuint parameter)
    {
        if (message != MFVP_MESSAGE_TYPE.MFVP_MESSAGE_PROCESSINPUTNOTIFY)
            Trace.WriteLine($"EVR prototype message={message} parameter={parameter}", "MediaFoundation.Core");

        return Guard
        (() =>
            {
                switch (message)
                {
                    case MFVP_MESSAGE_TYPE.MFVP_MESSAGE_STEP:
                        if ((_running && _rate != 0) || (uint)parameter == 0)
                            throw new COMException(SR.Get("InvalidStepState"), HRESULT.MF_E_INVALIDREQUEST.Value);
                        _steps = (uint)parameter;
                        Tick();
                        break;
                    case MFVP_MESSAGE_TYPE.MFVP_MESSAGE_CANCELSTEP: CompleteStep(true, 0); break;
                    case MFVP_MESSAGE_TYPE.MFVP_MESSAGE_FLUSH: Flush(); break;
                    case MFVP_MESSAGE_TYPE.MFVP_MESSAGE_INVALIDATEMEDIATYPE: Negotiate(); break;
                    case MFVP_MESSAGE_TYPE.MFVP_MESSAGE_PROCESSINPUTNOTIFY:
                        _inputNotifications++;
                        _input = true;
                        Tick();
                        break;
                    case MFVP_MESSAGE_TYPE.MFVP_MESSAGE_BEGINSTREAMING: _preroll = false; break;
                    case MFVP_MESSAGE_TYPE.MFVP_MESSAGE_ENDSTREAMING:
                        _running = false;
                        Flush();
                        break;
                    case MFVP_MESSAGE_TYPE.MFVP_MESSAGE_ENDOFSTREAM:
                        _eos = true;
                        _input = true;
                        break;
                    default: throw new ArgumentOutOfRangeException(nameof(message));
                }
            }
        );
    }

    private void Negotiate()
    {
        for (uint i = 0;; i++)
        {
            IMFMediaType* candidate = null;
            var hr = _mixer->GetOutputAvailableType(0, i, &candidate);
            if (hr == HRESULT.MF_E_NO_MORE_TYPES) Check(hr, "mixer no compatible RGB32 output");
            Check(hr, $"mixer.GetOutputAvailableType[{i}]");
            try
            {
                Guid key = PInvoke.MF_MT_SUBTYPE, subtype;
                Check(candidate->GetGUID(&key, &subtype), "output subtype");
                Trace.WriteLine($"EVR prototype candidate={i} subtype={subtype}", "MediaFoundation.Core");
                if (subtype != PInvoke.MFVideoFormat_RGB32) continue;
                key = PInvoke.MF_MT_FRAME_SIZE;
                ulong size = 0;
                Check(candidate->GetUINT64(&key, &size), "output frame size");
                int nativeWidth = checked((int)(size >> 32)), nativeHeight = checked((int)(size & uint.MaxValue));
                double aspect = ReadAspect(candidate, nativeWidth, nativeHeight);
                CommitOutput(candidate, nativeWidth, nativeHeight);
                Width = nativeWidth;
                Height = nativeHeight;
                _displayAspectRatio = aspect;
                return;
            }
            finally
            {
                candidate->Release();
            }
        }
    }

    private static double ReadAspect(IMFMediaType* type, int width, int height)
    {
        Guid key = PInvoke.MF_MT_PIXEL_ASPECT_RATIO;
        ulong par = 0;
        var hr = type->GetUINT64(&key, &par);
        if (hr.Failed && hr != HRESULT.MF_E_ATTRIBUTENOTFOUND) Check(hr, "source.PAR");
        double pixelAspect = par == 0 ? 1 : (double)(par >> 32) / (uint)par;
        foreach (Guid attribute in new[]
                 {
                     PInvoke.MF_MT_PAN_SCAN_APERTURE, PInvoke.MF_MT_MINIMUM_DISPLAY_APERTURE,
                     PInvoke.MF_MT_GEOMETRIC_APERTURE
                 })
        {
            key = attribute;
            if (attribute == PInvoke.MF_MT_PAN_SCAN_APERTURE)
            {
                Guid enabledKey = PInvoke.MF_MT_PAN_SCAN_ENABLED;
                uint enabled = 0;
                hr = type->GetUINT32(&enabledKey, &enabled);
                if (hr.Failed && hr != HRESULT.MF_E_ATTRIBUTENOTFOUND) Check(hr, "source.panScan");
                if (enabled == 0) continue;
            }

            MFVideoArea area = default;
            uint bytes = 0;
            hr = type->GetBlob(&key, (byte*)&area, (uint)sizeof(MFVideoArea), &bytes);
            if (hr == HRESULT.MF_E_ATTRIBUTENOTFOUND) continue;
            Check(hr, "source.aperture");
            if (bytes != sizeof(MFVideoArea) || area.Area.cx <= 0 || area.Area.cy <= 0)
                throw new InvalidOperationException(SR.Get("InvalidVideoAperture"));
            width = area.Area.cx;
            height = area.Area.cy;
            break;
        }

        return width * pixelAspect / height;
    }

    private void CommitOutput(IMFMediaType* source, int nativeWidth, int nativeHeight)
    {
        IMFMediaType* candidate = null;
        IDirect3DSurface9* staging = null;
        var pool = new nint[_samples.Length];
        int width = ScalingMode == VideoScalingMode.MixerOutput && _targetWidth > 0 ? _targetWidth : nativeWidth;
        int height = ScalingMode == VideoScalingMode.MixerOutput && _targetHeight > 0 ? _targetHeight : nativeHeight;
        try
        {
            Check(PInvoke.MFCreateMediaType(&candidate), "resize.CreateMediaType");
            Check(source->CopyAllItems((IMFAttributes*)candidate), "resize.CopyAllItems");
            if (ScalingMode == VideoScalingMode.MixerOutput)
            {
                Guid key = PInvoke.MF_MT_FRAME_SIZE;
                Check(candidate->SetUINT64(&key, ((ulong)(uint)width << 32) | (uint)height), "resize.frameSize");
                key = PInvoke.MF_MT_PIXEL_ASPECT_RATIO;
                Check(candidate->SetUINT64(&key, (1UL << 32) | 1), "resize.PAR");
                key = PInvoke.MF_MT_DEFAULT_STRIDE;
                Check(candidate->SetUINT32(&key, checked((uint)width * 4)), "resize.stride");
                key = PInvoke.MF_MT_SAMPLE_SIZE;
                Check(candidate->SetUINT32(&key, checked((uint)width * (uint)height * 4)), "resize.sampleSize");
                var area = new MFVideoArea { Area = new SIZE { cx = width, cy = height } };
                foreach (Guid attribute in new[]
                         {
                             PInvoke.MF_MT_GEOMETRIC_APERTURE, PInvoke.MF_MT_MINIMUM_DISPLAY_APERTURE,
                             PInvoke.MF_MT_PAN_SCAN_APERTURE
                         })
                {
                    key = attribute;
                    Check(candidate->SetBlob(&key, (byte*)&area, (uint)sizeof(MFVideoArea)), "resize.aperture");
                }
            }

            Check
            (
                _mixer->SetOutputType(0, candidate, (uint)_MFT_SET_TYPE_FLAGS.MFT_SET_TYPE_TEST_ONLY),
                "mixer.TEST_ONLY requested strategy"
            );
            staging = CreateSurface(width, height);
            for (int i = 0; i < pool.Length; i++)
            {
                var surface = CreateSurface(width, height);
                IMFSample* sample = null;
                try
                {
                    Check(PInvoke.MFCreateVideoSampleFromSurface((IUnknown*)surface, &sample), "resize.sample");
                }
                finally
                {
                    surface->Release();
                }

                pool[i] = (nint)sample;
            }

            if (_latest != null && Frames > 0)
            {
                Check
                (
                    _device->StretchRect(_latest, null, staging, null, D3DTEXTUREFILTERTYPE.D3DTEXF_LINEAR),
                    "resize.preserve paused frame LINEAR"
                );
            }

            // The gate serializes mixer, clock callbacks and sample recycling. Nothing old is
            // released before TEST_ONLY, allocation and the actual output-type commit succeed.
            SynchronizeGpu();
            Check(_mixer->SetOutputType(0, candidate, 0), "mixer.commit requested strategy");
            bool hadPreroll = _preroll;
            ReleaseType();
            _mediaType = candidate;
            candidate = null;
            _latest = staging;
            staging = null;
            _mixerWidth = width;
            _mixerHeight = height;
            for (int i = 0; i < pool.Length; i++)
            {
                _samples[i] = pool[i];
                pool[i] = 0;
                _free.Enqueue(i);
            }

            // A resize is not a seek: preserve preroll and outstanding STEP, and request more input.
            _preroll = hadPreroll;
            _input = true;
            _generation++;
            Trace.WriteLine
            (
                $"EVR scaling requested={ScalingMode} actual={ScalingMode} fallback=none native={nativeWidth}x{nativeHeight} mixer={width}x{height} generation={_generation}",
                "MediaFoundation.Core"
            );
        }
        finally
        {
            if (candidate != null) candidate->Release();
            if (staging != null) staging->Release();
            foreach (nint sample in pool)
                if (sample != 0)
                    ((IMFSample*)sample)->Release();
        }
    }

    private IDirect3DSurface9* CreateSurface(int width, int height)
    {
        IDirect3DSurface9* surface = null;
        Check
        (
            _device->CreateRenderTarget
            (
                (uint)width, (uint)height, D3DFORMAT.D3DFMT_X8R8G8B8,
                D3DMULTISAMPLE_TYPE.D3DMULTISAMPLE_NONE, 0, false, &surface, null
            ), "resize.CreateRenderTarget"
        );
        return surface;
    }

    internal void RequestTarget(int width, int height)
    {
        lock (_gate)
        {
            if (_closed || width <= 0 || height <= 0 || (width == _targetWidth && height == _targetHeight)) return;
            int previousWidth = _targetWidth, previousHeight = _targetHeight;
            _targetWidth = width;
            _targetHeight = height;
            try
            {
                if (_mediaType != null && ScalingMode == VideoScalingMode.MixerOutput)
                    CommitOutput(_mediaType, Width, Height);
            }
            catch
            {
                _targetWidth = previousWidth;
                _targetHeight = previousHeight;
                throw;
            }
        }
    }

    private void CompleteStep(bool cancelled, long timestamp)
    {
        if (_steps == 0) return;
        _steps = 0;
        Check(_events->Notify((int)PInvoke.EC_STEP_COMPLETE, cancelled ? 1 : 0, 0), "EC_STEP_COMPLETE");
        Trace.WriteLine
        (
            $"EVR prototype EC_STEP_COMPLETE cancelled={cancelled} timestamp={timestamp} rate={_rate}",
            "MediaFoundation.Core"
        );
        if (!cancelled && _rate == 0)
        {
            Check
            (
                _events->Notify((int)PInvoke.EC_SCRUB_TIME, (nint)(uint)timestamp, (nint)(uint)((ulong)timestamp >> 32)),
                "EC_SCRUB_TIME"
            );
            Trace.WriteLine($"EVR prototype EC_SCRUB_TIME timestamp={timestamp}", "MediaFoundation.Core");
        }
    }

    private void Flush()
    {
        CompleteStep(true, 0);
        _ready.Clear();
        _free.Clear();
        for (int i = 0; i < _samples.Length; i++)
            if (_samples[i] != 0)
                _free.Enqueue(i);
        _input = _eos = _preroll = false;
    }

    private void ReleaseType()
    {
        _ready.Clear();
        _free.Clear();
        for (int i = 0; i < _samples.Length; i++)
        {
            if (_samples[i] != 0) ((IMFSample*)_samples[i])->Release();
            _samples[i] = 0;
        }

        if (_mediaType != null)
        {
            _mediaType->Release();
            _mediaType = null;
        }

        if (_latest != null)
        {
            _latest->Release();
            _latest = null;
        }

        _displayedFrames = Frames;
        // WPF holds its own reference until SetBackBuffer confirms replacement or detachment.
        if (_display != null)
        {
            _display->Release();
            _display = null;
        }

        _displayWidth = _displayHeight = 0;
    }

    private void DiagnoseSurface(IDirect3DSurface9* source, string stage, long timestamp)
    {
        D3DSURFACE_DESC description;
        Check(source->GetDesc(&description), "diagnostic.GetDesc");
        int width = checked((int)description.Width), height = checked((int)description.Height);
        IDirect3DSurface9* readback = null;
        try
        {
            Check
            (
                _device->CreateOffscreenPlainSurface
                (
                    (uint)width, (uint)height, D3DFORMAT.D3DFMT_X8R8G8B8,
                    D3DPOOL.D3DPOOL_SYSTEMMEM, &readback, null
                ), "diagnostic.CreateOffscreenPlainSurface"
            );
            Check(_device->GetRenderTargetData(source, readback), "diagnostic.GetRenderTargetData");
            D3DLOCKED_RECT data;
            Check(readback->LockRect(&data, null, 0x10 /* D3DLOCK_READONLY */), "diagnostic.LockRect");
            try
            {
                var colors = new HashSet<uint>();
                int minAlpha = 255, maxAlpha = 0;
                for (int y = height / 4; y < height * 3 / 4; y += 3)
                for (int x = width / 4; x < width * 3 / 4; x += 3)
                {
                    uint pixel = *(uint*)((byte*)data.pBits + y * data.Pitch + x * 4);
                    colors.Add(pixel & 0xFFFFFF);
                    int alpha = (int)(pixel >> 24);
                    minAlpha = Math.Min(minAlpha, alpha);
                    maxAlpha = Math.Max(maxAlpha, alpha);
                }

                Trace.WriteLine
                (
                    $"backend=MFPlay GPU-readback stage={stage} timestamp={timestamp} frames={Frames} size={width}x{height} format=X8R8G8B8 rgbColors={colors.Count} unusedAlpha={minAlpha}..{maxAlpha}",
                    "MediaFoundation.Core"
                );
            }
            finally
            {
                Check(readback->UnlockRect(), "diagnostic.UnlockRect");
            }
        }
        finally
        {
            if (readback != null) readback->Release();
        }
    }

    private void SynchronizeGpu()
    {
        Check(_fence->Issue(PInvoke.D3DISSUE_END), "fence.Issue");
        long deadline = Environment.TickCount64 + 2000;
        HRESULT hr;
        while ((hr = _fence->GetData(null, 0, PInvoke.D3DGETDATA_FLUSH)).Value == 1)
        {
            if (Environment.TickCount64 >= deadline) throw new InvalidOperationException(SR.Get("GpuFenceTimeout"));
            Thread.Yield();
        }

        Check(hr, "fence.GetData");
    }

    private void Pump()
    {
        lock (_gate)
        {
            if (_failure != null || _closed) return;
            Guard(() => Tick());
        }
    }

    internal void RefreshStatus(out int width, out int height)
    {
        lock (_gate)
        {
            if (_failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_failure).Throw();
            width = Width;
            height = Height;
        }
    }

    internal bool Present(Action<nint, int, int, bool> present, bool force = false)
    {
        lock (_gate)
        {
            if (_failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_failure).Throw();
            if (_closed || _latest == null || Frames == 0 || (!force && _displayedFrames == Frames &&
                                                            _displayWidth == _targetWidth &&
                                                            _displayHeight == _targetHeight)) return false;
            // Only the UI, while holding D3DImage.Lock, writes the WPF-owned surface.
            int width = _targetWidth > 0 ? _targetWidth : _mixerWidth;
            int height = _targetHeight > 0 ? _targetHeight : _mixerHeight;
            if (_display == null || _displayWidth != width || _displayHeight != height)
            {
                var replacement = CreateSurface(width, height);
                if (_display != null) _display->Release();
                _display = replacement;
                _displayWidth = width;
                _displayHeight = height;
                _generation++;
                Trace.WriteLine
                (
                    $"EVR display requested={ScalingMode} actual={ScalingMode} native={Width}x{Height} mixer={_mixerWidth}x{_mixerHeight} display={width}x{height} generation={_generation}",
                    "MediaFoundation.Core"
                );
            }

            present((nint)_display, width, height, true);
            bool scaling = _mixerWidth != width || _mixerHeight != height;
            if (scaling)
            {
                D3DCAPS9 caps;
                Check(_device->GetDeviceCaps(&caps), "scaling.GetDeviceCaps");
                const uint LinearMinMag = 0x200 | 0x2000000;
                if (((uint)caps.StretchRectFilterCaps & LinearMinMag) != LinearMinMag)
                    throw new COMException(SR.Get("LinearFilteringUnsupported"), unchecked((int)0x8876086A));
            }

            Check
            (
                _device->StretchRect
                (
                    _latest, null, _display, null,
                    scaling ? D3DTEXTUREFILTERTYPE.D3DTEXF_LINEAR : D3DTEXTUREFILTERTYPE.D3DTEXF_NONE
                ), "present.StretchRect LINEAR"
            );
            SynchronizeGpu();
            if (_displayedFrames < 120 && Frames >= 120 &&
                Environment.GetEnvironmentVariable("MF_DIAGNOSE_FRAME") == "1")
                DiagnoseSurface(_display, "display", 0);
            present((nint)_display, width, height, false);
            _displayedFrames = Frames;
            return true;
        }
    }

    private bool Tick()
    {
        lock (_gate)
        {
            if (_failure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_failure).Throw();
            if (_closed || _mediaType == null) return false;
            if (Environment.TickCount64 >= _nextDiagnostic)
            {
                _nextDiagnostic = Environment.TickCount64 + 5000;
                long diagnosticTime = 0, diagnosticSystem = 0, diagnosticSample = 0;
                var clockHr = _clock == null
                    ? HRESULT.MF_E_INVALIDREQUEST
                    : _clock->GetCorrelatedTime(0, &diagnosticTime, &diagnosticSystem);
                if (_ready.Count != 0)
                    Check
                    (
                        ((IMFSample*)_samples[_ready.Peek()])->GetSampleTime(&diagnosticSample),
                        "diagnostic.GetSampleTime"
                    );
                Trace.WriteLine
                (
                    $"EVR prototype scheduler running={_running} preroll={_preroll} input={_input} notifications={_inputNotifications} outputAttempts={_outputAttempts} ready={_ready.Count} free={_free.Count} frames={Frames} timestamp={diagnosticSample} clock={diagnosticTime} clockHRESULT=0x{clockHr.Value:X8}",
                    "MediaFoundation.Core"
                );
            }

            while (_input && _free.Count != 0)
            {
                int index = _free.Dequeue();
                var output = new MFT_OUTPUT_DATA_BUFFER { pSample = (IMFSample*)_samples[index] };
                uint status = 0;
                long before = 0, after = 0, correlatedMedia = 0;
                if (_clock != null) Check(_clock->GetCorrelatedTime(0, &correlatedMedia, &before), "mixer clock before");
                var hr = _mixer->ProcessOutput(0, 1, &output, &status);
                if (hr.Succeeded && _clock != null && _events != null)
                {
                    Check(_clock->GetCorrelatedTime(0, &correlatedMedia, &after), "mixer clock after");
                    long latency = after - before;
                    Check
                    (
                        _events->Notify((int)PInvoke.EC_PROCESSING_LATENCY, (nint)(&latency), 0), "EC_PROCESSING_LATENCY"
                    );
                }

                _outputAttempts++;
                if (_outputAttempts <= 5)
                    Trace.WriteLine
                    (
                        $"EVR prototype mixer.ProcessOutput attempt={_outputAttempts} HRESULT=0x{hr.Value:X8}",
                        "MediaFoundation.Core"
                    );
                if (output.pEvents != null) output.pEvents->Release();
                if (hr.Failed)
                {
                    _free.Enqueue(index);
                    if (hr == HRESULT.MF_E_TRANSFORM_NEED_MORE_INPUT)
                    {
                        _input = false;
                        break;
                    }

                    Check(hr, "mixer.ProcessOutput");
                }

                _ready.Enqueue(index);
            }

            bool changed = false;
            while (_ready.Count != 0 && (_steps != 0 || (_running && _rate != 0) || !_preroll))
            {
                int index = _ready.Peek();
                var sample = (IMFSample*)_samples[index];
                long timestamp = 0, now = 0, system = 0;
                Check(sample->GetSampleTime(&timestamp), "sample.GetSampleTime");
                // After flush, EVR needs one preroll sample before the clock can advance.
                // OnClockStart can arrive while the presentation clock is still waiting for preroll.
                if (_running && _preroll && _steps == 0)
                {
                    if (_clock == null)
                        throw new InvalidOperationException
                            ("EVR prototype presentation clock unavailable at frame scheduling");
                    Check(_clock->GetCorrelatedTime(0, &now, &system), "clock.GetCorrelatedTime");
                    if (_rate != 0 && (timestamp - now) / _rate > 10000) break;
                }

                _ready.Dequeue();
                if (_steps > 1)
                {
                    _steps--;
                    _free.Enqueue(index);
                    continue;
                }

                IMFMediaBuffer* buffer = null;
                IMFGetService* service = null;
                IDirect3DSurface9* surface = null;
                try
                {
                    Check(sample->GetBufferByIndex(0, &buffer), "sample.GetBufferByIndex");
                    Guid iid = IMFGetService.IID_Guid;
                    Check(buffer->QueryInterface(&iid, (void**)&service), "buffer.IMFGetService");
                    iid = IDirect3DSurface9.IID_Guid;
                    Guid name = PInvoke.MR_BUFFER_SERVICE;
                    Check(service->GetService(&name, &iid, (void**)&surface), "buffer surface");
                    // The staging surface is never attached to WPF. Finish GPU reads before recycling the sample.
                    Check
                    (
                        _device->StretchRect(surface, null, _latest, null, D3DTEXTUREFILTERTYPE.D3DTEXF_NONE),
                        "latest.StretchRect"
                    );
                    SynchronizeGpu();
                    Frames++;
                    changed = _preroll = true;
                    if (Frames == 120 && Environment.GetEnvironmentVariable("MF_DIAGNOSE_FRAME") == "1")
                    {
                        DiagnoseSurface(_latest, "latest", timestamp);
                    }

                    CompleteStep(false, timestamp);
                    if (Frames == 1 || Frames % 120 == 0)
                        Trace.WriteLine
                        (
                            $"EVR prototype actualFrames={Frames} timestamp={timestamp} clock={now} size={Width}x{Height}",
                            "MediaFoundation.Core"
                        );
                }
                finally
                {
                    if (surface != null) surface->Release();
                    if (service != null) service->Release();
                    if (buffer != null) buffer->Release();
                    _free.Enqueue(index);
                }
            }

            if (_eos && !_input && _ready.Count == 0)
            {
                Check(_events->Notify((int)PInvoke.EC_COMPLETE, 0, 0), "EC_COMPLETE");
                _eos = false;
            }

            return changed;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_closed) return;
            _closed = true;
            _scheduler.Dispose();
            ReleaseServices();
            if (_fence != null)
            {
                _fence->Release();
                _fence = null;
            }

            if (_allocator != null)
            {
                _allocator->Release();
                _allocator = null;
            }

            if (_manager != null)
            {
                _manager->Release();
                _manager = null;
            }

            if (_device != null)
            {
                _device->Release();
                _device = null;
            }

            if (_api != null)
            {
                _api->Release();
                _api = null;
            }
        }
    }
}