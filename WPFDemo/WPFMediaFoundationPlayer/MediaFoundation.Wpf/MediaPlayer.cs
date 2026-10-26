using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using MediaFoundation.Core;

namespace MediaFoundation.Controls;

/// <summary>Dispatcher 线程拥有的播放器。先将 VideoView 加入可见视图，再异步打开媒体；必须在创建线程释放。</summary>
public sealed class MediaPlayer : INotifyPropertyChanged, IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly Grid _view = new();
    private readonly DispatcherTimer _timer;
    private VideoHost? _host;
    private MFPlayD3DImagePresenter? _presenter;
    private IPlaybackSession? _session;
    private VideoRenderingMode _renderingMode = VideoRenderingMode.D3DImage;
    private VideoScalingMode _scalingMode = VideoScalingMode.MixerOutput;

    /// <summary>D3DImage 缩放位置，默认 MixerOutput；Open 后锁定，Close 后可更改，不影响 HWND。</summary>
    public VideoScalingMode ScalingMode
    {
        get => _scalingMode;
        set
        {
            Verify();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == _scalingMode) return;
            if (_modeLocked) throw new InvalidOperationException(Text.Get("RenderingModeLocked"));
            _scalingMode = value;
            Notify();
        }
    }

    /// <summary>D3DImage 的实际 GPU 尺寸和策略，HWND 为 null。</summary>
    public VideoSurfaceInfo? VideoSurfaceInfo => (_session as MediaPlaybackSession)?.VideoSurfaceInfo;

    private bool _modeLocked;
    private bool _disposed;
    private bool _playPending;
    private double _volume = 1;
    private double _rate = 1;
    private bool _muted;
    private bool _looping;

    /// <summary>在具有消息循环的 WPF STA Dispatcher 上创建播放器。</summary>
    public MediaPlayer()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            throw new InvalidOperationException(Text.Get("StaRequired"));
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, OnTick, _dispatcher);
        _timer.Stop();
    }

    /// <summary>可放入 ContentControl 的唯一视频宿主，不支持同时放入多个可视树。</summary>
    public FrameworkElement VideoView => _view;

    /// <summary>呈现方式，默认 MFPlay/EVR D3DImage（D3D9Ex 表面）。Open（含延迟打开）立即锁定，Close 后可更改；VideoView 对象不变。</summary>
    public VideoRenderingMode RenderingMode
    {
        get => _renderingMode;
        set
        {
            Verify();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == _renderingMode) return;
            if (_modeLocked) throw new InvalidOperationException(Text.Get("RenderingModeLocked"));
            DestroyHost();
            _renderingMode = value;
            if (value == VideoRenderingMode.Hwnd) CreateHost();
            Notify();
        }
    }

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
    /// <summary>是否有视频轨道。</summary>
    public bool HasVideo { get; private set; }
    /// <summary>是否有音频轨道。</summary>
    public bool HasAudio { get; private set; }
    /// <summary>是否已完成打开且未关闭/失败。</summary>
    public bool IsReady => State is PlaybackState.Stopped or PlaybackState.Playing or PlaybackState.Paused or PlaybackState.Ended;
    /// <summary>当前可请求播放。</summary>
    public bool CanPlay => State is PlaybackState.Stopped or PlaybackState.Paused or PlaybackState.Ended;
    /// <summary>当前可请求暂停。</summary>
    public bool CanPause => State == PlaybackState.Playing;
    /// <summary>当前可请求定位；底层仍可能因媒体能力拒绝。</summary>
    public bool CanSeek => IsReady && Duration > TimeSpan.Zero;

    /// <summary>音量 0..1；跨媒体保留。</summary>
    public double Volume
    {
        get => _volume;
        set
        {
            Verify();
            if (!double.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value));
            if (_session != null) _session.Volume = value;
            _volume = value;
            Notify();
        }
    }

    /// <summary>静音设置；跨媒体保留。</summary>
    public bool IsMuted
    {
        get => _muted;
        set
        {
            Verify();
            if (_session != null) _session.IsMuted = value;
            _muted = value;
            Notify();
        }
    }

    /// <summary>播放速率；默认 1，具体媒体可能拒绝不支持的速率。</summary>
    public double PlaybackRate
    {
        get => _rate;
        set
        {
            Verify();
            if (!double.IsFinite(value) || value <= 0 || value > float.MaxValue || (float)value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (_session != null) _session.PlaybackRate = value;
            _rate = value;
            Notify();
        }
    }

    /// <summary>是否循环；跨媒体保留。</summary>
    public bool IsLooping
    {
        get => _looping;
        set
        {
            Verify();
            if (_session != null) _session.IsLooping = value;
            _looping = value;
            Notify();
        }
    }

    /// <summary>异步打开绝对 URI；宿主尚未创建时延迟到其 HWND 创建。替换旧会话并取消旧的待播放请求。</summary>
    public void Open(Uri source)
    {
        Verify();
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsAbsoluteUri) throw new ArgumentException(Text.Get("AbsoluteUriRequired"), nameof(source));
        System.Diagnostics.Trace.WriteLine
            ($"Open {SourceDiagnostics.Describe(source)} mode={_renderingMode}", "MediaFoundation.Controls");
        _modeLocked = true;
        ReleaseSession();
        Source = source;
        LastError = null;
        State = PlaybackState.Opening;
        Publish();
        if (_renderingMode == VideoRenderingMode.D3DImage) StartSession(0);
        else if (_host is { Handle: not 0 }) StartSession(_host.Handle);
    }

    /// <summary>播放；如果还在异步打开，则在 MediaOpened 后执行。</summary>
    public void Play()
    {
        Verify();
        if (State == PlaybackState.Opening)
        {
            _playPending = true;
            return;
        }

        if (CanPlay) _session?.Play();
    }

    /// <summary>暂停；也取消尚未完成打开的待播放请求。</summary>
    public void Pause()
    {
        Verify();
        _playPending = false;
        if (CanPause) _session?.Pause();
    }

    /// <summary>停止并回到起点；取消待播放请求。</summary>
    public void Stop()
    {
        Verify();
        _playPending = false;
        if (IsReady) _session?.Stop();
    }

    /// <summary>定位；仅当 CanSeek 为 true 时调用，核心按时长截断。</summary>
    public void Seek(TimeSpan position)
    {
        Verify();
        if (CanSeek) _session?.Seek(position);
    }

    /// <summary>释放当前会话，保留视频宿主和设置，允许再次 Open。</summary>
    public void Close()
    {
        Verify();
        Source = null;
        ReleaseSession();
        _modeLocked = false;
        LastError = null;
        Publish();
    }

    /// <summary>永久释放会话和 HWND；重复调用安全，必须在创建线程执行。</summary>
    public void Dispose()
    {
        _dispatcher.VerifyAccess();
        if (_disposed) return;
        Source = null;
        ReleaseSession();
        _disposed = true;
        DestroyHost();
        Publish();
    }

    private void Verify()
    {
        _dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void CreateHost()
    {
        _host = new VideoHost();
        _host.Created += OnCreated;
        _host.Destroying += ReleaseSession;
        _host.Resized += OnResize;
        _view.Children.Add(_host);
    }

    private void DestroyHost()
    {
        if (_host == null) return;
        _host.Created -= OnCreated;
        _host.Destroying -= ReleaseSession;
        _host.Resized -= OnResize;
        _view.Children.Remove(_host);
        _host.Dispose();
        _host = null;
    }

    private void OnCreated(nint hwnd)
    {
        if (!_disposed && Source != null && _session == null) StartSession(hwnd);
    }

    private void StartSession(nint hwnd)
    {
        var source = Source ?? throw new InvalidOperationException(Text.Get("NoMedia"));
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(_dispatcher));
        try
        {
            IPlaybackSession created;
            if (_renderingMode == VideoRenderingMode.D3DImage)
            {
                created = MediaPlaybackSession.CreateD3DImageSession(_scalingMode);
                _session = created;
                _presenter = new MFPlayD3DImagePresenter((MediaPlaybackSession)created, ReportError);
                _view.Children.Add(_presenter.View);
            }
            else created = new MediaPlaybackSession(hwnd);

            _session = created;
            System.Diagnostics.Trace.WriteLine
                ($"backend=MFPlay mode={_renderingMode} session-created", "MediaFoundation.Controls");
            created.StateChanged += OnStateChanged;
            created.MediaOpened += OnOpened;
            created.PlaybackEnded += OnEnded;
            created.Failed += OnFailed;
            created.Volume = _volume;
            created.IsMuted = _muted;
            created.PlaybackRate = _rate;
            created.IsLooping = _looping;
            created.Open(source);
            if (created.State != PlaybackState.Failed) _timer.Start();
            Snapshot();
        }
        catch
        {
            ReleaseSession(PlaybackState.Failed);
            throw;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private void ReleaseSession() => ReleaseSession(PlaybackState.Closed);

    private void ReleaseSession(PlaybackState finalState)
    {
        _timer.Stop();
        _playPending = false;
        if (_presenter != null)
        {
            _view.Children.Remove(_presenter.View);
            _presenter.Dispose();
            _presenter = null;
        }

        if (_session is { } old)
        {
            _session = null;
            old.StateChanged -= OnStateChanged;
            old.MediaOpened -= OnOpened;
            old.PlaybackEnded -= OnEnded;
            old.Failed -= OnFailed;
            old.Dispose();
        }

        Position = Duration = TimeSpan.Zero;
        VideoWidth = VideoHeight = 0;
        HasVideo = HasAudio = false;
        State = finalState;
        Publish();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _session)) Snapshot();
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, _session)) return;
        Snapshot();
        var opened = _session;
        MediaOpened?.Invoke(this, e);
        if (!ReferenceEquals(opened, _session)) return;
        var shouldPlay = _playPending;
        _playPending = false;
        if (shouldPlay && CanPlay)
        {
            try
            {
                Play();
            }
            catch (ExternalException error)
            {
                ReportError(error);
            }
            catch (InvalidOperationException error)
            {
                ReportError(error);
            }
        }
    }

    private void OnEnded(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, _session)) return;
        Snapshot();
        PlaybackEnded?.Invoke(this, e);
    }

    private void OnFailed(object? sender, PlaybackFailedEventArgs e)
    {
        if (!ReferenceEquals(sender, _session)) return;
        ReportError(e.Error);
    }

    internal void ReportError(Exception error)
    {
        System.Diagnostics.Trace.WriteLine
            ($"Player failure mode={_renderingMode} HRESULT=0x{error.HResult:X8} {error}", "MediaFoundation.Controls");
        LastError = error;
        ReleaseSession(PlaybackState.Failed);
        Failed?.Invoke(this, new PlaybackFailedEventArgs(error));
    }

    private void OnResize()
    {
        if (_disposed) return;
        try
        {
            _session?.ResizeVideo();
        }
        catch (ExternalException error)
        {
            ReportError(error);
        }
    }

    private long _lastDiagnosticTick;

    private void OnTick(object? sender, EventArgs e)
    {
        try
        {
            _session?.Refresh();
            Snapshot();
            if (_session != null && Environment.TickCount64 - _lastDiagnosticTick >= 5000)
            {
                _lastDiagnosticTick = Environment.TickCount64;
                System.Diagnostics.Trace.WriteLine
                (
                    $"Player progress mode={_renderingMode} state={State} position={Position} duration={Duration} rate={_rate} video={VideoWidth}x{VideoHeight}",
                    "MediaFoundation.Controls"
                );
            }
        }
        catch (ExternalException error)
        {
            ReportError(error);
        }
        catch (InvalidOperationException error)
        {
            ReportError(error);
        }
    }

    private void Snapshot()
    {
        if (_session is not { } current) return;
        State = current.State;
        Position = current.Position;
        Duration = current.Duration;
        VideoWidth = current.VideoWidth;
        VideoHeight = current.VideoHeight;
        HasVideo = current.HasVideo;
        HasAudio = current.HasAudio;
        Publish();
    }

    private void Publish() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke
        (this, new PropertyChangedEventArgs(name));
}
