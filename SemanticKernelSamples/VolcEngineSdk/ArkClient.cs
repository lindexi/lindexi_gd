using System.Net.Http.Headers;
using System.Net.Http.Json;
using VolcEngineSdk.ArkImageGenerations;

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

    //public ArkImageGeneration Images => field ??= new ArkImageGeneration(this);

    internal HttpClient HttpClient => field ??= new HttpClient();

    internal void AppendAuthorization(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
    }
}