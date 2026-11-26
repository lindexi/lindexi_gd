using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;

namespace MediaFoundation.Core;

/// <summary>Media Engine frame-server，会话拥有原生音频和媒体时钟，不依赖 WPF。所有调用须在创建 STA 线程执行。</summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows8.0")]
public sealed unsafe class MediaEngineSession : IPlaybackSession
{
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private readonly SynchronizationContext _context;
    private readonly nint _device;
    private IMFMediaEngine* _engine;
    private IMFByteStream* _sourceStream;
    private IMFDXGIDeviceManager* _manager;
    private EngineCallback? _callback;
    private bool _disposed;
    private bool _started;
    private bool _comInitialized;
    private bool _stopping;
    private double _volume = 1;
    private double _rate = 1;
    private bool _muted;
    private bool _looping;
    private bool _playPending;
    private TimeSpan? _seekPending;
    private bool _seekInFlight;

    /// <summary>使用宿主持有的 ID3D11Device；设备必须在会话释放前保持有效。</summary>
    public MediaEngineSession(nint d3d11Device)
    {
        if (d3d11Device == 0) throw new ArgumentNullException(nameof(d3d11Device));
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            throw new InvalidOperationException(SR.Current.StaRequired);
        _context = SynchronizationContext.Current ?? throw new InvalidOperationException(SR.Current.ContextRequired);
        if (_context.GetType() == typeof(SynchronizationContext))
            throw new InvalidOperationException(SR.Current.ContextRequired);
        _device = d3d11Device;
        try
        {
            Check(PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED));
            _comInitialized = true;
            Check(PInvoke.MFStartup(PInvoke.MF_VERSION, 0));
            _started = true;
            uint token;
            IMFDXGIDeviceManager* created = null;
            Check(PInvoke.MFCreateDXGIDeviceManager(&token, &created));
            _manager = created;
            Check(_manager->ResetDevice((IUnknown*)_device, token));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <inheritdoc/>
    public event EventHandler? StateChanged;

    /// <inheritdoc/>
    public event EventHandler? MediaOpened;

    /// <inheritdoc/>
    public event EventHandler? PlaybackEnded;

    /// <inheritdoc/>
    public event EventHandler<PlaybackFailedEventArgs>? Failed;

    /// <inheritdoc/>
    public PlaybackState State { get; private set; }

    /// <inheritdoc/>
    public TimeSpan Position { get; private set; }

    /// <inheritdoc/>
    public TimeSpan Duration { get; private set; }

    /// <inheritdoc/>
    public int VideoWidth { get; private set; }

    /// <inheritdoc/>
    public int VideoHeight { get; private set; }

    /// <inheritdoc/>
    public bool HasVideo { get; private set; }

    /// <inheritdoc/>
    public bool HasAudio { get; private set; }

    /// <inheritdoc/>
    public double Volume
    {
        get
        {
            Verify();
            return _volume;
        }
        set
        {
            Verify();
            if (!double.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value));
            if (_engine != null) _engine->SetVolume(value).ThrowOnFailure();
            _volume = value;
        }
    }

    /// <inheritdoc/>
    public bool IsMuted
    {
        get
        {
            Verify();
            return _muted;
        }
        set
        {
            Verify();
            if (_engine != null) _engine->SetMuted(value).ThrowOnFailure();
            _muted = value;
        }
    }

