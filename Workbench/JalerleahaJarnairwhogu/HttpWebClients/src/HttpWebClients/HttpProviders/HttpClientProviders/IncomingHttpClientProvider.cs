using System.Net.Http;

namespace HttpWebClients.HttpProviders;

/// <summary>
/// 外部传入的 HttpClient 对象，由外部决定 HttpClient 的释放
/// </summary>
class IncomingHttpClientProvider : IHttpClientProvider
{
    public IncomingHttpClientProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public HttpClient GetHttpClient() => _httpClient;

    private readonly HttpClient _httpClient;

    public void Dispose()
    {
        // 啥都不做
    }
}