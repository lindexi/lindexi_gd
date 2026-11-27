namespace MediaFoundation.Core;

/// <summary>内嵌字幕轨的只读快照；Id 仅在所属媒体中有效。</summary>
public sealed class EmbeddedSubtitleTrack
{
    internal EmbeddedSubtitleTrack(string id, string? name, string? language, string codec, SubtitleDocument? document, string? unsupportedReason)
    {
        Id = id;
        Name = name;
        Language = language;
        Codec = codec;
        Document = document;
        UnsupportedReason = unsupportedReason;
    }

    /// <summary>容器中的轨道标识。</summary>
    public string Id { get; }
    /// <summary>轨道名称，容器未提供时为 null。</summary>
    public string? Name { get; }
    /// <summary>容器声明的语言，未提供时为 null。</summary>
    public string? Language { get; }
    /// <summary>容器声明的字幕编码。</summary>
    public string Codec { get; }
    /// <summary>已解码的文本时间轴；不支持的轨道为 null。</summary>
    public SubtitleDocument? Document { get; }
    /// <summary>无法读取该轨道的原因；可读取轨道为 null。</summary>
    public string? UnsupportedReason { get; }
}
