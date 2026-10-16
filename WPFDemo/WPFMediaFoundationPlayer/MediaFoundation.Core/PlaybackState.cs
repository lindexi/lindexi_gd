namespace MediaFoundation.Core;

/// <summary>媒体会话的已确认状态。</summary>
public enum PlaybackState
{
    /// <summary>尚未打开媒体。</summary>
    Closed,
    /// <summary>正在异步解析或设置媒体。</summary>
    Opening,
    /// <summary>媒体已就绪，或已停止并回到起点。</summary>
    Stopped,
    /// <summary>正在播放。</summary>
    Playing,
    /// <summary>已暂停。</summary>
    Paused,
    /// <summary>已到达媒体末尾。</summary>
    Ended,
    /// <summary>原生操作失败；可重新打开媒体。</summary>
    Failed
}
