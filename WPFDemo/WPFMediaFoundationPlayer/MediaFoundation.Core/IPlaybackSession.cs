namespace MediaFoundation.Core;

/// <summary>由线程拥有的原生播放会话；宿主负责定期刷新及先于视频资源释放会话。</summary>
public interface IPlaybackSession : IDisposable
{
    /// <summary>状态改变。</summary>
    event EventHandler? StateChanged;
    /// <summary>媒体打开完成。</summary>
    event EventHandler? MediaOpened;
    /// <summary>自然播放结束。</summary>
    event EventHandler? PlaybackEnded;
    /// <summary>异步播放失败。</summary>
    event EventHandler<PlaybackFailedEventArgs>? Failed;
    /// <summary>已确认的状态。</summary>
    PlaybackState State { get; }
    /// <summary>最近采样的位置。</summary>
    TimeSpan Position { get; }
    /// <summary>已知时长，未知为零。</summary>
    TimeSpan Duration { get; }
    /// <summary>视频像素宽度。</summary>
    int VideoWidth { get; }
    /// <summary>视频像素高度。</summary>
    int VideoHeight { get; }
    /// <summary>是否存在视频。</summary>
    bool HasVideo { get; }
    /// <summary>是否存在音频。</summary>
    bool HasAudio { get; }
    /// <summary>线性音量，范围 0 到 1。</summary>
    double Volume { get; set; }
    /// <summary>是否静音。</summary>
    bool IsMuted { get; set; }
    /// <summary>正向播放速率。</summary>
    double PlaybackRate { get; set; }
    /// <summary>是否循环。</summary>
    bool IsLooping { get; set; }
    /// <summary>异步打开绝对媒体地址。</summary>
    void Open(Uri source);
    /// <summary>播放已打开的媒体。</summary>
    void Play();
    /// <summary>暂停。</summary>
    void Pause();
    /// <summary>停止并定位到零。</summary>
    void Stop();
    /// <summary>定位到指定位置。</summary>
    void Seek(TimeSpan position);
    /// <summary>更新时钟和原生状态。</summary>
    void Refresh();
    /// <summary>通知 HWND 渲染器刷新尺寸；frame-server 无需此操作。</summary>
    void ResizeVideo();
}
