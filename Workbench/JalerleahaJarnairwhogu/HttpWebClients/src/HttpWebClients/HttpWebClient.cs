using HttpWebClients.Configurations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HttpWebClients;

/// <summary>
/// 提供与后台 HTTP 请求的能力客户端
/// </summary>
/// 此类是本程序集入口
/// 参考项目：
/// - https://github.com/dotnetcore/WebApiClient
/// - https://github.com/reactiveui/refit
public class HttpWebClient : IDisposable
{
    internal HttpWebClient(HttpWebClientConfiguration configuration)
    {
        if (configuration.IsUsed)
        {
            throw new InvalidOperationException();
        }

        configuration.IsUsed = true;
        configuration.OwnerHttpWebClient = this;

        _baseClient = configuration.HttpClientProvider.GetHttpClient();

        JsonSerializerContext = configuration.MainJsonSerializerContext;
    }

    public static HttpWebClientBuilder CreateBuilder() => new HttpWebClientBuilder();

    internal JsonSerializerContext JsonSerializerContext { get; }

    private readonly HttpClient _baseClient;

    public HttpClient AsHttpClient()
    {
        return _baseClient;
        //return new HttpClient(new ProxyHttpMessageHandler(_baseClient));
    }

    public void Dispose()
    {
        _baseClient.Dispose();
    }
}