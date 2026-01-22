using System;
using System.Net.Http;

namespace HttpWebClients.HttpProviders;

/// <summary>
/// 可配置创建的 HttpClient 提供器
/// </summary>
public class ConfigurableHttpClientProvider : IHttpClientProvider
{
    /// <summary>
    /// 创建可配置创建的 HttpClient 提供器
    /// </summary>
    /// <param name="creator">创建出来的 HttpClient 对象将会在此类型进行释放</param>
    public ConfigurableHttpClientProvider(Func<HttpClient> creator)
    {
        _httpClient = creator();
    }

    /// <inheritdoc />
    public HttpClient GetHttpClient() => _httpClient;

    private readonly HttpClient _httpClient;

    /// <inheritdoc />
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}