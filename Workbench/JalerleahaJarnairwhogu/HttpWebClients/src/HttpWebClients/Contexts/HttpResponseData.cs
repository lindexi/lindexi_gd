using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HttpWebClients.Contexts;

public class HttpResponseData : IDisposable
{
    internal HttpResponseData(IHttpRequestData requestData, HttpResponseMessage? httpResponseMessage, Exception? exception)
    {
        RequestData = requestData;
        HttpResponseMessage = httpResponseMessage;
        Exception = exception;
        Debug.Assert(httpResponseMessage != null || exception != null);
    }

    public IHttpRequestData RequestData { get; }
    public HttpResponseMessage? HttpResponseMessage { get; }
    public Exception? Exception { get; }

    public void EnsureHttpSuccess()
    {
        if (HttpResponseMessage is not null)
        {
            HttpResponseMessage.EnsureSuccessStatusCode();
        }
        else
        {
            Debug.Assert(Exception != null);
            ExceptionDispatchInfo.Throw(Exception);
        }
    }

    public string? ContentText => _contextText ??= ReadContentText();
    private string? _contextText;

    private string? ReadContentText()
    {
        if (HttpResponseMessage is null)
        {
            return null;
        }

        if (HttpResponseMessage.Content.Headers.ContentLength == 0)
        {
            return null;
        }

        using var stream = HttpResponseMessage.Content.ReadAsStream();
        using var streamReader = new StreamReader(stream);
        return streamReader.ReadToEnd();
    }

    public T As<T>(JsonSerializerContext? context = null)
    {
        context ??= RequestData.JsonSerializerContext;

        var contentText = ContentText;

        if (contentText is null)
        {
            contentText = "{}";
        }

        return JsonSerializer.Deserialize<T>(contentText, context?.Options)!;
    }

    public void Dispose()
    {
        HttpResponseMessage?.Dispose();
    }
}