    /// <inheritdoc/>
    public double PlaybackRate
    {
        get
        {
            Verify();
            return _rate;
        }
        set
        {
            Verify();
            if (!double.IsFinite(value) || value <= 0 || value > float.MaxValue || (float)value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (_engine != null) _engine->SetPlaybackRate(value).ThrowOnFailure();
            _rate = value;
            System.Diagnostics.Trace.WriteLine($"MediaEngine rate={_rate} position={Position}", "MediaFoundation.Core");
        }
    }

    /// <inheritdoc/>
    public bool IsLooping
    {
        get
        {
            Verify();
            return _looping;
        }
        set
        {
            Verify();
            _looping = value;
        }
    }

    /// <inheritdoc/>
    public void Open(Uri source)
    {
        Verify();
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsAbsoluteUri) throw new ArgumentException(SR.Current.AbsoluteUriRequired, nameof(source));
        System.Diagnostics.Trace.WriteLine
            ($"MediaEngine Open {SourceDiagnostics.Describe(source, source.AbsoluteUri)}", "MediaFoundation.Core");
        ReleaseEngine();
        Position = Duration = TimeSpan.Zero;
        VideoWidth = VideoHeight = 0;
        HasVideo = HasAudio = false;
        _stopping = false;
        State = PlaybackState.Opening;
        IMFAttributes* attributes = null;
        IMFMediaEngineClassFactory* factory = null;
        try
        {
            _callback = new EngineCallback(_context, OnEvent);
            Check(PInvoke.MFCreateAttributes(&attributes, 3));
            var key = PInvoke.MF_MEDIA_ENGINE_CALLBACK;
            Check
            (
                attributes->SetUnknown(&key, (IUnknown*)_callback.Pointer),
                "IMFAttributes.SetUnknown(MF_MEDIA_ENGINE_CALLBACK)"
            );
            key = PInvoke.MF_MEDIA_ENGINE_DXGI_MANAGER;
            Check
            (
                attributes->SetUnknown(&key, (IUnknown*)_manager),
                "IMFAttributes.SetUnknown(MF_MEDIA_ENGINE_DXGI_MANAGER)"
            );
            key = PInvoke.MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT;
            Check
            (
                attributes->SetUINT32(&key, 87),
                "IMFAttributes.SetUINT32(MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT=B8G8R8A8_UNORM)"
            );
            var clsid = new Guid("B44392DA-499B-446B-A4CB-005FEAD0E6D5");
            var iid = typeof(IMFMediaEngineClassFactory).GUID;
            void* value = null;
            Check
            (
                PInvoke.CoCreateInstance(&clsid, null, CLSCTX.CLSCTX_INPROC_SERVER, &iid, &value),
                "CoCreateInstance(CLSID_MFMediaEngineClassFactory, IMFMediaEngineClassFactory)"
            );
            factory = (IMFMediaEngineClassFactory*)value;
            IMFMediaEngine* created = null;
            System.Diagnostics.Trace.WriteLine
            (
                "MediaEngine frame-server flags=0 DISABLE_LOCAL_PLUGINS=False DXGI_MANAGER=True output=B8G8R8A8_UNORM; decoder candidates do not prove D3D11/profile support",
                "MediaFoundation.Core"
            );
            Check
            (
                factory->CreateInstance(0, attributes, &created),
                "IMFMediaEngineClassFactory.CreateInstance(frame-server)"
            );
            _engine = created;
            Check(_engine->SetAutoPlay(false));
            Check(_engine->SetVolume(_volume));
            Check(_engine->SetMuted(_muted));
            Check(_engine->SetPlaybackRate(_rate));
            if (source.IsFile)
            {
                IMFByteStream* stream = null;
                IMFMediaEngineEx* extended = null;
                try
                {
                    string path = source.LocalPath;
                    fixed (char* file = path)
                        Check
                        (
                            PInvoke.MFCreateFile
                            (
                                MF_FILE_ACCESSMODE.MF_ACCESSMODE_READ, MF_FILE_OPENMODE.MF_OPENMODE_FAIL_IF_NOT_EXIST,
                                MF_FILE_FLAGS.MF_FILEFLAGS_NONE, new PCWSTR(file), &stream
                            ), "MFCreateFile(local)"
                        );
                    _sourceStream = stream;
                    stream = null;
                    string extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
                    string? mime = extension switch
                    {
                        ".mp4" or ".m4v" or ".mov" => "video/mp4",
                        ".mp3" => "audio/mpeg",
                        ".wmv" => "video/x-ms-wmv",
                        ".wma" => "audio/x-ms-wma",
                        ".wav" => "audio/wav",
                        _ => null
                    };
                    if (mime != null)
                    {
                        var attributesId = typeof(IMFAttributes).GUID;
                        void* streamAttributes = null;
                        Check
                        (
                            _sourceStream->QueryInterface(&attributesId, &streamAttributes),
                            "IMFByteStream.QueryInterface(IMFAttributes)"
                        );
                        try
                        {
                            var contentType = PInvoke.MF_BYTESTREAM_CONTENT_TYPE;
                            fixed (char* text =
                                       mime)
                                Check
                                (
                                    ((IMFAttributes*)streamAttributes)->SetString(&contentType, new PCWSTR(text)),
                                    "IMFByteStream.SetString(MF_BYTESTREAM_CONTENT_TYPE)"
                                );
                        }
                        finally
                        {
                            ((IMFAttributes*)streamAttributes)->Release();
                        }
                    }

                    var extendedId = typeof(IMFMediaEngineEx).GUID;
                    void* valueEx = null;
                    Check(_engine->QueryInterface(&extendedId, &valueEx), "QueryInterface(IMFMediaEngineEx)");
                    extended = (IMFMediaEngineEx*)valueEx;
                    string hintExtension = extension.Length <= 16 && extension.All
                        (c => char.IsAsciiLetterOrDigit(c) || c == '.')
                        ? extension
                        : "";
                    System.Diagnostics.Trace.WriteLine
                    (
                        $"MediaEngine local byte stream contentTypeHint={mime ?? "unset"} anonymousUrlExtension={hintExtension}",
                        "MediaFoundation.Core"
                    );
                    nint hint = Marshal.StringToBSTR("media" + hintExtension);
                    try
                    {
                        Check
                        (
                            extended->SetSourceFromByteStream(_sourceStream, new BSTR((char*)hint)),
                            "IMFMediaEngineEx.SetSourceFromByteStream(local; no Load)"
                        );
                    }
                    finally
                    {
                        Marshal.FreeBSTR(hint);
                    }
                }
                finally
                {
                    if (extended != null) extended->Release();
                    if (stream != null) stream->Release();
                }
            }
            else
            {
                nint text = Marshal.StringToBSTR(source.AbsoluteUri);
                try
                {
                    Check(_engine->SetSource(new BSTR((char*)text)), "IMFMediaEngine.SetSource");
                }
                finally
                {
                    Marshal.FreeBSTR(text);
                }

                Check(_engine->Load());
            }
        }
        catch
        {
            ReleaseEngine();
            State = PlaybackState.Failed;
            throw;
        }
        finally
        {
            if (factory != null) factory->Release();
            if (attributes != null) attributes->Release();
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Play()
    {
        Ready();
        System.Diagnostics.Trace.WriteLine
            ($"MediaEngine Play position={Position} rate={_rate} seeking={_seekInFlight}", "MediaFoundation.Core");
        _stopping = false;
        if (_seekInFlight || _engine->IsSeeking())
        {
            _playPending = true;
            return;
        }

        _engine->Play().ThrowOnFailure();
    }

    /// <inheritdoc/>
    public void Pause()
    {
        Ready();
        System.Diagnostics.Trace.WriteLine($"MediaEngine Pause position={Position}", "MediaFoundation.Core");
        _playPending = false;
        _stopping = false;
        _engine->Pause().ThrowOnFailure();
    }

    /// <inheritdoc/>
    public void Stop()
    {
        Ready();
        _playPending = false;
        _engine->Pause().ThrowOnFailure();
        _stopping = true;
        if (_seekInFlight || _engine->IsSeeking())
        {
            _seekPending = TimeSpan.Zero;
            return;
        }

        BeginSeek(TimeSpan.Zero);
        if (!_seekInFlight) ConfirmStop();
    }

    /// <inheritdoc/>
    public void Seek(TimeSpan position)
    {
        Ready();
        _stopping = false;
        System.Diagnostics.Trace.WriteLine
            ($"MediaEngine Seek requested={position} current={Position} rate={_rate}", "MediaFoundation.Core");
        if (_seekInFlight || _engine->IsSeeking())
        {
            _seekPending = position;
            return;
        }

        BeginSeek(position);
    }

    /// <inheritdoc/>
    public void Refresh()
    {
        Verify();
        if (_engine == null || State == PlaybackState.Opening) return;
        Position = ToTime(_engine->GetCurrentTime());
        Duration = ToTime(_engine->GetDuration());
        uint width = 0, height = 0;
        if (_engine->HasVideo()) Check(_engine->GetNativeVideoSize(&width, &height));
        VideoWidth = checked((int)width);
        VideoHeight = checked((int)height);
        HasVideo = _engine->HasVideo();
        HasAudio = _engine->HasAudio();
    }

    /// <inheritdoc/>
    public void ResizeVideo() => Verify();

    /// <summary>将新视频帧传入宿主的 ID3D11Texture2D（BGRA render target）；返回 false 表示当前无新帧。</summary>
    public bool TransferFrame(nint texture, int width, int height)
    {
        Verify();
        if (texture == 0) throw new ArgumentNullException(nameof(texture));
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (_engine == null || !HasVideo || State is PlaybackState.Opening or PlaybackState.Failed) return false;
        long timestamp;
        var result = _engine->OnVideoStreamTick(&timestamp);
        Check(result, "IMFMediaEngine.OnVideoStreamTick");
        if (result.Value != 0 && !_engine->IsSeeking()) return false;
        var destination = new RECT(0, 0, width, height);
        var border = new MFARGB { rgbAlpha = 255 };
        Check(_engine->TransferVideoFrame((IUnknown*)texture, null, &destination, &border));
        return true;
    }

    private static void Check
        (HRESULT result, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(result))] string stage = "")
    {
        if (result.Value < 0 || (!stage.Contains("OnVideoStreamTick", StringComparison.Ordinal) &&
                                 !stage.Contains("TransferVideoFrame", StringComparison.Ordinal) && !stage.Contains
                                     ("GetNativeVideoSize", StringComparison.Ordinal)))
            System.Diagnostics.Trace.WriteLine
                ($"MediaEngine stage={stage} HRESULT=0x{result.Value:X8}", "MediaFoundation.Core");
        if (result.Value < 0)
            throw new COMException(SR.Current.NativeStageFailure.ToString(stage, result.Value.ToString("X8", System.Globalization.CultureInfo.InvariantCulture)), result.Value);
    }

