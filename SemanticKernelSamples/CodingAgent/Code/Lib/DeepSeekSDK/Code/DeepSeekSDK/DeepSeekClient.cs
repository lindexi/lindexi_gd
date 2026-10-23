using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DeepSeekSDK;

/// <summary>DeepSeek 工具 API 客户端。调用方负责管理传入的 HttpClient 的生命周期。</summary>
public sealed class DeepSeekClient
{
    private const string ApiHost = "https://api.deepseek.com/";
    private const string SearchEndpoint = ApiHost + "v1/tools/search";
    private const string FetchEndpoint = ApiHost + "v1/tools/fetch";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    /// <summary>使用调用方管理的 HTTP 客户端及 API Key 创建客户端。</summary>
    public DeepSeekClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    /// <summary>搜索网页。非成功状态通过 HttpRequestException 返回，取消通过 OperationCanceledException 返回。</summary>
    public Task<DeepSeekSearchResponse> SearchAsync
    (
        DeepSeekSearchRequest request,
        CancellationToken cancellationToken = default
    ) => PostAsync<DeepSeekSearchRequest, DeepSeekSearchResponse>(SearchEndpoint, request,
        $"Search Query={request.Query}; Count={request.Count}; NeedContent={request.NeedContent}; Localization={request.Localization}; OriginalQuestion={request.OriginalQuestion}",
        cancellationToken);

    /// <summary>抓取网页，返回标题及 Markdown 正文。非成功状态通过 HttpRequestException 返回，取消通过 OperationCanceledException 返回。</summary>
    public Task<DeepSeekFetchResponse> FetchAsync
    (
        DeepSeekFetchRequest request,
        CancellationToken cancellationToken = default
    ) => PostAsync<DeepSeekFetchRequest, DeepSeekFetchResponse>(FetchEndpoint, request,
        $"Fetch Url={request.Url}", cancellationToken);

    private async Task<TResponse> PostAsync<TRequest, TResponse>
    (
        string endpoint,
        TRequest request,
        string context,
        CancellationToken cancellationToken
    )
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        message.Content = JsonContent.Create(request);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync
            (
                message, HttpCompletionOption.ResponseHeadersRead, cancellationToken
            ).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new HttpRequestException($"DeepSeek {context} 失败；endpoint={endpoint}；HTTP {(int?)exception.StatusCode}: {exception.Message}",
                exception, exception.StatusCode);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string? errorContent = null;
                Exception? readException = null;
                try
                {
                    errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is HttpRequestException or IOException ||
                    exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
                {
                    readException = exception;
                }
                cancellationToken.ThrowIfCancellationRequested();

                var errorMessage = string.IsNullOrWhiteSpace(errorContent)
                    ? response.ReasonPhrase ?? response.StatusCode.ToString()
                    : errorContent;
                throw new HttpRequestException($"DeepSeek {context} 失败；endpoint={endpoint}；HTTP {(int)response.StatusCode}: {errorMessage}",
                    readException, response.StatusCode);
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
                    .ConfigureAwait(false) ?? throw new JsonException();
            }
            catch (HttpRequestException exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new HttpRequestException($"DeepSeek {context} 失败；endpoint={endpoint}；HTTP {(int?)exception.StatusCode}: {exception.Message}",
                    exception, exception.StatusCode);
            }
        }
    }
}
