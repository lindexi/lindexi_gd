using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;

namespace MediaFoundation.Core;

/// <summary>
/// 使用 MFPlay 的 HWND 视频渲染器及原生音频输出、媒体时钟播放媒体。
/// 所有成员（包括 Dispose）必须在构造线程调用；该线程必须是有消息循环和专用
/// SynchronizationContext 的 STA。宿主必须先释放会话再销毁 HWND。
/// </summary>
public sealed unsafe class MediaPlaybackSession : IPlaybackSession
{
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private readonly SynchronizationContext _context;
    private readonly HWND _window;
    private IMFPMediaPlayer* _player;
    private NativeCallback? _callback;

    /// <summary>实验 surface 释放前在会话线程通知宿主；宿主必须同步解绑 D3DImage。</summary>
    public event EventHandler? PrototypeSurfaceReleasing;

    private VideoScalingMode _scalingMode = VideoScalingMode.MixerOutput;

    /// <summary>创建指定缩放位置的 MFPlay D3DImage 会话；未指定策略的无参入口默认 MixerOutput。</summary>
    public static MediaPlaybackSession CreateD3DImageSession(VideoScalingMode scalingMode)
    {
        if (!Enum.IsDefined(scalingMode)) throw new ArgumentOutOfRangeException(nameof(scalingMode));
        var session = CreateD3DImageSession();
        session._scalingMode = scalingMode;
        return session;
    }

    /// <summary>源、mixer 和展示表面的独立尺寸；尚未协商时为 null。</summary>
    public VideoSurfaceInfo? VideoSurfaceInfo
    {
        get
        {
            Verify();
            return _prototypePresenter?.SurfaceInfo;
        }
    }

    /// <summary>在会话线程请求视频内容的整数像素尺寸。零尺寸不分配；同值去重，不影响 HWND。</summary>
    public void RequestVideoTarget(int width, int height)
    {
        Verify();
        if (width < 0 || height < 0) throw new ArgumentOutOfRangeException(nameof(width));
        _prototypePresenter?.RequestTarget(width, height);
    }

    private bool _prototype;
    private EVRPrototypePresenter? _prototypePresenter;
    private IMFMediaSink* _prototypeSink;

    /// <summary>创建 MFPlay 与自定义 EVR 会话；媒体时钟和 staging 输出不依赖宿主呈现频率。</summary>
    /// <remarks>宿主须在创建线程持有展示表面锁时调用 CopyLatestFrame，并在 SurfaceReleasing 中同步解绑表面。</remarks>
    public static MediaPlaybackSession CreateD3DImageSession() => CreateD3DImagePrototype
        ((nint)PInvoke.GetDesktopWindow().Value);

    /// <summary>展示表面释放前同步通知；宿主必须先停止呈现并解绑表面。</summary>
    public event EventHandler? SurfaceReleasing;

    /// <summary>在宿主展示锁内将最新 staging 帧复制到 D3D9 展示表面，并等待 GPU 完成。</summary>
    /// <param name="present">复制前以 true 通知绑定表面，复制后以 false 通知脏区域。不得保留或释放借用的表面指针。</param>
    /// <param name="force">即使无新帧也复制，用于前缓冲恢复和重新绑定。</param>
    /// <returns>是否完成了一次展示复制。媒体调度不依赖此方法。</returns>
    public bool CopyLatestFrame(Action<nint, int, int, bool> present, bool force = false)
    {
        Verify();
        ArgumentNullException.ThrowIfNull(present);
        return _prototypePresenter?.Present(present, force) ?? false;
    }

    /// <summary>创建独立诊断会话，与产品入口复用同一自定义 EVR 实现。</summary>
    public static MediaPlaybackSession CreateD3DImagePrototype(nint videoWindowHandle)
    {
        var session = new MediaPlaybackSession(videoWindowHandle);
        session._prototype = true;
        return session;
    }

