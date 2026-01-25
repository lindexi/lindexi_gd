using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HttpWebClients.HttpProviders;

class ProxyHttpMessageHandler : HttpMessageHandler
{
    public ProxyHttpMessageHandler(HttpClient baseClient)
    {
        _baseClient = baseClient;
    }

    private readonly HttpClient _baseClient;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _baseClient.SendAsync(request, cancellationToken);
    }
}