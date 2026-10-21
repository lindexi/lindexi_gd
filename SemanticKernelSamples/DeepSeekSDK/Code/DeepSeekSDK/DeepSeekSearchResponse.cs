using System.Text.Json.Serialization;

namespace DeepSeekSDK;

/// <summary>网页搜索响应。</summary>
public sealed record DeepSeekSearchResponse
{
    /// <summary>搜索结果；没有匹配结果时为空数组。</summary>
    [JsonPropertyName("results")]
    [JsonRequired]
    public required IReadOnlyList<DeepSeekSearchResult> Results { get; init; }
}

/// <summary>单条网页搜索结果。</summary>
public sealed record DeepSeekSearchResult
{
    /// <summary>排序分数，范围为 0～3；大于等于 1.5 为优质结果。</summary>
    [JsonPropertyName("rerank_score")]
    [JsonRequired]
    public required float RerankScore { get; init; }

    /// <summary>搜索结果标题。</summary>
    [JsonPropertyName("title")]
    [JsonRequired]
    public required string Title { get; init; }

    /// <summary>网页链接。</summary>
    [JsonPropertyName("url")]
    [JsonRequired]
    public required string Url { get; init; }

    /// <summary>适用于展示的短摘要。</summary>
    [JsonPropertyName("snippet")]
    [JsonRequired]
    public required string Snippet { get; init; }

    /// <summary>适用于模型阅读的长摘要。</summary>
    [JsonPropertyName("summary")]
    [JsonRequired]
    public required string Summary { get; init; }

    /// <summary>网页正文；未请求或缺失时为空字符串。</summary>
    [JsonPropertyName("content")]
    [JsonRequired]
    public required string Content { get; init; }

    /// <summary>站点名称；缺失时为 null。</summary>
    [JsonPropertyName("site_name")]
    [JsonRequired]
    public required string? SiteName { get; init; }

    /// <summary>站点图标链接；缺失时为 null。</summary>
    [JsonPropertyName("site_icon")]
    [JsonRequired]
    public required string? SiteIcon { get; init; }

    /// <summary>UTC 发布时间；未知时为 null。</summary>
    [JsonPropertyName("date")]
    [JsonRequired]
    public required DateTimeOffset? Date { get; init; }

    /// <summary>网页权威性，范围为 -1～4；-1 表示未知，大于等于 3 为极权威结果。</summary>
    [JsonPropertyName("authority")]
    [JsonRequired]
    public required int Authority { get; init; }
}
