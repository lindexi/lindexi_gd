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
    private readonly int threadId = Environment.CurrentManagedThreadId;
    private readonly SynchronizationContext context;
    private readonly nint device;
    private IMFMediaEngine* engine;
    private IMFByteStream* sourceStream;
    private IMFDXGIDeviceManager* manager;
    private EngineCallback? callback;
    private bool disposed, started, comInitialized, stopping;
    private double volume = 1, rate = 1;
    private bool muted, looping, playPending;
    private TimeSpan? seekPending;
    private bool seekInFlight;

    /// <summary>使用宿主持有的 ID3D11Device；设备必须在会话释放前保持有效。</summary>
    public MediaEngineSession(nint d3d11Device)
    {
        if (d3d11Device == 0) throw new ArgumentNullException(nameof(d3d11Device));
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) throw new InvalidOperationException(SR.Get("StaRequired"));
        context = SynchronizationContext.Current ?? throw new InvalidOperationException(SR.Get("ContextRequired"));
        if (context.GetType() == typeof(SynchronizationContext)) throw new InvalidOperationException(SR.Get("ContextRequired"));
        device = d3d11Device;
        try
        {
            Check(PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED));
            comInitialized = true;
            Check(PInvoke.MFStartup(PInvoke.MF_VERSION, 0));
            started = true;
            uint token;
            IMFDXGIDeviceManager* created = null;
            Check(PInvoke.MFCreateDXGIDeviceManager(&token, &created));
            manager = created;
            Check(manager->ResetDevice((IUnknown*)device, token));
        }
        catch { Dispose(); throw; }
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
        get { Verify(); return volume; }
        set { Verify(); if (!double.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); if (engine != null) engine->SetVolume(value).ThrowOnFailure(); volume = value; }
    }
    /// <inheritdoc/>
    public bool IsMuted
    {
        get { Verify(); return muted; }
        set { Verify(); if (engine != null) engine->SetMuted(value).ThrowOnFailure(); muted = value; }
    }
    /// <inheritdoc/>
    public double PlaybackRate
    {
        get { Verify(); return rate; }
        set { Verify(); if (!double.IsFinite(value) || value <= 0 || value > float.MaxValue || (float)value == 0) throw new ArgumentOutOfRangeException(nameof(value)); if (engine != null) engine->SetPlaybackRate(value).ThrowOnFailure(); rate = value; System.Diagnostics.Trace.WriteLine($"MediaEngine rate={rate} position={Position}", "MediaFoundation.Core"); }
    }
    /// <inheritdoc/>
    public bool IsLooping { get { Verify(); return looping; } set { Verify(); looping = value; } }

    /// <inheritdoc/>
    public void Open(Uri source)
    {
        Verify(); ArgumentNullException.ThrowIfNull(source);
        if (!source.IsAbsoluteUri) throw new ArgumentException(SR.Get("AbsoluteUriRequired"), nameof(source));
        System.Diagnostics.Trace.WriteLine($"MediaEngine Open {SourceDiagnostics.Describe(source, source.AbsoluteUri)}", "MediaFoundation.Core");
        ReleaseEngine();
        Position = Duration = TimeSpan.Zero; VideoWidth = VideoHeight = 0; HasVideo = HasAudio = false; stopping = false;
        State = PlaybackState.Opening;
        IMFAttributes* attributes = null;
        IMFMediaEngineClassFactory* factory = null;
        try
        {
            callback = new EngineCallback(context, OnEvent);
            Check(PInvoke.MFCreateAttributes(&attributes, 3));
            var key = PInvoke.MF_MEDIA_ENGINE_CALLBACK;
            Check(attributes->SetUnknown(&key, (IUnknown*)callback.Pointer), "IMFAttributes.SetUnknown(MF_MEDIA_ENGINE_CALLBACK)");
            key = PInvoke.MF_MEDIA_ENGINE_DXGI_MANAGER;
            Check(attributes->SetUnknown(&key, (IUnknown*)manager), "IMFAttributes.SetUnknown(MF_MEDIA_ENGINE_DXGI_MANAGER)");
            key = PInvoke.MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT;
            Check(attributes->SetUINT32(&key, 87), "IMFAttributes.SetUINT32(MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT=B8G8R8A8_UNORM)");
            var clsid = new Guid("B44392DA-499B-446B-A4CB-005FEAD0E6D5");
            var iid = typeof(IMFMediaEngineClassFactory).GUID;
            void* value = null;
            Check(PInvoke.CoCreateInstance(&clsid, null, CLSCTX.CLSCTX_INPROC_SERVER, &iid, &value), "CoCreateInstance(CLSID_MFMediaEngineClassFactory, IMFMediaEngineClassFactory)");
            factory = (IMFMediaEngineClassFactory*)value;
            IMFMediaEngine* created = null;
            System.Diagnostics.Trace.WriteLine("MediaEngine frame-server flags=0 DISABLE_LOCAL_PLUGINS=False DXGI_MANAGER=True output=B8G8R8A8_UNORM; decoder candidates do not prove D3D11/profile support", "MediaFoundation.Core");
            Check(factory->CreateInstance(0, attributes, &created), "IMFMediaEngineClassFactory.CreateInstance(frame-server)");
            engine = created;
            Check(engine->SetAutoPlay(false));
            Check(engine->SetVolume(volume)); Check(engine->SetMuted(muted));
            Check(engine->SetPlaybackRate(rate));
            if (source.IsFile)
            {
                IMFByteStream* stream = null;
                IMFMediaEngineEx* extended = null;
                try
                {
                    string path = source.LocalPath;
                    fixed (char* file = path) Check(PInvoke.MFCreateFile(MF_FILE_ACCESSMODE.MF_ACCESSMODE_READ, MF_FILE_OPENMODE.MF_OPENMODE_FAIL_IF_NOT_EXIST, MF_FILE_FLAGS.MF_FILEFLAGS_NONE, new PCWSTR(file), &stream), "MFCreateFile(local)");
                    sourceStream = stream;
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
                        Check(sourceStream->QueryInterface(&attributesId, &streamAttributes), "IMFByteStream.QueryInterface(IMFAttributes)");
                        try
                        {
                            var contentType = PInvoke.MF_BYTESTREAM_CONTENT_TYPE;
                            fixed (char* text = mime) Check(((IMFAttributes*)streamAttributes)->SetString(&contentType, new PCWSTR(text)), "IMFByteStream.SetString(MF_BYTESTREAM_CONTENT_TYPE)");
                        }
                        finally { ((IMFAttributes*)streamAttributes)->Release(); }
                    }
                    var extendedId = typeof(IMFMediaEngineEx).GUID;
                    void* valueEx = null;
                    Check(engine->QueryInterface(&extendedId, &valueEx), "QueryInterface(IMFMediaEngineEx)");
                    extended = (IMFMediaEngineEx*)valueEx;
                    string hintExtension = extension.Length <= 16 && extension.All(c => char.IsAsciiLetterOrDigit(c) || c == '.') ? extension : "";
                    System.Diagnostics.Trace.WriteLine($"MediaEngine local byte stream contentTypeHint={mime ?? "unset"} anonymousUrlExtension={hintExtension}", "MediaFoundation.Core");
                    nint hint = Marshal.StringToBSTR("media" + hintExtension);
                    try { Check(extended->SetSourceFromByteStream(sourceStream, new BSTR((char*)hint)), "IMFMediaEngineEx.SetSourceFromByteStream(local; no Load)"); }
                    finally { Marshal.FreeBSTR(hint); }
                }
                finally { if (extended != null) extended->Release(); if (stream != null) stream->Release(); }
            }
            else
            {
                nint text = Marshal.StringToBSTR(source.AbsoluteUri);
                try { Check(engine->SetSource(new BSTR((char*)text)), "IMFMediaEngine.SetSource"); }
                finally { Marshal.FreeBSTR(text); }
                Check(engine->Load());
            }
        }
        catch { ReleaseEngine(); State = PlaybackState.Failed; throw; }
        finally { if (factory != null) factory->Release(); if (attributes != null) attributes->Release(); }
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Play() { Ready(); System.Diagnostics.Trace.WriteLine($"MediaEngine Play position={Position} rate={rate} seeking={seekInFlight}", "MediaFoundation.Core"); stopping = false; if (seekInFlight || engine->IsSeeking()) { playPending = true; return; } engine->Play().ThrowOnFailure(); }
    /// <inheritdoc/>
    public void Pause() { Ready(); System.Diagnostics.Trace.WriteLine($"MediaEngine Pause position={Position}", "MediaFoundation.Core"); playPending = false; stopping = false; engine->Pause().ThrowOnFailure(); }
    /// <inheritdoc/>
    public void Stop()
    {
        Ready(); playPending = false; engine->Pause().ThrowOnFailure(); stopping = true;
        if (seekInFlight || engine->IsSeeking()) { seekPending = TimeSpan.Zero; return; }
        BeginSeek(TimeSpan.Zero);
        if (!seekInFlight) ConfirmStop();
    }
    /// <inheritdoc/>
    public void Seek(TimeSpan position)
    {
        Ready(); stopping = false;
        System.Diagnostics.Trace.WriteLine($"MediaEngine Seek requested={position} current={Position} rate={rate}", "MediaFoundation.Core");
        if (seekInFlight || engine->IsSeeking()) { seekPending = position; return; }
        BeginSeek(position);
    }
    /// <inheritdoc/>
    public void Refresh()
    {
        Verify(); if (engine == null || State == PlaybackState.Opening) return;
        Position = ToTime(engine->GetCurrentTime()); Duration = ToTime(engine->GetDuration());
        uint width = 0, height = 0;
        if (engine->HasVideo()) Check(engine->GetNativeVideoSize(&width, &height));
        VideoWidth = checked((int)width); VideoHeight = checked((int)height);
        HasVideo = engine->HasVideo(); HasAudio = engine->HasAudio();
    }
    /// <inheritdoc/>
    public void ResizeVideo() => Verify();

    /// <summary>将新视频帧传入宿主的 ID3D11Texture2D（BGRA render target）；返回 false 表示当前无新帧。</summary>
    public bool TransferFrame(nint texture, int width, int height)
    {
        Verify(); if (texture == 0) throw new ArgumentNullException(nameof(texture));
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (engine == null || !HasVideo || State is PlaybackState.Opening or PlaybackState.Failed) return false;
        long timestamp;
        var result = engine->OnVideoStreamTick(&timestamp);
        Check(result, "IMFMediaEngine.OnVideoStreamTick");
        if (result.Value != 0 && !engine->IsSeeking()) return false;
        var destination = new RECT(0, 0, width, height);
        var border = new MFARGB { rgbAlpha = 255 };
        Check(engine->TransferVideoFrame((IUnknown*)texture, null, &destination, &border));
        return true;
    }

    private static void Check(HRESULT result, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(result))] string stage = "")
    {
        if (result.Value < 0 || (!stage.Contains("OnVideoStreamTick", StringComparison.Ordinal) && !stage.Contains("TransferVideoFrame", StringComparison.Ordinal) && !stage.Contains("GetNativeVideoSize", StringComparison.Ordinal)))
            System.Diagnostics.Trace.WriteLine($"MediaEngine stage={stage} HRESULT=0x{result.Value:X8}", "MediaFoundation.Core");
        if (result.Value < 0) throw new COMException(string.Format(SR.Get("NativeStageFailure"), stage, result.Value), result.Value);
    }

    private void OnEvent(uint kind, nuint mediaError, uint error)
    {
        if (disposed || engine == null || State == PlaybackState.Failed) return;
        if ((MF_MEDIA_ENGINE_EVENT)kind != MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_TIMEUPDATE)
        System.Diagnostics.Trace.WriteLine($"MediaEngine event={(MF_MEDIA_ENGINE_EVENT)kind} state={State} seeking={engine->IsSeeking()} paused={engine->IsPaused()} time={engine->GetCurrentTime():F3}", "MediaFoundation.Core");
        switch ((MF_MEDIA_ENGINE_EVENT)kind)
        {
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ERROR:
                // ERROR param1 is MF_MEDIA_ENGINE_ERR, not an HRESULT; param2 is the extended HRESULT.
                var code = unchecked((int)error);
                if (code >= 0) code = unchecked((int)0x80004005);
                System.Diagnostics.Trace.WriteLine($"MediaEngine stage=IMFMediaEngineNotify.ERROR mediaError={mediaError} mediaErrorName={(MF_MEDIA_ENGINE_ERR)mediaError} param2=0x{error:X8} HRESULT=0x{code:X8} nativeError={(code == HRESULT.MF_E_TOPO_CODEC_NOT_FOUND.Value ? nameof(HRESULT.MF_E_TOPO_CODEC_NOT_FOUND) : "unclassified")}", "MediaFoundation.Core");
                var failure = new COMException(string.Format(SR.Get("NativeStageFailure"), "IMFMediaEngineNotify.ERROR", code), code);
                System.Diagnostics.Trace.WriteLine($"MediaEngine failure HRESULT=0x{code:X8} {failure}", "MediaFoundation.Core");
                ReleaseEngine(); State = PlaybackState.Failed; StateChanged?.Invoke(this, EventArgs.Empty); Failed?.Invoke(this, new(failure)); break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_LOADEDDATA:
                if (State != PlaybackState.Opening) break;
                State = PlaybackState.Stopped; Refresh(); StateChanged?.Invoke(this, EventArgs.Empty);
                if (!disposed && engine != null) MediaOpened?.Invoke(this, EventArgs.Empty);
                break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PLAYING:
                State = PlaybackState.Playing; StateChanged?.Invoke(this, EventArgs.Empty); break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PAUSE:
                if (!stopping && State != PlaybackState.Stopped) { State = PlaybackState.Paused; StateChanged?.Invoke(this, EventArgs.Empty); } break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_SEEKED:
                if (engine->IsSeeking()) break;
                seekInFlight = false;
                Refresh();
                if (seekPending is { } target)
                {
                    seekPending = null;
                    BeginSeek(target);
                    if (seekInFlight) break;
                }
                if (stopping) ConfirmStop(); else { Refresh(); StateChanged?.Invoke(this, EventArgs.Empty); }
                if (playPending) { playPending = false; engine->Play().ThrowOnFailure(); }
                break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ENDED:
                State = PlaybackState.Ended; Refresh();
                var current = engine;
                StateChanged?.Invoke(this, EventArgs.Empty); PlaybackEnded?.Invoke(this, EventArgs.Empty);
                if (!disposed && engine == current && looping) { engine->SetCurrentTime(0).ThrowOnFailure(); engine->Play().ThrowOnFailure(); }
                break;
        }
    }
    private void BeginSeek(TimeSpan position)
    {
        engine->SetCurrentTime(Math.Clamp(position.TotalSeconds, 0, Duration > TimeSpan.Zero ? Duration.TotalSeconds : double.MaxValue)).ThrowOnFailure();
        seekInFlight = engine->IsSeeking();
    }
    private void ConfirmStop() { stopping = false; Position = TimeSpan.Zero; State = PlaybackState.Stopped; StateChanged?.Invoke(this, EventArgs.Empty); }
    private static TimeSpan ToTime(double seconds) => double.IsFinite(seconds) && seconds > 0 && seconds < TimeSpan.MaxValue.TotalSeconds ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
    private void Ready() { Verify(); if (engine == null || State is PlaybackState.Opening or PlaybackState.Failed or PlaybackState.Closed) throw new InvalidOperationException(SR.Get("NotReady")); }
    private void Verify() { if (threadId != Environment.CurrentManagedThreadId) throw new InvalidOperationException(SR.Get("WrongThread")); ObjectDisposedException.ThrowIf(disposed, this); }
    private void ReleaseEngine()
    {
        playPending = false; seekPending = null; seekInFlight = false;
        callback?.Dispose(); callback = null;
        if (engine != null) { engine->Shutdown(); engine->Release(); engine = null; }
        if (sourceStream != null) { sourceStream->Release(); sourceStream = null; System.Diagnostics.Trace.WriteLine("MediaEngine source stream released", "MediaFoundation.Core"); }
    }
    /// <summary>关闭通知、关闭引擎，再释放 DXGI manager 和 MF/COM 初始化引用。</summary>
    public void Dispose()
    {
        if (disposed) return; Verify(); disposed = true;
        ReleaseEngine();
        if (manager != null) { manager->Release(); manager = null; }
        if (started) { PInvoke.MFShutdown(); started = false; }
        if (comInitialized) { PInvoke.CoUninitialize(); comInitialized = false; }
    }
}
