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
public sealed unsafe class MediaPlaybackSession : IDisposable
{
    private readonly int threadId = Environment.CurrentManagedThreadId;
    private readonly SynchronizationContext context;
    private readonly HWND window;
    private IMFPMediaPlayer* player;
    private NativeCallback? callback;
    private bool disposed;
    private bool comInitialized;
    private PlaybackState state;
    private Uri? source;
    private TimeSpan position;
    private TimeSpan duration;
    private int videoWidth;
    private int videoHeight;
    private bool hasVideo;
    private bool hasAudio;
    private double volume = 1;
    private double rate = 1;
    private bool muted;
    private bool looping;
    private bool seekInFlight;
    private TimeSpan? pendingSeek;
    private bool playAfterSeek;
    private bool stopping;
    private bool stopConfirmed;

    /// <summary>绑定有效的视频子窗口；即使播放纯音频，也需要宿主提供有效 HWND。</summary>
    /// <param name="videoWindowHandle">由宿主持有并在会话整个生命周期保持有效的 HWND。</param>
    public MediaPlaybackSession(nint videoWindowHandle)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            throw new InvalidOperationException(SR.Get("StaRequired"));
        context = SynchronizationContext.Current ?? throw new InvalidOperationException(SR.Get("ContextRequired"));
        if (context.GetType() == typeof(SynchronizationContext))
            throw new InvalidOperationException(SR.Get("ContextRequired"));
        window = new HWND(videoWindowHandle);
        if (!PInvoke.IsWindow(window)) throw new ArgumentException(SR.Get("InvalidWindow"), nameof(videoWindowHandle));
        PInvoke.CoInitializeEx(null, COINIT.COINIT_APARTMENTTHREADED).ThrowOnFailure();
        comInitialized = true;
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
    public PlaybackState State { get { Verify(); return state; } }
    /// <summary>最近请求打开的绝对 URI。</summary>
    public Uri? Source { get { Verify(); return source; } }
    /// <summary>最近一次 Refresh 或定位确认的位置；不会在后台启动计时器。</summary>
    public TimeSpan Position { get { Verify(); return position; } }
    /// <summary>媒体时长；未知或直播时为零。</summary>
    public TimeSpan Duration { get { Verify(); return duration; } }
    /// <summary>原生视频像素宽度；纯音频为零。</summary>
    public int VideoWidth { get { Verify(); return videoWidth; } }
    /// <summary>原生视频像素高度；纯音频为零。</summary>
    public int VideoHeight { get { Verify(); return videoHeight; } }
    /// <summary>媒体是否包含已选择的视频流。</summary>
    public bool HasVideo { get { Verify(); return hasVideo; } }
    /// <summary>媒体是否包含已选择的音频流；由 MFPlay 原生音频输出播放。</summary>
    public bool HasAudio { get { Verify(); return hasAudio; } }

