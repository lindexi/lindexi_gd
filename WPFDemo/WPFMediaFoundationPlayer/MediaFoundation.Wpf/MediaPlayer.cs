using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using MediaFoundation.Core;

namespace MediaFoundation.Wpf;

/// <summary>Dispatcher 线程拥有的播放器。先将 VideoView 加入可见视图，再异步打开媒体；必须在创建线程释放。</summary>
public sealed class MediaPlayer : INotifyPropertyChanged, IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly VideoHost host;
    private readonly DispatcherTimer timer;
    private MediaPlaybackSession? session;
    private bool disposed;
    private bool playPending;
    private double volume = 1, rate = 1;
    private bool muted, looping;

    /// <summary>在具有消息循环的 WPF STA Dispatcher 上创建播放器。</summary>
    public MediaPlayer()
    {
        dispatcher = Dispatcher.CurrentDispatcher;
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            throw new InvalidOperationException(Text.Get("StaRequired"));
        host = new VideoHost();
        host.Created += OnCreated;
        host.Destroying += ReleaseSession;
        host.Resized += OnResize;
        timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, OnTick, dispatcher);
        timer.Stop();
    }

    /// <summary>可放入 ContentControl 的唯一视频宿主，不支持同时放入多个可视树。</summary>
    public FrameworkElement VideoView => host;
    /// <summary>属性变化通知；始终在创建线程发生。</summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>异步打开完成，此后可以播放。</summary>
    public event EventHandler? MediaOpened;
    /// <summary>自然播放结束，包括循环播放的每次末尾。</summary>
    public event EventHandler? PlaybackEnded;
    /// <summary>异步失败或刷新失败；同步调用错误直接抛出。</summary>
    public event EventHandler<PlaybackFailedEventArgs>? Failed;
    /// <summary>最近一次错误；下一次 Open 或 Close 时清除。</summary>
    public Exception? LastError { get; private set; }
    /// <summary>最近请求的绝对媒体地址。</summary>
    public Uri? Source { get; private set; }
    /// <summary>最近确认的播放状态。</summary>
    public PlaybackState State { get; private set; }
    /// <summary>轮询更新的媒体位置。</summary>
    public TimeSpan Position { get; private set; }
    /// <summary>媒体时长；未知为零。</summary>
    public TimeSpan Duration { get; private set; }
    /// <summary>视频原始宽度。</summary>
    public int VideoWidth { get; private set; }
    /// <summary>视频原始高度。</summary>
    public int VideoHeight { get; private set; }
    /// <summary>是否包含视频。</summary>
    public bool HasVideo { get; private set; }
    /// <summary>是否包含音频。</summary>
    public bool HasAudio { get; private set; }
    /// <summary>是否有已打开的媒体。</summary>
    public bool CanPlay => State is PlaybackState.Stopped or PlaybackState.Paused or PlaybackState.Ended;
    /// <summary>是否正在播放，可以暂停。</summary>
    public bool CanPause => State == PlaybackState.Playing;
    /// <summary>是否有已知时长且媒体就绪；个别源仍可能拒绝原生定位请求。</summary>
    public bool CanSeek => Duration > TimeSpan.Zero && IsReady;
    internal bool IsReady => State is PlaybackState.Playing or PlaybackState.Paused or PlaybackState.Stopped or PlaybackState.Ended;
    /// <summary>线性音量 0 到 1；打开前设置及 Close 后保留。</summary>
    public double Volume
    {
        get => volume;
        set { Verify(); if (!double.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); if (session != null) session.Volume = value; volume = value; Notify(); }
    }
    /// <summary>静音设置；跨媒体保留。</summary>
    public bool IsMuted
    {
        get => muted;
        set { Verify(); if (session != null) session.IsMuted = value; muted = value; Notify(); }
    }
    /// <summary>播放速率；默认 1，具体媒体可能拒绝不支持的速率。</summary>
    public double PlaybackRate
    {
        get => rate;
        set { Verify(); if (!double.IsFinite(value) || value <= 0 || value > float.MaxValue || (float)value == 0) throw new ArgumentOutOfRangeException(nameof(value)); if (session != null) session.PlaybackRate = value; rate = value; Notify(); }
    }
    /// <summary>是否循环；跨媒体保留。</summary>
    public bool IsLooping
    {
        get => looping;
        set { Verify(); if (session != null) session.IsLooping = value; looping = value; Notify(); }
    }

    /// <summary>异步打开绝对 URI；宿主尚未创建时延迟到其 HWND 创建。替换旧会话并取消旧的待播放请求。</summary>
    public void Open(Uri source)
    {
        Verify(); ArgumentNullException.ThrowIfNull(source);
        if (!source.IsAbsoluteUri) throw new ArgumentException(Text.Get("AbsoluteUriRequired"), nameof(source));
        ReleaseSession(); Source = source; LastError = null; State = PlaybackState.Opening; Publish();
        if (host.Handle != 0) StartSession(host.Handle);
    }
    /// <summary>播放；如果还在异步打开，则在 MediaOpened 后执行。</summary>
    public void Play() { Verify(); if (State == PlaybackState.Opening) { playPending = true; return; } if (CanPlay) session?.Play(); }
    /// <summary>暂停；也取消尚未完成打开的待播放请求。</summary>
    public void Pause() { Verify(); playPending = false; if (CanPause) session?.Pause(); }
    /// <summary>停止并回到起点；取消待播放请求。</summary>
    public void Stop() { Verify(); playPending = false; if (IsReady) session?.Stop(); }
    /// <summary>定位；仅当 CanSeek 为 true 时调用，核心按时长截断。</summary>
    public void Seek(TimeSpan position) { Verify(); if (CanSeek) session?.Seek(position); }
    /// <summary>释放当前会话，保留视频宿主和设置，允许再次 Open。</summary>
    public void Close() { Verify(); ReleaseSession(); Source = null; LastError = null; Publish(); }
    /// <summary>永久释放会话和 HWND；重复调用安全，必须在创建线程执行。</summary>
    public void Dispose()
    {
        dispatcher.VerifyAccess(); if (disposed) return;
        ReleaseSession(); disposed = true; host.Dispose(); Source = null; Publish();
    }

    private void Verify() { dispatcher.VerifyAccess(); ObjectDisposedException.ThrowIf(disposed, this); }
    private void OnCreated(nint hwnd) { if (!disposed && Source != null) StartSession(hwnd); }
    private void StartSession(nint hwnd)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
        try
        {
            var created = new MediaPlaybackSession(hwnd);
            session = created;
            created.StateChanged += OnStateChanged; created.MediaOpened += OnOpened;
            created.PlaybackEnded += OnEnded; created.Failed += OnFailed;
            created.Volume = volume; created.IsMuted = muted; created.PlaybackRate = rate; created.IsLooping = looping;
            created.Open(Source!);
            if (created.State != PlaybackState.Failed) timer.Start();
            Snapshot();
        }
        catch { ReleaseSession(); throw; }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }
    private void ReleaseSession()
    {
        timer.Stop(); playPending = false;
        if (session is { } old)
        {
            session = null;
            old.StateChanged -= OnStateChanged; old.MediaOpened -= OnOpened;
            old.PlaybackEnded -= OnEnded; old.Failed -= OnFailed; old.Dispose();
        }
        Position = Duration = TimeSpan.Zero; VideoWidth = VideoHeight = 0; HasVideo = HasAudio = false; State = PlaybackState.Closed;
        Publish();
    }
    private void OnStateChanged(object? sender, EventArgs e) { if (ReferenceEquals(sender, session)) Snapshot(); }
    private void OnOpened(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, session)) return;
        Snapshot();
        var opened = session;
        MediaOpened?.Invoke(this, e);
        if (!ReferenceEquals(opened, session)) return;
        var shouldPlay = playPending; playPending = false;
        if (shouldPlay && CanPlay)
        {
            try { Play(); }
            catch (ExternalException error) { ReportError(error); }
            catch (InvalidOperationException error) { ReportError(error); }
        }
    }
    private void OnEnded(object? sender, EventArgs e) { if (!ReferenceEquals(sender, session)) return; Snapshot(); PlaybackEnded?.Invoke(this, e); }
    private void OnFailed(object? sender, PlaybackFailedEventArgs e) { if (!ReferenceEquals(sender, session)) return; ReportError(e.Error); }
    internal void ReportError(Exception error)
    {
        timer.Stop(); playPending = false; LastError = error; State = PlaybackState.Failed; Publish();
        Failed?.Invoke(this, new PlaybackFailedEventArgs(error));
    }
    private void OnResize()
    {
        if (disposed) return;
        try { session?.ResizeVideo(); }
        catch (ExternalException error) { ReportError(error); }
    }
    private void OnTick(object? sender, EventArgs e)
    {
        try { session?.Refresh(); Snapshot(); }
        catch (ExternalException error) { ReportError(error); }
        catch (InvalidOperationException error) { ReportError(error); }
    }
    private void Snapshot()
    {
        if (session is not { } current) return;
        State = current.State; Position = current.Position; Duration = current.Duration;
        VideoWidth = current.VideoWidth; VideoHeight = current.VideoHeight; HasVideo = current.HasVideo; HasAudio = current.HasAudio;
        Publish();
    }
    private void Publish() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
