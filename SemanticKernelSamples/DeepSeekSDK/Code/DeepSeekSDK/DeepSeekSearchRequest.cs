using System.Text.Json.Serialization;

namespace DeepSeekSDK;

/// <summary>网页搜索请求。</summary>
public sealed record DeepSeekSearchRequest
{
    /// <summary>搜索词，支持搜索语法，长度为 1～128 个 Unicode 字符。</summary>
    [JsonPropertyName("query")]
    public required string Query { get; init; }

    /// <summary>返回条数，范围为 1～50，默认为 10。</summary>
    [JsonPropertyName("count")]
    public int Count { get; init; } = 10;

    /// <summary>用户原始问题；null 表示未提供。</summary>
    [JsonPropertyName("ori_question")]
    public string? OriginalQuestion { get; init; }

    /// <summary>用于优先返回本地化结果的 IP 地址。</summary>
    [JsonPropertyName("localization")]
    public string Localization { get; init; } = string.Empty;

    /// <summary>是否返回网页正文，默认为 false。</summary>
    [JsonPropertyName("need_content")]
    public bool NeedContent { get; init; }
}
