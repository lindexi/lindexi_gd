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
    private IMFDXGIDeviceManager* manager;
    private EngineCallback? callback;
    private bool disposed, started, comInitialized, stopping;
    private double volume = 1, rate = 1;
    private bool muted, looping;

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
            PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED).ThrowOnFailure();
            comInitialized = true;
            PInvoke.MFStartup(PInvoke.MF_VERSION, 0).ThrowOnFailure();
            started = true;
            uint token;
            IMFDXGIDeviceManager* created = null;
            PInvoke.MFCreateDXGIDeviceManager(&token, &created).ThrowOnFailure();
            manager = created;
            manager->ResetDevice((IUnknown*)device, token).ThrowOnFailure();
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
        set { Verify(); if (!double.IsFinite(value) || value <= 0 || value > float.MaxValue || (float)value == 0) throw new ArgumentOutOfRangeException(nameof(value)); if (engine != null) engine->SetPlaybackRate(value).ThrowOnFailure(); rate = value; }
    }
    /// <inheritdoc/>
    public bool IsLooping { get { Verify(); return looping; } set { Verify(); looping = value; } }

    /// <inheritdoc/>
    public void Open(Uri source)
    {
        Verify(); ArgumentNullException.ThrowIfNull(source);
        if (!source.IsAbsoluteUri) throw new ArgumentException(SR.Get("AbsoluteUriRequired"), nameof(source));
        ReleaseEngine();
        Position = Duration = TimeSpan.Zero; VideoWidth = VideoHeight = 0; HasVideo = HasAudio = false; stopping = false;
        State = PlaybackState.Opening;
        IMFAttributes* attributes = null;
        IMFMediaEngineClassFactory* factory = null;
        try
        {
            callback = new EngineCallback(context, OnEvent);
            PInvoke.MFCreateAttributes(&attributes, 3).ThrowOnFailure();
            var key = PInvoke.MF_MEDIA_ENGINE_CALLBACK;
            attributes->SetUnknown(&key, (IUnknown*)callback.Pointer).ThrowOnFailure();
            key = PInvoke.MF_MEDIA_ENGINE_DXGI_MANAGER;
            attributes->SetUnknown(&key, (IUnknown*)manager).ThrowOnFailure();
            key = PInvoke.MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT;
            attributes->SetUINT32(&key, 87).ThrowOnFailure();
            var clsid = new Guid("B44392DA-499B-446B-A4CB-005FEAD0E6D5");
            var iid = typeof(IMFMediaEngineClassFactory).GUID;
            void* value = null;
            PInvoke.CoCreateInstance(&clsid, null, CLSCTX.CLSCTX_INPROC_SERVER, &iid, &value).ThrowOnFailure();
            factory = (IMFMediaEngineClassFactory*)value;
            IMFMediaEngine* created = null;
            factory->CreateInstance(0, attributes, &created).ThrowOnFailure();
            engine = created;
            engine->SetAutoPlay(false).ThrowOnFailure();
            engine->SetVolume(volume).ThrowOnFailure(); engine->SetMuted(muted).ThrowOnFailure();
            engine->SetPlaybackRate(rate).ThrowOnFailure();
            nint text = Marshal.StringToBSTR(source.AbsoluteUri);
            try { engine->SetSource(new BSTR((char*)text)).ThrowOnFailure(); }
            finally { Marshal.FreeBSTR(text); }
            engine->Load().ThrowOnFailure();
        }
        catch { ReleaseEngine(); State = PlaybackState.Failed; throw; }
        finally { if (factory != null) factory->Release(); if (attributes != null) attributes->Release(); }
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Play() { Ready(); stopping = false; engine->Play().ThrowOnFailure(); }
    /// <inheritdoc/>
    public void Pause() { Ready(); stopping = false; engine->Pause().ThrowOnFailure(); }
    /// <inheritdoc/>
    public void Stop()
    {
        Ready(); engine->Pause().ThrowOnFailure(); stopping = true;
        engine->SetCurrentTime(0).ThrowOnFailure();
        if (!engine->IsSeeking()) ConfirmStop();
    }
    /// <inheritdoc/>
    public void Seek(TimeSpan position) { Ready(); stopping = false; engine->SetCurrentTime(Math.Clamp(position.TotalSeconds, 0, Duration > TimeSpan.Zero ? Duration.TotalSeconds : double.MaxValue)).ThrowOnFailure(); }
    /// <inheritdoc/>
    public void Refresh()
    {
        Verify(); if (engine == null) return;
        Position = ToTime(engine->GetCurrentTime()); Duration = ToTime(engine->GetDuration());
        uint width = 0, height = 0;
        if (engine->HasVideo()) engine->GetNativeVideoSize(&width, &height).ThrowOnFailure();
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
        result.ThrowOnFailure();
        if (result.Value != 0) return false;
        var destination = new RECT(0, 0, width, height);
        var border = new MFARGB { rgbAlpha = 255 };
        engine->TransferVideoFrame((IUnknown*)texture, null, &destination, &border).ThrowOnFailure();
        return true;
    }

    private void OnEvent(uint kind, uint error)
    {
        if (disposed || engine == null || State == PlaybackState.Failed) return;
        switch ((MF_MEDIA_ENGINE_EVENT)kind)
        {
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ERROR:
                var failure = new COMException(SR.Get("NativeFailure"), error == 0 ? unchecked((int)0x80004005) : unchecked((int)error));
                ReleaseEngine(); State = PlaybackState.Failed; StateChanged?.Invoke(this, EventArgs.Empty); Failed?.Invoke(this, new(failure)); break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_LOADEDDATA:
                if (State != PlaybackState.Opening) break;
                Refresh(); State = PlaybackState.Stopped; StateChanged?.Invoke(this, EventArgs.Empty);
                if (!disposed && engine != null) MediaOpened?.Invoke(this, EventArgs.Empty);
                break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PLAYING:
                State = PlaybackState.Playing; StateChanged?.Invoke(this, EventArgs.Empty); break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PAUSE:
                if (!stopping && State != PlaybackState.Stopped) { State = PlaybackState.Paused; StateChanged?.Invoke(this, EventArgs.Empty); } break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_SEEKED:
                if (stopping) ConfirmStop(); else { Refresh(); StateChanged?.Invoke(this, EventArgs.Empty); } break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ENDED:
                State = PlaybackState.Ended; Refresh();
                var current = engine;
                StateChanged?.Invoke(this, EventArgs.Empty); PlaybackEnded?.Invoke(this, EventArgs.Empty);
                if (!disposed && engine == current && looping) { engine->SetCurrentTime(0).ThrowOnFailure(); engine->Play().ThrowOnFailure(); }
                break;
        }
    }
    private void ConfirmStop() { stopping = false; Position = TimeSpan.Zero; State = PlaybackState.Stopped; StateChanged?.Invoke(this, EventArgs.Empty); }
    private static TimeSpan ToTime(double seconds) => double.IsFinite(seconds) && seconds > 0 && seconds < TimeSpan.MaxValue.TotalSeconds ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
    private void Ready() { Verify(); if (engine == null || State is PlaybackState.Opening or PlaybackState.Failed or PlaybackState.Closed) throw new InvalidOperationException(SR.Get("NotReady")); }
    private void Verify() { if (threadId != Environment.CurrentManagedThreadId) throw new InvalidOperationException(SR.Get("WrongThread")); ObjectDisposedException.ThrowIf(disposed, this); }
    private void ReleaseEngine()
    {
        callback?.Dispose(); callback = null;
        if (engine != null) { engine->Shutdown(); engine->Release(); engine = null; }
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
