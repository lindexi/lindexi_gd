namespace MediaFoundation.Core;

/// <summary>媒体管线异步失败的详细信息。</summary>
public sealed class PlaybackFailedEventArgs : EventArgs
{
    /// <summary>创建失败通知。</summary>
    public PlaybackFailedEventArgs(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    /// <summary>原始错误，原生失败保留 HRESULT。</summary>
    public Exception Error { get; }
}
