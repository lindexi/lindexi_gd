namespace MediaFoundation.Core;

/// <summary>读取本地可定位媒体的内嵌文本字幕，不参与音视频播放。</summary>
public static class EmbeddedSubtitleReader
{
    /// <summary>按文件签名读取 MKV 或 MP4 字幕轨。后台扫描仅保留字幕数据；失败保留路径、操作和异常原因。</summary>
    public static async Task<IReadOnlyList<EmbeddedSubtitleTrack>> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return await Task.Run<IReadOnlyList<EmbeddedSubtitleTrack>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                Span<byte> signature = stackalloc byte[8];
                stream.ReadExactly(signature);
                stream.Position = 0;
                if (signature[0] == 0x1A && signature[1] == 0x45 && signature[2] == 0xDF && signature[3] == 0xA3)
                    return MatroskaSubtitleReader.Read(stream, cancellationToken).AsReadOnly();
                return Mp4SubtitleReader.Read(stream, cancellationToken).AsReadOnly();
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or FormatException or NotSupportedException or OverflowException or System.Text.DecoderFallbackException)
            {
                throw new IOException(SR.Current.EmbeddedReadFailed.ToString(path, error.Message), error);
            }
        }, cancellationToken).ConfigureAwait(false);
    }
}
