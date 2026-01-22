using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using HttpWebClients.Contexts;

namespace HttpWebClients;

public class HttpJsonResponse<T>
{
    internal HttpJsonResponse(HttpWebClient client, IHttpRequestData requestData, HttpResponseMessage? httpResponseMessage, string? contentText, Exception? exception)
    {
        Client = client;
        RequestData = requestData;
        HttpResponseMessage = httpResponseMessage;
        ContentText = contentText;
        Exception = exception;
        Debug.Assert(httpResponseMessage != null || exception != null);
    }

    public HttpWebClient Client { get; }
    public IHttpRequestData RequestData { get; }
    public HttpResponseMessage? HttpResponseMessage { get; }
    public string? ContentText { get; }
    public Exception? Exception { get; }

    /// <summary>
    /// 处理错误情况
    /// </summary>
    /// <param name="data"></param>
    /// <param name="error"></param>
    /// <returns>返回 true 表示存在错误，需要处理</returns>
    public bool HandleError([NotNullWhen(false)] out T? data, [NotNullWhen(true)] out HttpJsonResponseError<T>? error)
    {
        error = new HttpJsonResponseError<T>()
        {
            OriginResponse = this,
        };

        data = Deserialize();

        return Exception != null;
    }

    public void ThrowIfError(out T data)
    {
        // 1. 网络异常
        // 2. Http 异常
        // 3. 反序列化异常
        // 4. 业务异常

        if (Exception is not null)
        {
            ExceptionDispatchInfo.Throw(Exception);
        }

        data = Deserialize();
    }

    private T Deserialize()
    {
        ArgumentNullException.ThrowIfNull(ContentText);

        var jsonTypeInfo = GetJsonTypeInfo();

        JsonTypeInfo<T>? GetJsonTypeInfo()
        {
            var result = RequestData.JsonSerializerContext?.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>;

            if (result is null)
            {
                result = Client.JsonSerializerContext.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>;
            }

            return result;
        }

        if (jsonTypeInfo is not null)
        {
            var data = JsonSerializer.Deserialize<T>(ContentText, jsonTypeInfo)!;
            return data;
        }
        else
        {
            var data = JsonSerializer.Deserialize<T>(ContentText)!;
            return data;
        }
    }

    //internal async Task<string> ReadContentTextAsync()
    //{
    //    //if (HttpResponseMessage is null)
    //    //{
    //    //    return null;
    //    //}

    //    if (HttpResponseMessage!.Content.Headers.ContentLength == 0)
    //    {
    //        return string.Empty;
    //    }

    //    await using var stream = await HttpResponseMessage.Content.ReadAsStreamAsync();
    //    using var streamReader = new StreamReader(stream, leaveOpen: true);
    //    return await streamReader.ReadToEndAsync();
    //}
}

public class HttpJsonResponseError<TResponse>
{
    public required HttpJsonResponse<TResponse> OriginResponse { get; init; }

    public HttpJsonResponse<T> AsResponse<T>()
    {
        return new HttpJsonResponse<T>(OriginResponse.Client, OriginResponse.RequestData,
            OriginResponse.HttpResponseMessage, OriginResponse.ContentText, OriginResponse.Exception);
    }
}