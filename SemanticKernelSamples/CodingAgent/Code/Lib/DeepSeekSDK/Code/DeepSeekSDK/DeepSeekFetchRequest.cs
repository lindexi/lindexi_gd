using System.Text.Json.Serialization;

namespace DeepSeekSDK;

/// <summary>网页抓取请求。</summary>
public sealed record DeepSeekFetchRequest
{
    /// <summary>要抓取的网页地址。</summary>
    /// <remarks>
    /// 服务端要求长度不超过 8192，必须为含 host 的 HTTP/HTTPS URL，且不能携带用户名或密码。
    /// 客户端不校验或改写地址，原样传递给服务端处理。
    /// </remarks>
    [JsonPropertyName("url")]
    [JsonRequired]
    public required string Url { get; init; }
}
