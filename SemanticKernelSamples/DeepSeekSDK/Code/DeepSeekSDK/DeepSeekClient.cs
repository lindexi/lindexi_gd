using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DeepSeekSDK;

/// <summary>DeepSeek 工具 API 客户端。调用方负责管理传入的 HttpClient 的生命周期。</summary>
public sealed class DeepSeekClient
{
    private static readonly Uri SearchEndpoint = new("https://api.deepseek.com/v1/tools/search");
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    /// <summary>使用调用方管理的 HTTP 客户端及 API Key 创建客户端。</summary>
    public DeepSeekClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    /// <summary>搜索网页。非成功状态通过 HttpRequestException 返回，取消通过 OperationCanceledException 返回。</summary>
    public async Task<DeepSeekSearchResponse> SearchAsync
    (
        DeepSeekSearchRequest request,
        CancellationToken cancellationToken = default
    )
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, SearchEndpoint);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        message.Content = JsonContent.Create(request);

        using var response = await _httpClient.SendAsync
        (
            message, HttpCompletionOption.ResponseHeadersRead, cancellationToken
        ).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DeepSeekSearchResponse>(cancellationToken)
            .ConfigureAwait(false) ?? throw new JsonException();
    }
}