using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HttpWebClients.Contexts;

public class HttpRequestData
{
}

public interface IHttpRequestData
{
    HttpMethod HttpMethod { get; }
    string Url { get; }

    JsonSerializerContext? JsonSerializerContext { get; }

    Type? HttpResponseDataType { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class HttpRequestDataAttribute : Attribute
{
    public HttpRequestDataAttribute(HttpMethodType httpMethod, string? url = null)
    {
    }

    public Type? ResponseType { get; set; }
}

public enum HttpMethodType
{
    Get,
    Post,
}

public interface IGetHttpRequestData : IHttpRequestData
{
    string? GetQueryString();
}


[HttpRequestData(HttpMethodType.Get, "/api/foo", ResponseType = typeof(FooTestHttpResponseData))]
public partial class FooTestGetHttpRequestData : HttpRequestData
{
    public string? Id { get; init; }
}

public partial class FooTestGetHttpRequestData : IGetHttpRequestData
{
    // 这是源代码生成器生成的部分

    public HttpMethod HttpMethod => HttpMethod.Get;
    public string Url => "/api/foo";
    public JsonSerializerContext? JsonSerializerContext { get; set; }
    public Type? HttpResponseDataType => typeof(FooTestHttpResponseData);

    public string? GetQueryString()
    {
        StringBuilder? queryStringBuilder = null;

        AppendQueryString(nameof(Id), Id);

        return queryStringBuilder?.ToString();

        void AppendQueryString(string key, string? value)
        {
            if (value is null)
            {
                return;
            }
            if (queryStringBuilder is null)
            {
                queryStringBuilder = new StringBuilder("?");
            }
            else
            {
                queryStringBuilder.Append('&');
            }
            queryStringBuilder.Append(Uri.EscapeDataString(key));
            queryStringBuilder.Append('=');
            queryStringBuilder.Append(Uri.EscapeDataString(value));
        }
    }

    // 对于 Get 类型的，额外添加方法，获取请求参数
}

// 附带生成请求扩展方法
public static partial class FooTestHttpRequestDataExtensions
{
    // 根据是 Get 还是 Post 请求，生成不同的扩展方法

    // 如果是没有指定 Response 类型的，则生成 HttpResponseData 返回值的扩展方法

    /// <summary>
    /// 请求
    /// </summary>
    /// <param name="client"></param>
    /// <param name="requestData"></param>
    /// <param name="url">如果要重新指定请求地址，则传入此参数。这个参数会根据 FooTestHttpRequestData 是否有标明，而决定是可空默认，还是必填参数</param>
    /// <returns></returns>
    public static async Task<HttpJsonResponse<FooTestHttpResponseData>> GetAsync(this HttpWebClient client,
        FooTestGetHttpRequestData requestData, string? url = null)
    {
        url ??= requestData.Url;
        var requestUrl = url + requestData.GetQueryString();

        var httpClient = client.AsHttpClient();
        HttpResponseMessage? httpResponseMessage = null;
        Exception? exception = null;
        string? contentText = null;
        try
        {
            httpResponseMessage = await httpClient.GetAsync(requestUrl);
            contentText = await httpResponseMessage.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            exception = e;
        }

        return new HttpJsonResponse<FooTestHttpResponseData>(client, requestData, httpResponseMessage, contentText, exception);
    }
}

public class FooTestHttpResponseData
{
}