    /// <summary>线性音量，范围 0 到 1；重新打开后保留。</summary>
    public double Volume
    {
        get { Verify(); return volume; }
        set
        {
            Verify();
            if (!double.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value));
            if (player != null)
            {
                player->SetVolume((float)value);
                float actual = 0;
                player->GetVolume(&actual);
                Trace.WriteLine($"MFPlay native volume requested={value} actual={actual}", "MediaFoundation.Core");
                if (Math.Abs(actual - value) > 0.001) throw new COMException(SR.Get("NativeFailure"));
            }
            volume = value;
        }
    }

    /// <summary>静音状态；不更改保存的音量，重新打开后保留。</summary>
    public bool IsMuted
    {
        get { Verify(); return muted; }
        set
        {
            Verify();
            if (player != null)
            {
                player->SetMute(value);
                BOOL actual = default;
                player->GetMute(&actual);
                Trace.WriteLine($"MFPlay native mute requested={value} actual={(bool)actual}", "MediaFoundation.Core");
                if ((bool)actual != value) throw new COMException(SR.Get("NativeFailure"));
            }
            muted = value;
        }
    }

    /// <summary>
    /// 正向播放速率，必须为有限正数。媒体未就绪时保存设置；打开时应用。
    /// 原生不支持时同步抛 NotSupportedException，异步拒绝则通过 Failed 通知；非正常速率的音频行为由原生管线决定。
    /// </summary>
    public double PlaybackRate
    {
        get { Verify(); return rate; }
        set
        {
            Verify();
            if (!double.IsFinite(value) || value <= 0 || value > float.MaxValue || (float)value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (IsReady) ApplyRate(value);
            rate = value;
        }
    }

    /// <summary>是否在自然末尾异步定位到零并继续播放。</summary>
    public bool IsLooping { get { Verify(); return looping; } set { Verify(); looping = value; } }

    /// <summary>异步打开绝对文件或网络 URI；替换旧媒体并释放旧管线。通过 MediaOpened 或 Failed 完成通知。</summary>
    public void Open(Uri source)
    {
        Verify();
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsAbsoluteUri) throw new ArgumentException(SR.Get("AbsoluteUriRequired"), nameof(source));
        ReleasePlayer();
        this.source = source;
        position = duration = TimeSpan.Zero;
        videoWidth = videoHeight = 0;
        hasAudio = hasVideo = false;
        seekInFlight = playAfterSeek = stopping = stopConfirmed = false;
        pendingSeek = null;
        callback = new NativeCallback(this, context);
        var nativeCallback = callback.Create();
        try
        {
            IMFPMediaPlayer* created = null;
            PInvoke.MFPCreateMediaPlayer(default(PCWSTR), false, 0, nativeCallback, window, &created).ThrowOnFailure();
            player = created;
            player->SetVolume((float)volume);
            player->SetMute(muted);
            string url = source.IsFile ? source.LocalPath : source.AbsoluteUri;
            fixed (char* text = url) player->CreateMediaItemFromURL(new PCWSTR(text), false, 0, null);
        }
        catch (COMException error)
        {
            ReleasePlayer();
            ReportFailure(error);
            return;
        }
        finally { nativeCallback->Release(); }
        SetState(PlaybackState.Opening);
    }

    /// <summary>请求播放；必须已收到 MediaOpened。末尾播放会先异步定位到零。</summary>
    public void Play()
    {
        VerifyReady();
        if (state == PlaybackState.Ended || seekInFlight || stopping)
        {
            playAfterSeek = true;
            if (state == PlaybackState.Ended) QueueSeek(TimeSpan.Zero);
            return;
        }
        player->Play();
    }

    /// <summary>请求暂停；原生事件确认后更新 State。</summary>
    public void Pause()
    {
        VerifyReady();
        playAfterSeek = false;
        player->Pause();
    }

    /// <summary>请求停止并定位到零；State 在原生停止及定位完成后变为 Stopped。</summary>
    public void Stop()
    {
        VerifyReady();
        playAfterSeek = false;
        if (stopping) return;
        stopping = true;
        stopConfirmed = false;
        pendingSeek = TimeSpan.Zero;
        player->Stop();
    }

    /// <summary>异步定位；按已知时长截断，负值截断为零。连续请求合并为最后一个，不同步等待原生回调。</summary>
    public void Seek(TimeSpan position)
    {
        VerifyReady();
        if (position < TimeSpan.Zero) position = TimeSpan.Zero;
        if (duration > TimeSpan.Zero && position > duration) position = duration;
        QueueSeek(position);
    }

    /// <summary>由宿主 UI timer 调用以刷新 Position；未就绪时不操作。不产生 PositionChanged，也不自行创建计时器。</summary>
    public void Refresh()
    {
        Verify();
        if (!IsReady || seekInFlight || stopping || state == PlaybackState.Ended) return;
        PROPVARIANT value = default;
        Guid format = PInvoke.MFP_POSITIONTYPE_100NS;
        try
        {
            player->GetPosition(&format, &value);
            position = ReadTime(value);
        }
        finally { PInvoke.PropVariantClear(&value).ThrowOnFailure(); }
    }

    /// <summary>宿主改变 HWND 尺寸或需要重绘时调用，保持原生渲染器的宽高比策略。</summary>
    public void ResizeVideo()
    {
        Verify();
        if (player == null || !hasVideo || state is not (PlaybackState.Playing or PlaybackState.Paused)) return;
        var renderingPlayer = player;
        renderingPlayer->AddRef();
        try { renderingPlayer->UpdateVideo(); }
        finally { renderingPlayer->Release(); }
    }

    private bool IsReady => player != null && state is PlaybackState.Stopped or PlaybackState.Playing or PlaybackState.Paused or PlaybackState.Ended;

    private void Verify()
    {
        if (Environment.CurrentManagedThreadId != threadId) throw new InvalidOperationException(SR.Get("WrongThread"));
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private void VerifyReady()
    {
        Verify();
        if (!IsReady) throw new InvalidOperationException(SR.Get("NotReady"));
    }

    private void ApplyRate(double value)
    {
        try
        {
            player->SetRate((float)value);
            float actual = 0;
            player->GetRate(&actual);
            Trace.WriteLine($"MFPlay native rate requested={value} current={actual} (confirmation via RATE_SET event)", "MediaFoundation.Core");
        }
        catch (COMException error) { throw new NotSupportedException(SR.Get("UnsupportedRate"), error); }
    }

    private void QueueSeek(TimeSpan target)
    {
        pendingSeek = target;
        if (seekInFlight || stopping) return;
        IssueSeek();
    }

    private void IssueSeek()
    {
        if (pendingSeek is not { } target) return;
        PROPVARIANT value = default;
        value.Anonymous.Anonymous.vt = VARENUM.VT_I8;
        value.Anonymous.Anonymous.Anonymous.hVal = target.Ticks;
        Guid format = PInvoke.MFP_POSITIONTYPE_100NS;
        player->SetPosition(&format, &value);
        pendingSeek = null;
        seekInFlight = true;
        position = target;
    }

    internal void HandleNotification(NativeCallback sender, NativeCallback.Notification notification)
    {
        Verify();
        if (!ReferenceEquals(callback, sender)) return;
        Trace.WriteLine($"MFPlay event={notification.Type} hr={notification.Result} state={state}", "MediaFoundation.Core");
        if (notification.Result.Failed)
        {
            Exception error = Marshal.GetExceptionForHR(notification.Result.Value) ?? new COMException(SR.Get("NativeFailure"), notification.Result.Value);
            if (notification.Type == MFP_EVENT_TYPE.MFP_EVENT_TYPE_RATE_SET)
                error = new NotSupportedException(SR.Get("UnsupportedRate"), error);
            ReportFailure(error);
            return;
        }
        try { ProcessNotification(sender, notification); }
        catch (COMException error) { ReportFailure(error); }
        catch (NotSupportedException error) { ReportFailure(error); }
    }

    private void ProcessNotification(NativeCallback sender, NativeCallback.Notification notification)
    {
        switch (notification.Type)
        {
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_MEDIAITEM_CREATED:
                if (notification.Item == 0) throw new COMException(SR.Get("NativeFailure"));
                player->SetMediaItem((IMFPMediaItem*)notification.Item);
                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_MEDIAITEM_SET:
                ReadMetadata();
                // The video renderer exists only after MEDIAITEM_SET.
                if (hasVideo) player->SetAspectRatioMode(0x00000001); // MFP_AR_MODE_PRESERVE_PICTURE
                player->SetVolume((float)volume);
                player->SetMute(muted);
                if (rate != 1) ApplyRate(rate);
                SetState(PlaybackState.Stopped);
                if (!disposed && ReferenceEquals(callback, sender)) MediaOpened?.Invoke(this, EventArgs.Empty);
                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_PLAY:
                if (!stopping) SetState(PlaybackState.Playing);
                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_PAUSE:
                if (!stopping) SetState(PlaybackState.Paused);
                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_STOP:
                stopConfirmed = true;
                if (stopping && !seekInFlight) IssueSeek();
                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_POSITION_SET:
                seekInFlight = false;
                if (stopping && !stopConfirmed) break;
                if (pendingSeek.HasValue) { IssueSeek(); break; }
                if (stopping)
                {
                    stopping = false;
                    SetState(PlaybackState.Stopped);
                    if (disposed || !ReferenceEquals(callback, sender)) return;
                }
                if (playAfterSeek) { playAfterSeek = false; player->Play(); }
                else if (state == PlaybackState.Ended) SetState(PlaybackState.Paused);
                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_PLAYBACK_ENDED:
                if (stopping) break;
                position = duration;
                SetState(PlaybackState.Ended);
                if (disposed || !ReferenceEquals(callback, sender)) return;
                PlaybackEnded?.Invoke(this, EventArgs.Empty);
                if (!disposed && ReferenceEquals(callback, sender) && looping && state == PlaybackState.Ended)
                {
                    playAfterSeek = true;
                    QueueSeek(TimeSpan.Zero);
                }
                break;
            case MFP_EVENT_TYPE.MFP_EVENT_TYPE_ERROR:
                ReportFailure(new COMException(SR.Get("NativeFailure"), notification.Result.Value));
                break;
        }
    }

    private void ReadMetadata()
    {
        IMFPMediaItem* item = null;
        player->GetMediaItem(&item);
        try
        {
            BOOL exists = default, selected = default;
            item->HasAudio(&exists, &selected);
            hasAudio = exists && selected;
            item->HasVideo(&exists, &selected);
            hasVideo = exists && selected;
            if (hasVideo)
            {
                SIZE size = default;
                player->GetNativeVideoSize(&size, null);
                videoWidth = size.cx;
                videoHeight = size.cy;
            }
            PROPVARIANT value = default;
            Guid format = PInvoke.MFP_POSITIONTYPE_100NS;
            try
            {
                item->GetDuration(&format, &value);
                duration = ReadTime(value);
            }
            catch (COMException error)
            {
                // Live sources may not expose a duration; retain diagnostics rather than inventing one.
                Trace.WriteLine(error, "MediaFoundation.Core.Duration");
                duration = TimeSpan.Zero;
            }
            finally { PInvoke.PropVariantClear(&value).ThrowOnFailure(); }
        }
        finally { if (item != null) item->Release(); }
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

    private void ReportFailure(Exception error)
    {
        Trace.TraceError("MFPlay failure: {0}", error);
        ReleasePlayer();
        var current = callback;
        SetState(PlaybackState.Failed);
        if (!disposed && ReferenceEquals(callback, current)) Failed?.Invoke(this, new PlaybackFailedEventArgs(error));
    }

    private void SetState(PlaybackState value)
    {
        if (state == value) return;
        state = value;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ReleasePlayer()
    {
        callback?.Close();
        callback = null;
        var old = player;
        player = null;
        if (old == null) return;
        try { old->Shutdown(); }
        finally { old->Release(); }
    }

    /// <summary>在创建线程释放原生管线及 COM 引用；已排队的回调被丢弃。可重复调用。</summary>
    public void Dispose()
    {
        if (Environment.CurrentManagedThreadId != threadId) throw new InvalidOperationException(SR.Get("WrongThread"));
        if (disposed) return;
        disposed = true;
        try { ReleasePlayer(); }
        finally
        {
            StateChanged = MediaOpened = PlaybackEnded = null;
            Failed = null;
            if (comInitialized) { PInvoke.CoUninitialize(); comInitialized = false; }
        }
    }
}
