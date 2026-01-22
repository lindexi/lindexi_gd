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