    /// <summary>实验入口：在所属线程的 D3DImage 锁内呈现。回调 true 为复制前，false 为复制后。</summary>
    public bool PresentPrototypeFrame(Action<nint, int, int, bool> present)
    {
        Verify();
        ArgumentNullException.ThrowIfNull(present);
        return _prototypePresenter?.Present(present) ?? false;
    }

    /// <summary>自定义 presenter 实际复制到展示 surface 的帧数，不是 UI tick 数。</summary>
    public long PrototypeFrames
    {
        get
        {
            Verify();
            return _prototypePresenter?.Frames ?? 0;
        }
    }

    private void InstallPrototypeSink(IMFPMediaItem* item)
    {
        uint count = 0;
        item->GetNumberOfStreams(&count).ThrowOnFailure();
        for (uint index = 0; index < count; index++)
        {
            BOOL selected = default;
            item->GetStreamSelection(index, &selected).ThrowOnFailure();
            if (!selected) continue;
            Guid key = PInvoke.MF_MT_MAJOR_TYPE;
            PROPVARIANT value = default;
            bool video;
            try
            {
                item->GetStreamAttribute(index, &key, &value).ThrowOnFailure();
                video = value.Anonymous.Anonymous.vt == VARENUM.VT_CLSID &&
                        *value.Anonymous.Anonymous.Anonymous.puuid == PInvoke.MFMediaType_Video;
            }
            finally
            {
                PInvoke.PropVariantClear(&value).ThrowOnFailure();
            }

            if (!video) continue;
            _prototypePresenter = new EVRPrototypePresenter(_scalingMode);
            IMFMediaSink* sink = null;
            Guid iid = IMFMediaSink.IID_Guid;
            EVRPrototypePresenter.Check(PInvoke.MFCreateVideoRenderer(&iid, (void**)&sink), "MFCreateVideoRenderer");
            _prototypeSink = sink;
            IMFVideoRenderer* renderer = null;
            nint presenter = 0;
            try
            {
                iid = IMFVideoRenderer.IID_Guid;
                EVRPrototypePresenter.Check(sink->QueryInterface(&iid, (void**)&renderer), "EVR.IMFVideoRenderer");
                presenter = Marshal.GetComInterfaceForObject(_prototypePresenter, typeof(IMFVideoPresenter.Interface));
                EVRPrototypePresenter.Check
                (
                    renderer->InitializeRenderer(null, (IMFVideoPresenter*)presenter),
                    "InitializeRenderer(custom presenter)"
                );
                IMFStreamSink* streamSink = null;
                EVRPrototypePresenter.Check(sink->GetStreamSinkByIndex(0, &streamSink), "EVR.GetStreamSinkByIndex");
                try
                {
                    var hr = item->SetStreamSink(index, (IUnknown*)streamSink);
                    Trace.WriteLine
                    (
                        $"EVR prototype SetStreamSink argument=IMFStreamSink video={index} HRESULT=0x{hr.Value:X8}; default audio unchanged",
                        "MediaFoundation.Core"
                    );
                    EVRPrototypePresenter.Check(hr, $"SetStreamSink video={index}; default audio unchanged");
                }
                finally
                {
                    streamSink->Release();
                }
            }
            finally
            {
                if (presenter != 0) Marshal.Release(presenter);
                if (renderer != null) renderer->Release();
            }

            break;
        }
    }

    private bool _disposed;
    private bool _comInitialized;
    private PlaybackState _state;
    private Uri? _source;
    private TimeSpan _position;
    private TimeSpan _duration;
    private int _videoWidth;
    private int _videoHeight;
    private bool _hasVideo;
    private bool _hasAudio;
    private double _volume = 1;
    private double _rate = 1;
    private bool _muted;
    private bool _looping;
    private bool _seekInFlight;
    private TimeSpan? _pendingSeek;
    private bool _playAfterSeek;
    private bool _stopping;
    private bool _stopConfirmed;

