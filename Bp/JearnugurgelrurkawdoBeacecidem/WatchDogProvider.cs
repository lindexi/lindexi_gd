using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;

//using WatchDog.Core.Context;
//using WatchDog.Service.Contexts;

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

    //public async Task<GetWangResponse?> GetWangAsync(GetWangInfo getWangInfo)
    //{
    //    var request = new GetWangRequest(getWangInfo);
    //    var response = await _httpClient.PostAsJsonAsync("Dog/Wang", request);
    //    return await response.Content.ReadFromJsonAsync<GetWangResponse>();
    //}

    //public async Task<MuteResponse?> MuteAsync(MuteInfo muteInfo)
    //{
    //    var request = new MuteRequest(muteInfo);
    //    var response = await _httpClient.PostAsJsonAsync("Dog/Mute", request);
    //    return await response.Content.ReadFromJsonAsync<MuteResponse>();
    //}

    public static WatchDogProvider? CreateFromConfiguration()
    {
        var defaultConfigurationFile = @"C:\lindexi\Configuration\WatchDog.coin";
        var configurationFile = defaultConfigurationFile;

        if (!System.IO.File.Exists(configurationFile))
        {
            return null;
        }

        var appConfigurator = ConfigurationFactory.FromFile(configurationFile, RepoSyncingBehavior.Static).CreateAppConfigurator();
        var host = appConfigurator.Of<WatchDogConfiguration>().Host;
        if (string.IsNullOrEmpty(host))
        {
            return null;
        }

        return new WatchDogProvider(host);
    }

    class WatchDogConfiguration : Configuration
    {
        public string Host
        {
            set => SetValue(value);
            get => GetString();
        }
    }
}

/// <summary>
/// 喂狗信息
/// </summary>
/// <param name="Name">名称</param>
/// <param name="Status">状态</param>
/// <param name="Id">Id号，如为空则自动分配</param>
/// <param name="DelaySecond">喂狗允许的延迟时间，超过时间就被狗咬</param>
/// <param name="MaxDelayCount">最多的次数，一般是 1 的值</param>
/// <param name="NotifyIntervalSecond">通知的间隔时间</param>
/// <param name="NotifyMaxCount">最多的通知次数，默认是无限通知</param>
/// <param name="MaxCleanTimeSecond">如果经过了多久都没有响应，则清除喂狗信息。默认是 7 天</param>
public record FeedDogInfo
(
    string Name,
    string Status,
    string? Id = null,
    uint DelaySecond = 60,
    uint MaxDelayCount = 1,
    uint NotifyIntervalSecond = 60 * 30,
    int NotifyMaxCount = -1,
    int MaxCleanTimeSecond = 60 * 60 * 24 * 7
);

public record FeedDogRequest(FeedDogInfo FeedDogInfo);

public record FeedDogResponse(FeedDogResult FeedDogResult);

/// <summary>
/// 喂狗的结果，返回的是喂狗的信息
/// </summary>
/// <param name="Id"></param>
/// <param name="DelaySecond"></param>
/// <param name="MaxDelayCount"></param>
/// <param name="NotifyIntervalSecond"></param>
/// <param name="NotifyMaxCount"></param>
/// <param name="RegisterTime">注册时间</param>
/// <param name="IsRegister">这一条是否输出注册的，首次喂狗</param>
public record FeedDogResult(string Id, uint DelaySecond, uint MaxDelayCount, uint NotifyIntervalSecond, int NotifyMaxCount, DateTimeOffset RegisterTime, bool IsRegister);