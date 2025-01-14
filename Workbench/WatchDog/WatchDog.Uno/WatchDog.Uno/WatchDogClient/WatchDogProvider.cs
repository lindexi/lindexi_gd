using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using WatchDog.Core.Context;
using WatchDog.Service.Contexts;

namespace WatchDog.Uno.WatchDogClient;

internal class WatchDogProvider
{
    public WatchDogProvider(string host)
    {
        Host = host;

        _httpClient = CreateHttpClient();
    }

    private readonly HttpClient _httpClient;

    private HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(Host);
        return httpClient;
    }

    public string Host { get; }

    public async Task<FeedDogResponse?> FeedAsync(FeedDogInfo feedDogInfo)
    {
        var request = new FeedDogRequest(feedDogInfo);
        var response = await _httpClient.PostAsJsonAsync("Dog/Feed", request);
        return await response.Content.ReadFromJsonAsync<FeedDogResponse>();
    }

    public async Task<GetWangResponse?> GetWangAsync(GetWangInfo getWangInfo)
    {
        var request = new GetWangRequest(getWangInfo);
        var response = await _httpClient.PostAsJsonAsync("Dog/Wang", request);
        return await response.Content.ReadFromJsonAsync<GetWangResponse>();
    }

    public async Task<MuteResponse?> MuteAsync(MuteInfo muteInfo)
    {
        var request = new MuteRequest(muteInfo);
        var response = await _httpClient.PostAsJsonAsync("Dog/Mute", request);
        return await response.Content.ReadFromJsonAsync<MuteResponse>();
    }
}
