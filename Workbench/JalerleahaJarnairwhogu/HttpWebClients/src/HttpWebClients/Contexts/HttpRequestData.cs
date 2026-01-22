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