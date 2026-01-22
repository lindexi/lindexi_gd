using HttpWebClients.HostBackup;
using HttpWebClients.HttpProviders;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace HttpWebClients;

public class HttpWebClientBuilder
{
    public HttpWebClientBuilder UseHost(string host)
    {
        _host = host;
        return this;
    }

    private string? _host;

    private HostBackupManager? _hostBackupManager;

    private List<JsonSerializerContext>? _jsonSerializerContextList;


    /// <summary>
    /// 设置提供 <see cref="HttpClient"/> 对象
    /// </summary>
    /// <param name="httpClientProvider"></param>
    /// <returns></returns>
    public HttpWebClientBuilder UseHttpClientProvider(IHttpClientProvider httpClientProvider)
    {
        HttpClientProvider = httpClientProvider;
        return this;
    }

    /// <summary>
    /// 使用指定的 <see cref="HttpClient"/> 对象，传入的 <see cref="HttpClient"/> 对象 必须由传入方负责释放
    /// </summary>
    /// <param name="httpClient"></param>
    /// <returns></returns>
    public HttpWebClientBuilder UseHttpClient(HttpClient httpClient)
    {
        HttpClientProvider = new IncomingHttpClientProvider(httpClient);
        return this;
    }

    /// <summary>
    /// 使用特殊的方式创建 <see cref="HttpClient"/> 对象，创建的 <see cref="HttpClient"/> 对象将会自动释放
    /// </summary>
    /// <param name="httpClientCreator"></param>
    /// <returns></returns>
    public HttpWebClientBuilder UseHttpClient(Func<HttpClient> httpClientCreator)
    {
        HttpClientProvider = new ConfigurableHttpClientProvider(httpClientCreator);
        return this;
    }

    private IHttpClientProvider? HttpClientProvider { set; get; }
}