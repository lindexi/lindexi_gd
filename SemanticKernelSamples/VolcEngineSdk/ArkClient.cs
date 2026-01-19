using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace VolcEngineSdk;

public class ArkClient
{
    public ArkClient(string apiKey, string baseUrl = "https://ark.cn-beijing.volces.com/api/v3")
    {
        ApiKey = apiKey;
        BaseUrl = baseUrl;
    }

    public string ApiKey { get; }

    public string BaseUrl { get; }

    public ArkContentGeneration ContentGeneration => field ??= new ArkContentGeneration(this);

    internal HttpClient HttpClient => field ??= new HttpClient();

    internal void AppendAuthorization(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
    }
}