    /// <summary>绑定有效的视频子窗口；即使播放纯音频，也需要宿主提供有效 HWND。</summary>
    /// <param name="videoWindowHandle">由宿主持有并在会话整个生命周期保持有效的 HWND。</param>
    public MediaPlaybackSession(nint videoWindowHandle)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            throw new InvalidOperationException(SR.Current.StaRequired);
        _context = SynchronizationContext.Current ?? throw new InvalidOperationException(SR.Current.ContextRequired);
        if (_context.GetType() == typeof(SynchronizationContext))
            throw new InvalidOperationException(SR.Current.ContextRequired);
        _window = new HWND(videoWindowHandle);
        if (!PInvoke.IsWindow(_window)) throw new ArgumentException(SR.Current.InvalidWindow, nameof(videoWindowHandle));
        PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED).ThrowOnFailure();
        _comInitialized = true;
    }

    /// <summary>原生事件确认状态改变时通知；在构造同步上下文执行。</summary>
    public event EventHandler? StateChanged;

    /// <summary>媒体已设置，元数据可用；不会自动播放。</summary>
    public event EventHandler? MediaOpened;

    /// <summary>到达自然末尾时通知，包括循环播放的每次末尾。</summary>
    public event EventHandler? PlaybackEnded;

    /// <summary>异步原生操作失败时通知；同步参数或调用错误直接抛出。</summary>
    public event EventHandler<PlaybackFailedEventArgs>? Failed;

    /// <summary>当前已确认状态。</summary>
    public PlaybackState State
    {
        get
        {
            Verify();
            return _state;
        }
    }

    /// <summary>最近请求打开的绝对 URI。</summary>
    public Uri? Source
    {
        get
        {
            Verify();
            return _source;
        }
    }

    /// <summary>最近一次 Refresh 或定位确认的位置；不会在后台启动计时器。</summary>
    public TimeSpan Position
    {
        get
        {
            Verify();
            return _position;
        }
    }

    /// <summary>媒体时长；未知或直播时为零。</summary>
    public TimeSpan Duration
    {
        get
        {
            Verify();
            return _duration;
        }
    }

    /// <summary>原生视频像素宽度；纯音频为零。</summary>
    public int VideoWidth
    {
        get
        {
            Verify();
            return _videoWidth;
        }
    }

    /// <summary>原生视频像素高度；纯音频为零。</summary>
    public int VideoHeight
    {
        get
        {
            Verify();
            return _videoHeight;
        }
    }

    /// <summary>媒体是否包含已选择的视频流。</summary>
    public bool HasVideo
    {
        get
        {
            Verify();
            return _hasVideo;
        }
    }

    /// <summary>媒体是否包含已选择的音频流；由 MFPlay 原生音频输出播放。</summary>
    public bool HasAudio
    {
        get
        {
            Verify();
            return _hasAudio;
        }
    }

    /// <summary>线性音量，范围 0 到 1；重新打开后保留。</summary>
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
            if (_player != null)
            {
                _player->SetVolume((float)value);
                float actual = 0;
                _player->GetVolume(&actual);
                Trace.WriteLine($"MFPlay native volume requested={value} actual={actual}", "MediaFoundation.Core");
                if (Math.Abs(actual - value) > 0.001) throw new COMException(SR.Current.NativeFailure);
            }

            _volume = value;
        }
    }

    /// <summary>静音状态；不更改保存的音量，重新打开后保留。</summary>
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
            if (_player != null)
            {
                _player->SetMute(value);
                BOOL actual = default;
                _player->GetMute(&actual);
                Trace.WriteLine($"MFPlay native mute requested={value} actual={(bool)actual}", "MediaFoundation.Core");
                if ((bool)actual != value) throw new COMException(SR.Current.NativeFailure);
            }

            _muted = value;
        }
    }

    /// <summary>
    /// 正向播放速率，必须为有限正数。媒体未就绪时保存设置；打开时应用。
    /// 原生不支持时同步抛 NotSupportedException，异步拒绝则通过 Failed 通知；非正常速率的音频行为由原生管线决定。
    /// </summary>
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
            if (IsReady) ApplyRate(value);
            _rate = value;
        }
    }

    /// <summary>是否在自然末尾异步定位到零并继续播放。</summary>
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

    /// <summary>异步打开绝对文件或网络 URI；替换旧媒体并释放旧管线。通过 MediaOpened 或 Failed 完成通知。</summary>
    public void Open(Uri source)
    {
        Verify();
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsAbsoluteUri) throw new ArgumentException(SR.Current.AbsoluteUriRequired, nameof(source));
        Trace.WriteLine
            ($"MFPlay Open {SourceDiagnostics.Describe(source, source.IsFile ? null : source.AbsoluteUri)}", "MediaFoundation.Core");
        ReleasePlayer();
        this._source = source;
        _position = _duration = TimeSpan.Zero;
        _videoWidth = _videoHeight = 0;
        _hasAudio = _hasVideo = false;
        _seekInFlight = _playAfterSeek = _stopping = _stopConfirmed = false;
        _pendingSeek = null;
        _callback = new NativeCallback(this, _context);
        var nativeCallback = _callback.Create();
        string stage = "MFPCreateMediaPlayer";
        try
        {
            IMFPMediaPlayer* created = null;
            PInvoke.MFPCreateMediaPlayer(default(PCWSTR), false, 0, nativeCallback, _window, &created).ThrowOnFailure();
            _player = created;
            stage = "IMFPMediaPlayer.SetVolume";
            _player->SetVolume((float)_volume);
            stage = "IMFPMediaPlayer.SetMute";
            _player->SetMute(_muted);
            if (source.IsFile)
            {
                IMFByteStream* stream = null;
                try
                {
                    stage = "MFCreateFile";
                    string path = source.LocalPath;
                    fixed (char* text =
                               path)
                        PInvoke.MFCreateFile
                        (
                            MF_FILE_ACCESSMODE.MF_ACCESSMODE_READ, MF_FILE_OPENMODE.MF_OPENMODE_FAIL_IF_NOT_EXIST,
                            MF_FILE_FLAGS.MF_FILEFLAGS_NONE, new PCWSTR(text), &stream
                        ).ThrowOnFailure();
                    stage = "IMFPMediaPlayer.CreateMediaItemFromObject";
                    Trace.WriteLine($"MFPlay request stage={stage} source=IMFByteStream async=true", "MediaFoundation.Core");
                    _player->CreateMediaItemFromObject((IUnknown*)stream, false, 0, null);
                }
                finally
                {
                    if (stream != null) stream->Release();
                }
            }
            else
            {
                stage = "IMFPMediaPlayer.CreateMediaItemFromURL";
                Trace.WriteLine($"MFPlay request stage={stage} source=url async=true", "MediaFoundation.Core");
                string url = source.AbsoluteUri;
                fixed (char* text = url) _player->CreateMediaItemFromURL(new PCWSTR(text), false, 0, null);
            }
        }
        catch (COMException error)
        {
            ReportFailure(error, stage);
            return;
        }
        finally
        {
            nativeCallback->Release();
        }

        SetState(PlaybackState.Opening);
    }

    /// <summary>请求播放；必须已收到 MediaOpened。末尾播放会先异步定位到零。</summary>
    public void Play()
    {
        VerifyReady();
        if (_state == PlaybackState.Ended || _seekInFlight || _stopping)
        {
            _playAfterSeek = true;
            if (_state == PlaybackState.Ended) QueueSeek(TimeSpan.Zero);
            return;
        }

        _player->Play();
    }

    /// <summary>请求暂停；原生事件确认后更新 State。</summary>
    public void Pause()
    {
        VerifyReady();
        _playAfterSeek = false;
        _player->Pause();
    }

    /// <summary>请求停止并定位到零；State 在原生停止及定位完成后变为 Stopped。</summary>
    public void Stop()
    {
        VerifyReady();
        _playAfterSeek = false;
        if (_stopping) return;
        _stopping = true;
        _stopConfirmed = false;
        _pendingSeek = TimeSpan.Zero;
        _player->Stop();
    }

    /// <summary>异步定位；按已知时长截断，负值截断为零。连续请求合并为最后一个，不同步等待原生回调。</summary>
    public void Seek(TimeSpan position)
    {
        VerifyReady();
        if (position < TimeSpan.Zero) position = TimeSpan.Zero;
        if (_duration > TimeSpan.Zero && position > _duration) position = _duration;
        QueueSeek(position);
    }

    /// <summary>由宿主 UI timer 调用以刷新 Position；未就绪时不操作。不产生 PositionChanged，也不自行创建计时器。</summary>
    public void Refresh()
    {
        Verify();
        _prototypePresenter?.RefreshStatus(out _videoWidth, out _videoHeight);
        if (!IsReady || _seekInFlight || _stopping || _state == PlaybackState.Ended) return;
        PROPVARIANT value = default;
        Guid format = PInvoke.MFP_POSITIONTYPE_100NS;
        try
        {
            _player->GetPosition(&format, &value);
            _position = ReadTime(value);
        }
        finally
        {
            PInvoke.PropVariantClear(&value).ThrowOnFailure();
        }
    }

    /// <summary>宿主改变 HWND 尺寸或需要重绘时调用，保持原生渲染器的宽高比策略。</summary>
    public void ResizeVideo()
    {
        Verify();
        if (_player == null || !_hasVideo || _state is not (PlaybackState.Playing or PlaybackState.Paused)) return;
        var renderingPlayer = _player;
        renderingPlayer->AddRef();
        try
        {
            renderingPlayer->UpdateVideo();
        }
        finally
        {
            renderingPlayer->Release();
        }
    }

    private bool IsReady => _player != null &&
                            _state is PlaybackState.Stopped or PlaybackState.Playing or PlaybackState.Paused
                                or PlaybackState.Ended;

    private void Verify()
    {
        if (Environment.CurrentManagedThreadId != _threadId) throw new InvalidOperationException(SR.Current.WrongThread);
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void VerifyReady()
    {
        Verify();
        if (!IsReady) throw new InvalidOperationException(SR.Current.NotReady);
    }

    private void ApplyRate(double value)
    {
        try
        {
            _player->SetRate((float)value);
            float actual = 0;
            _player->GetRate(&actual);
            Trace.WriteLine
            (
                $"MFPlay native rate requested={value} current={actual} (confirmation via RATE_SET event)",
                "MediaFoundation.Core"
            );
        }
        catch (COMException error)
        {
            throw new NotSupportedException(SR.Current.UnsupportedRate, error);
        }
    }

    private void QueueSeek(TimeSpan target)
    {
        _pendingSeek = target;
        if (_seekInFlight || _stopping) return;
        IssueSeek();
    }

    private void IssueSeek()
    {
        if (_pendingSeek is not { } target) return;
        PROPVARIANT value = default;
        value.Anonymous.Anonymous.vt = VARENUM.VT_I8;
        value.Anonymous.Anonymous.Anonymous.hVal = target.Ticks;
        Guid format = PInvoke.MFP_POSITIONTYPE_100NS;
        _player->SetPosition(&format, &value);
        _pendingSeek = null;
        _seekInFlight = true;
        _position = target;
    }

    internal void HandleNotification(NativeCallback sender, NativeCallback.Notification notification)
    {
        Verify();
        if (!ReferenceEquals(_callback, sender)) return;
        Trace.WriteLine
        (
            $"MFPlay event={notification.Type} hr={notification.Result} state={_state} nativeState={notification.NativeState} callbackThread={notification.CallbackThreadId} hasMediaItem={notification.Item != 0}",
            "MediaFoundation.Core"
        );
        if (notification.Result.Failed)
        {
            Exception error = Marshal.GetExceptionForHR
                (notification.Result.Value, new IntPtr(-1)) ?? new COMException(SR.Current.NativeFailure, notification.Result.Value);
            // The asynchronous callback supplies an HRESULT, not an exception with a stack.
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.SetCurrentStackTrace(error);
            if (notification.Type == MFP_EVENT_TYPE.MFP_EVENT_TYPE_RATE_SET)
                error = new NotSupportedException(SR.Current.UnsupportedRate, error);
            ReportFailure(error, $"callback:{notification.Type}");
            return;
        }

        try
        {
            ProcessNotification(sender, notification);
        }
        catch (COMException error)
        {
            ReportFailure(error, $"processing:{notification.Type}");
        }
        catch (NotSupportedException error)
        {
            ReportFailure(error, $"processing:{notification.Type}");
        }
    }

    private void ProcessNotification(NativeCallback sender, NativeCallback.Notification notification)
    {
        switch (notification.Type)
        {
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_MEDIAITEM_CREATED:
                if (notification.Item == 0) throw new COMException(SR.Current.NativeFailure);
                if (_prototype) InstallPrototypeSink((IMFPMediaItem*)notification.Item);
                _player->SetMediaItem((IMFPMediaItem*)notification.Item);
                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_MEDIAITEM_SET:
                ReadMetadata();
                // The video renderer exists only after MEDIAITEM_SET.
                if (_hasVideo && !_prototype) _player->SetAspectRatioMode(0x00000001); // MFP_AR_MODE_PRESERVE_PICTURE
                _player->SetVolume((float)_volume);
                _player->SetMute(_muted);
                if (_rate != 1) ApplyRate(_rate);
                SetState(PlaybackState.Stopped);
                if (!_disposed && ReferenceEquals(_callback, sender)) MediaOpened?.Invoke(this, EventArgs.Empty);
                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_PLAY:
                if (!_stopping) SetState(PlaybackState.Playing);
                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_PAUSE:
                if (!_stopping) SetState(PlaybackState.Paused);
                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_STOP:
                _stopConfirmed = true;
                if (_stopping && !_seekInFlight) IssueSeek();
                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_POSITION_SET:
                _seekInFlight = false;
                if (_stopping && !_stopConfirmed) break;
                if (_pendingSeek.HasValue)
                {
                    IssueSeek();
                    break;
                }

                if (_stopping)
                {
                    _stopping = false;
                    SetState(PlaybackState.Stopped);
                    if (_disposed || !ReferenceEquals(_callback, sender)) return;
                }

                if (_playAfterSeek)
                {
                    _playAfterSeek = false;
                    _player->Play();
                }
                else if (_state == PlaybackState.Ended) SetState(PlaybackState.Paused);

                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_PLAYBACK_ENDED:
                if (_stopping) break;
                _position = _duration;
                SetState(PlaybackState.Ended);
                if (_disposed || !ReferenceEquals(_callback, sender)) return;
                PlaybackEnded?.Invoke(this, EventArgs.Empty);
                if (!_disposed && ReferenceEquals(_callback, sender) && _looping && _state == PlaybackState.Ended)
                {
                    _playAfterSeek = true;
                    QueueSeek(TimeSpan.Zero);
                }

                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_ERROR:
                throw new COMException(SR.Current.NativeFailure, notification.Result.Value);
        }
    }

    private void ReadMetadata()
    {
        IMFPMediaItem* item = null;
        _player->GetMediaItem(&item);
        try
        {
            BOOL exists = default, selected = default;
            item->HasAudio(&exists, &selected);
            _hasAudio = exists && selected;
            Trace.WriteLine($"MFPlay audio exists={exists} selected={selected}", "MediaFoundation.Core");
            item->HasVideo(&exists, &selected);
            _hasVideo = exists && selected;
            Trace.WriteLine($"MFPlay video exists={exists} selected={selected}", "MediaFoundation.Core");
            try
            {
                uint count = 0;
                item->GetNumberOfStreams(&count);
                for (uint index = 0; index < Math.Min(count, 256u); index++)
                {
                    BOOL streamSelected = default;
                    item->GetStreamSelection(index, &streamSelected);
                    Trace.WriteLine
                    (
                        $"MFPlay track={index} selected={streamSelected} (media item selection, not resolver default)",
                        "MediaFoundation.Core"
                    );
                }
            }
            catch (COMException error)
            {
                Trace.WriteLine
                    ($"MFPlay track selection diagnostic HRESULT=0x{error.HResult:X8}", "MediaFoundation.Core");
            }

            if (_hasVideo && _prototypePresenter != null)
                _prototypePresenter.RefreshStatus(out _videoWidth, out _videoHeight);
            if (_hasVideo && !_prototype)
            {
                SIZE size = default;
                _player->GetNativeVideoSize(&size, null);
                _videoWidth = size.cx;
                _videoHeight = size.cy;
            }

            PROPVARIANT value = default;
            Guid format = PInvoke.MFP_POSITIONTYPE_100NS;
            try
            {
                item->GetDuration(&format, &value);
                _duration = ReadTime(value);
            }
            catch (COMException error)
            {
                // Live sources may not expose a duration; retain diagnostics rather than inventing one.
                Trace.WriteLine(error, "MediaFoundation.Core.Duration");
                _duration = TimeSpan.Zero;
            }
            finally
            {
                PInvoke.PropVariantClear(&value).ThrowOnFailure();
            }
        }
        finally
        {
            if (item != null) item->Release();
        }
    }

    private static TimeSpan ReadTime(PROPVARIANT value)
    {
        var data = value.Anonymous.Anonymous;
        return data.vt switch
        {
            VARENUM.VT_I8 => TimeSpan.FromTicks(Math.Max(0, data.Anonymous.hVal)),
            VARENUM.VT_UI8 => TimeSpan.FromTicks((long)Math.Min((ulong)long.MaxValue, data.Anonymous.uhVal)),
            _ => TimeSpan.Zero
        };
    }

    private void ReportFailure(Exception error, string stage)
    {
        Trace.TraceError("MFPlay failure: stage={0} state={1} prototype={2} HRESULT=0x{3:X8} {4}",
            stage, _state, _prototype, error.HResult, error);
        ReleasePlayer();
        var current = _callback;
        SetState(PlaybackState.Failed);
        if (!_disposed && ReferenceEquals(_callback, current)) Failed?.Invoke(this, new PlaybackFailedEventArgs(error));
    }

    private void SetState(PlaybackState value)
    {
        if (_state == value) return;
        _state = value;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ReleasePlayer()
    {
        if (_prototypePresenter != null)
        {
            SurfaceReleasing?.Invoke(this, EventArgs.Empty);
            PrototypeSurfaceReleasing?.Invoke(this, EventArgs.Empty);
        }

        _callback?.Close();
        _callback = null;
        var old = _player;
        _player = null;
        if (old == null) return;
        try
        {
            old->Shutdown();
        }
        finally
        {
            old->Release();
            if (_prototypeSink != null)
            {
                var hr = _prototypeSink->Shutdown();
                Trace.WriteLine($"EVR prototype sink shutdown HRESULT=0x{hr.Value:X8}", "MediaFoundation.Core");
                _prototypeSink->Release();
                _prototypeSink = null;
            }

            _prototypePresenter?.Dispose();
            _prototypePresenter = null;
        }
    }

    /// <summary>在创建线程释放原生管线及 COM 引用；已排队的回调被丢弃。可重复调用。</summary>
    public void Dispose()
    {
        if (Environment.CurrentManagedThreadId != _threadId) throw new InvalidOperationException(SR.Current.WrongThread);
        if (_disposed) return;
        _disposed = true;
        try
        {
            ReleasePlayer();
        }
        finally
        {
            StateChanged = MediaOpened = PlaybackEnded = null;
            Failed = null;
            if (_comInitialized)
            {
                PInvoke.CoUninitialize();
                _comInitialized = false;
            }
        }
    }
}