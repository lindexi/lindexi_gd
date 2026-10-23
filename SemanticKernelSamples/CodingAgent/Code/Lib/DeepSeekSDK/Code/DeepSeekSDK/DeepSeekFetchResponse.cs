using System.Text.Json.Serialization;

namespace DeepSeekSDK;

/// <summary>网页抓取响应。</summary>
public sealed record DeepSeekFetchResponse
{
    /// <summary>页面标题；缺失时为空字符串。</summary>
    [JsonPropertyName("title")]
    [JsonRequired]
    public required string Title { get; init; }

    /// <summary>抓取后的 Markdown 正文。</summary>
    [JsonPropertyName("content")]
    [JsonRequired]
    public required string Content { get; init; }
}
