using System.Text.Json.Serialization;

namespace DeepSeekSDK;

/// <summary>网页搜索请求。</summary>
public sealed record DeepSeekSearchRequest
{
    /// <summary>搜索词，支持搜索语法，长度为 1～128 个 Unicode 字符。</summary>
    /// <remarks>
    /// <para>query 中的搜索语法会原样传递给搜索服务。下表中的示例将操作符与搜索词组合使用；你可以在一个 query 中组合多个操作符。</para>
    /// <list type="table">
    /// <listheader>
    /// <term>操作符及示例</term>
    /// <description>说明</description>
    /// </listheader>
    /// <item>
    /// <term><c>site:</c>：<c>DeepSeek site:github.com</c></term>
    /// <description>将结果限制在指定站点，指定多个时用空格分隔并通过 OR 连接，例如 <c>DeepSeek site:github.com OR site:huggingface.co</c>。</description>
    /// </item>
    /// <item>
    /// <term><c>-site:</c>：<c>DeepSeek -site:example.com</c></term>
    /// <description>排除指定站点，指定多个时用空格分隔。</description>
    /// </item>
    /// <item>
    /// <term><c>-</c>：<c>DeepSeek -广告</c></term>
    /// <description>排除包含指定关键词的网页，指定多个时用空格分隔。</description>
    /// </item>
    /// <item>
    /// <term><c>-"..."</c>：<c>DeepSeek -"release notes"</c></term>
    /// <description>排除包含指定短语的网页，短语内可以包含空格，指定多个时用空格分隔。</description>
    /// </item>
    /// <item>
    /// <term><c>"..."</c>：<c>DeepSeek "精确短语"</c></term>
    /// <description>精确匹配指定短语，指定多个时用空格分隔。</description>
    /// </item>
    /// <item>
    /// <term><c>lang:</c>：<c>DeepSeek lang:zh,en</c></term>
    /// <description>将结果限制在指定语言，多种语言用逗号分隔，例如 <c>lang:zh,en</c>。</description>
    /// </item>
    /// <item>
    /// <term><c>after:</c>：<c>DeepSeek after:2025</c> 或 <c>DeepSeek after:2025-01-01</c></term>
    /// <description>将结果限制在指定日期之后发布的网页。</description>
    /// </item>
    /// <item>
    /// <term><c>before:</c>：<c>DeepSeek before:2026</c> 或 <c>DeepSeek before:2026-06-01</c></term>
    /// <description>将结果限制在指定日期之前发布的网页。</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code language="json">
    /// {
    ///   "query": "DeepSeek 模型 site:github.com OR site:huggingface.co lang:zh after:2025",
    ///   "count": 10
    /// }
    /// </code>
    /// </example>
    [JsonPropertyName("query")]
    [JsonRequired]
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
