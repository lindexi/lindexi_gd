using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using HttpWebClients.Contexts;

namespace HttpWebClients;

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

    public void Dispose()
    {
        HttpResponseMessage?.Dispose();
    }
}