    private void OnEvent(uint kind, nuint mediaError, uint error)
    {
        if (_disposed || _engine == null || State == PlaybackState.Failed) return;
        if ((MF_MEDIA_ENGINE_EVENT)kind != MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_TIMEUPDATE)
            System.Diagnostics.Trace.WriteLine
            (
                $"MediaEngine event={(MF_MEDIA_ENGINE_EVENT)kind} state={State} seeking={_engine->IsSeeking()} paused={_engine->IsPaused()} time={_engine->GetCurrentTime():F3}",
                "MediaFoundation.Core"
            );
        switch ((MF_MEDIA_ENGINE_EVENT)kind)
        {
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ERROR:
                // ERROR param1 is MF_MEDIA_ENGINE_ERR, not an HRESULT; param2 is the extended HRESULT.
                var code = unchecked((int)error);
                if (code >= 0) code = unchecked((int)0x80004005);
                System.Diagnostics.Trace.WriteLine
                (
                    $"MediaEngine stage=IMFMediaEngineNotify.ERROR mediaError={mediaError} mediaErrorName={(MF_MEDIA_ENGINE_ERR)mediaError} param2=0x{error:X8} HRESULT=0x{code:X8} nativeError={(code == HRESULT.MF_E_TOPO_CODEC_NOT_FOUND.Value ? nameof(HRESULT.MF_E_TOPO_CODEC_NOT_FOUND) : "unclassified")}",
                    "MediaFoundation.Core"
                );
                var failure = new COMException
                    (SR.Current.NativeStageFailure.ToString("IMFMediaEngineNotify.ERROR", code.ToString("X8", System.Globalization.CultureInfo.InvariantCulture)), code);
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.SetCurrentStackTrace(failure);
                System.Diagnostics.Trace.WriteLine
                    ($"MediaEngine failure HRESULT=0x{code:X8} {failure}", "MediaFoundation.Core");
                ReleaseEngine();
                State = PlaybackState.Failed;
                StateChanged?.Invoke(this, EventArgs.Empty);
                Failed?.Invoke(this, new(failure));
                break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_LOADEDDATA:
                if (State != PlaybackState.Opening) break;
                State = PlaybackState.Stopped;
                Refresh();
                StateChanged?.Invoke(this, EventArgs.Empty);
                if (!_disposed && _engine != null) MediaOpened?.Invoke(this, EventArgs.Empty);
                break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PLAYING:
                State = PlaybackState.Playing;
                StateChanged?.Invoke(this, EventArgs.Empty);
                break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PAUSE:
                if (!_stopping && State != PlaybackState.Stopped)
                {
                    State = PlaybackState.Paused;
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }

                break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_SEEKED:
                if (_engine->IsSeeking()) break;
                _seekInFlight = false;
                Refresh();
                if (_seekPending is { } target)
                {
                    _seekPending = null;
                    BeginSeek(target);
                    if (_seekInFlight) break;
                }

                if (_stopping) ConfirmStop();
                else
                {
                    Refresh();
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }

                if (_playPending)
                {
                    _playPending = false;
                    _engine->Play().ThrowOnFailure();
                }

                break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ENDED:
                State = PlaybackState.Ended;
                Refresh();
                var current = _engine;
                StateChanged?.Invoke(this, EventArgs.Empty);
                PlaybackEnded?.Invoke(this, EventArgs.Empty);
                if (!_disposed && _engine == current && _looping)
                {
                    _engine->SetCurrentTime(0).ThrowOnFailure();
                    _engine->Play().ThrowOnFailure();
                }

                break;
        }
    }

