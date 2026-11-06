using System.Text;

namespace MediaFoundation.Core;

/// <summary>内置文本字幕格式。</summary>
public enum SubtitleFormat
{
    /// <summary>SubRip。</summary>
    SubRip,
    /// <summary>WebVTT 文本时间轴。</summary>
    WebVtt,
    /// <summary>行时间轴 LRC，不含逐字时间效果。</summary>
    Lrc,
    /// <summary>基于帧号的 MicroDVD。</summary>
    MicroDvd,
    /// <summary>SubViewer 2.0 文本。</summary>
    SubViewer,
    /// <summary>MPL2 文本。</summary>
    Mpl2
}

/// <summary>内置字幕格式的文件加载和解析入口；文档与展示不依赖此入口。</summary>
public static class SubtitleReader
{
    /// <summary>加载 UTF-8 文本字幕，.sub 根据内容识别 MicroDVD 或 SubViewer。MicroDVD 须有文件帧率声明或显式 frameRate；文件访问异常保留原始原因，解析异常包含行号。</summary>
    public static async Task<SubtitleDocument> LoadAsync(string path, CancellationToken cancellationToken = default, double? frameRate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        SubtitleFormat? format = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".srt" => SubtitleFormat.SubRip,
            ".vtt" => SubtitleFormat.WebVtt,
            ".lrc" => SubtitleFormat.Lrc,
            ".sub" => (SubtitleFormat?)null,
            ".mpl" => SubtitleFormat.Mpl2,
            _ => throw new NotSupportedException(SR.Get("SubtitleExtension"))
        };
        string text = await File.ReadAllTextAsync(path, new UTF8Encoding(false, true), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (format == null)
        {
            string first = text.TrimStart('\uFEFF').TrimStart();
            format = first.StartsWith('{') ? SubtitleFormat.MicroDvd : SubtitleFormat.SubViewer;
        }
        return Parse(text, format.Value, frameRate);
    }

    /// <summary>使用指定内置格式解析文本，frameRate 仅用于 MicroDVD。自定义解析器可直接构造 SubtitleDocument。</summary>
    public static SubtitleDocument Parse(string text, SubtitleFormat format, double? frameRate = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (frameRate.HasValue && format != SubtitleFormat.MicroDvd)
            throw new ArgumentException(SR.Get("SubtitleFrameRateFormat"), nameof(frameRate));
        return format switch
        {
            SubtitleFormat.SubRip => SubRipParser.Parse(text),
            SubtitleFormat.WebVtt => WebVttParser.Parse(text),
            SubtitleFormat.Lrc => LrcParser.Parse(text),
            SubtitleFormat.MicroDvd => MicroDvdParser.Parse(text, frameRate),
            SubtitleFormat.SubViewer => SubViewerParser.Parse(text),
            SubtitleFormat.Mpl2 => Mpl2Parser.Parse(text),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }
}