    private void BeginSeek(TimeSpan position)
    {
        _engine->SetCurrentTime
            (Math.Clamp(position.TotalSeconds, 0, Duration > TimeSpan.Zero ? Duration.TotalSeconds : double.MaxValue))
            .ThrowOnFailure();
        _seekInFlight = _engine->IsSeeking();
    }

    private void ConfirmStop()
    {
        _stopping = false;
        Position = TimeSpan.Zero;
        State = PlaybackState.Stopped;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static TimeSpan ToTime
        (double seconds) => double.IsFinite(seconds) && seconds > 0 && seconds < TimeSpan.MaxValue.TotalSeconds
        ? TimeSpan.FromSeconds(seconds)
        : TimeSpan.Zero;

    private void Ready()
    {
        Verify();
        if (_engine == null || State is PlaybackState.Opening or PlaybackState.Failed or PlaybackState.Closed)
            throw new InvalidOperationException(SR.Current.NotReady);
    }

    private void Verify()
    {
        if (_threadId != Environment.CurrentManagedThreadId) throw new InvalidOperationException(SR.Current.WrongThread);
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void ReleaseEngine()
    {
        _playPending = false;
        _seekPending = null;
        _seekInFlight = false;
        _callback?.Dispose();
        _callback = null;
        if (_engine != null)
        {
            _engine->Shutdown();
            _engine->Release();
            _engine = null;
        }

        if (_sourceStream != null)
        {
            _sourceStream->Release();
            _sourceStream = null;
            System.Diagnostics.Trace.WriteLine("MediaEngine source stream released", "MediaFoundation.Core");
        }
    }

    /// <summary>关闭通知、关闭引擎，再释放 DXGI manager 和 MF/COM 初始化引用。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        Verify();
        _disposed = true;
        ReleaseEngine();
        if (_manager != null)
        {
            _manager->Release();
            _manager = null;
        }

        if (_started)
        {
            PInvoke.MFShutdown();
            _started = false;
        }

        if (_comInitialized)
        {
            PInvoke.CoUninitialize();
            _comInitialized = false;
        }
    }
}