using Microsoft.AspNetCore.Mvc;

using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Implementations;

using WatchDog.Core;
using WatchDog.Core.Context;
using WatchDog.Core.Services;

namespace WatchDog.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class DogController : ControllerBase
{
    public DogController(ILogger<DogController> logger, IMasterHostProvider masterHostProvider,IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _masterHostProvider = masterHostProvider;
        _httpClientFactory = httpClientFactory;
    }

    private readonly ILogger<DogController> _logger;

    private readonly IMasterHostProvider _masterHostProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    [HttpGet]
    [Route("Enable")]
    public string GetEnable()
    {
        return "Enable";
    }

    [HttpPost]
    [Route("Feed")]
    public async Task<FeedDogResponse?> FeedAsync(FeedDogRequest request)
    {
        var host = GetMasterHostAsync();
        var url = $"{host}WatchDog/Feed";
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsJsonAsync(url, request);
        var result = await response.Content.ReadFromJsonAsync<FeedDogResponse>();
        return result;
    }

    private async Task<string> GetMasterHostAsync()
    {
        var result = await _masterHostProvider.GetMasterHostAsync();
        var masterHost = result.MasterHost;

        if (masterHost.StartsWith("Fake-") || result.SelfIsMaster)
        {
            return "http://127.0.0.1:57725/";
        }
        else
        {
            return $"http://{masterHost}:57725/";
        }
    }

    [HttpPost]
    [Route("Wang")]
    public async Task<GetWangResponse?> GetWangAsync(GetWangRequest request)
    {
        var host = GetMasterHostAsync();
        var url = $"{host}WatchDog/Wang";
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsJsonAsync(url, request);
        var result = await response.Content.ReadFromJsonAsync<GetWangResponse>();
        return result;
    }

    [HttpPost]
    [Route("Mute")]
    public async Task<MuteResponse?> MuteAsync(MuteRequest request)
    {
        var host = GetMasterHostAsync();
        var url = $"{host}WatchDog/Mute";
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsJsonAsync(url, request);
        var result = await response.Content.ReadFromJsonAsync<MuteResponse>();
        return result;
    }
}

public record MuteRequest(MuteInfo MuteInfo);

public record MuteResponse();

public record GetWangRequest(GetWangInfo GetWangInfo);

public record GetWangResponse(GetWangResult GetWangResult);

public record FeedDogRequest(FeedDogInfo FeedDogInfo);

public record FeedDogResponse(FeedDogResult FeedDogResult);

[ApiController]
[Route("[controller]")]
public class WatchDogController : ControllerBase
{
    public WatchDogController(WatchDogProvider watchDogProvider)
    {
        _watchDogProvider = watchDogProvider;
    }

    private readonly WatchDogProvider _watchDogProvider;

    [HttpPost]
    [Route("Feed")]
    public async Task<FeedDogResponse?> FeedAsync(FeedDogRequest request)
    {
        await Task.CompletedTask;

        lock (_watchDogProvider)
        {
            var result = _watchDogProvider.Feed(request.FeedDogInfo);
            return new FeedDogResponse(result);
        }
    }

    [HttpPost]
    [Route("Wang")]
    public async Task<GetWangResponse?> GetWangAsync(GetWangRequest request)
    {
        await Task.CompletedTask;
        lock (_watchDogProvider)
        {
            var result = _watchDogProvider.GetWang(request.GetWangInfo);
            return new GetWangResponse(result);
        }
    }

    [HttpPost]
    [Route("Mute")]
    public async Task<MuteResponse?> MuteAsync(MuteRequest request)
    {
        await Task.CompletedTask;
        lock (_watchDogProvider)
        {
            _watchDogProvider.Mute(request.MuteInfo);
            return new MuteResponse();
        }
    }
}

//public class MasterHostMiddleware
//{
//    public MasterHostMiddleware(RequestDelegate next)
//    {
//        _next = next;
//    }

//    private readonly RequestDelegate _next;

//    public async Task InvokeAsync(HttpContext context)
//    {

//    }
//}

/// <summary>
/// 用于获取主设备信息
/// </summary>
public interface IMasterHostProvider
{
    /// <summary>
    /// 获取主设备信息
    /// </summary>
    /// <returns></returns>
    Task<MasterHostResult> GetMasterHostAsync();
}

/// <summary>
/// 用于检查主设备是否可用
/// </summary>
public interface IMasterHostStatusChecker
{
    /// <summary>
    /// 检查主设备是否可用
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    Task<bool> CheckMasterHostEnableAsync(string host);
}

public class MasterHostStatusChecker : IMasterHostStatusChecker
{
    public MasterHostStatusChecker(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private readonly IHttpClientFactory _httpClientFactory;

    public async Task<bool> CheckMasterHostEnableAsync(string host)
    {
        var httpClient = _httpClientFactory.CreateClient();
        if (host.StartsWith("Fake-"))
        {
            return true;
        }

        try
        {
            // 这里可以使用 HttpClient 去请求一下，看看是否可用
            var url = $"http://{host}/Dog/Enable";
            var result = await httpClient.GetStringAsync(url);
            _ = result;
            // 不用判断返回值，只要能访问就可以
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}

/// <summary>
/// 基于 Redis 的主设备提供者
/// </summary>
public class RedisMasterHostProvider : IMasterHostProvider, IDisposable
{
    public RedisMasterHostProvider(IRedisClient redisClient, ISelfHostProvider selfHostProvider, IMasterHostStatusChecker masterHostStatusChecker)
    {
        _redisClient = redisClient;
        _selfHostProvider = selfHostProvider;
        _masterHostStatusChecker = masterHostStatusChecker;
    }

    private readonly IRedisClient _redisClient;

    private readonly ISelfHostProvider _selfHostProvider;
    private readonly IMasterHostStatusChecker _masterHostStatusChecker;

    private SemaphoreSlim SemaphoreSlim { get; } = new SemaphoreSlim(1);

    public async Task<MasterHostResult> GetMasterHostAsync()
    {
        // 一个进程只能同时有一个线程在执行，减少复杂度
        await SemaphoreSlim.WaitAsync();
        try
        {
            var result = await GetMasterHostAsyncInner();
            _cacheMasterHost = result.MasterHost;
            if (result.SelfIsMaster)
            {
                _ = UpdateSelfMaterAsync(result.MasterHost);
            }
            return result;
        }
        finally
        {
            SemaphoreSlim.Release();
        }
    }

    private const string RedisLockKey = "WatchDog-MasterLock-4ACF7B3F-222D-469C-B99D-5E3966FFB422";
    private string? _selfHost;

    private async Task<MasterHostResult> GetMasterHostAsyncInner()
    {
        _selfHost ??= await _selfHostProvider.GetSelfHostAsync();
        var selfHost = _selfHost;
        // 先获取一遍，发现存在了，则尝试访问一下。如果能够通过，证明主设备存在
        // 如果发现存在的就是自己，那自己就是主设备

        for (int i = 0; i < 1000; i++)
        {
            var (success, masterHost) = await TryGetMasterHostAsync();
            if (success)
            {
                if (masterHost == selfHost)
                {
                    // 如果就是自己，那就不需要再判断是否可用了
                    return new MasterHostResult(SelfIsMaster: true, selfHost);
                }
                else
                {
                    // 判断一下是否可用
                    var enable = await _masterHostStatusChecker.CheckMasterHostEnableAsync(masterHost);
                    if (enable)
                    {
                        return new MasterHostResult(SelfIsMaster: false, masterHost);
                    }
                    else
                    {
                        // 不可用，那就继续以下的注册逻辑
                    }
                }
            }
            else
            {
                // 没获取成功，则继续尝试注册逻辑
            }

            // 没有主设备，那就尝试自己注册
            await _redisClient.Db0.AddAsync(RedisLockKey, selfHost, TimeSpan.FromHours(1));

            // 注册之后，等一下，再次尝试获取
            await Task.Delay(100);
        }

        throw new InvalidOperationException();

        async Task<(bool Success, string MasterHost)> TryGetMasterHostAsync()
        {
            var masterHost = await _redisClient.Db0.GetAsync<string>(RedisLockKey);
            if (string.IsNullOrEmpty(masterHost))
            {
                // 不存在主设备
                return (false, string.Empty);
            }

            if (string.Equals(masterHost, _cacheMasterHost))
            {
                // 和缓存的相同，则可立刻返回，不需要等待
                return (true, masterHost);
            }

            for (int i = 0; i < 1000; i++)
            {
                await Task.Delay(300);
                var host = await _redisClient.Db0.GetAsync<string>(RedisLockKey);
                if (string.IsNullOrEmpty(host))
                {
                    return (false, string.Empty);
                }

                if (host == masterHost)
                {
                    // 两次获取相同，即可证明是主设备
                    return (true, host);
                }
                else
                {
                    masterHost = host;
                }
            }

            return (false, string.Empty);
        }
    }

    private string? _cacheMasterHost;

    /// <summary>
    /// 更新自己的主设备信息，防止 Redis 过期
    /// </summary>
    /// <param name="selfHost"></param>
    /// <returns></returns>
    private async Task UpdateSelfMaterAsync(string selfHost)
    {
        if (_running)
        {
            return;
        }

        _running = true;

        try
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                await _redisClient.Db0.AddAsync(RedisLockKey, selfHost, TimeSpan.FromHours(1));
            }
        }
        finally
        {
            _running = false;
        }
    }

    private bool _running;

    public void Dispose()
    {
        SemaphoreSlim.Dispose();
    }
}

/// <summary>
/// 自己当前设备的主机信息提供者
/// </summary>
public interface ISelfHostProvider
{
    Task<string> GetSelfHostAsync();
}

/// <summary>
/// 获取主机信息结果
/// </summary>
/// <param name="SelfIsMaster">当前是否就是主设备</param>
/// <param name="MasterHost">主设备地址</param>
public record MasterHostResult(bool SelfIsMaster, string MasterHost);

/// <summary>
/// 假的设备信息提供者
/// </summary>
public class FakeSelfHostProvider : ISelfHostProvider
{
    public FakeSelfHostProvider()
    {
        _hostName = "Fake-" + Random.Shared.Next().ToString();
    }

    private readonly string _hostName;

    public Task<string> GetSelfHostAsync()
    {
        return Task.FromResult(_hostName);
    }
}

public static class WatchDogStartup
{
    public static WebApplicationBuilder AddWatchDog(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        services.AddSingleton<ISelfHostProvider, FakeSelfHostProvider>();
        services.AddSingleton<IMasterHostProvider, RedisMasterHostProvider>();
        services.AddSingleton<IMasterHostStatusChecker, MasterHostStatusChecker>();

        services.AddSingleton<IDogInfoProvider, DogInfoProvider>();
        services.AddSingleton<ITimeProvider, TimeProvider>();
        services.AddSingleton<WatchDogProvider>();
        return builder;
    }

    public static WebApplication UseWatchDog(this WebApplication webApplication)
    {
        //webApplication.UseMiddleware<MasterHostMiddleware>();
        return webApplication;
    }
}

class RedisLocker
{
    public RedisLocker(IRedisClient redisClient, string lockKey, string lockValue)
    {
        _redisClient = redisClient;
        _lockKey = lockKey;
        _lockValue = lockValue;
    }

    private readonly IRedisClient _redisClient;
    private readonly string _lockKey;
    private readonly string _lockValue;

    public async Task DoInLockAsync(Func<Task> task)
    {
        var redisKey = new RedisKey(_lockKey);
        var redisValue = new RedisValue(_lockValue);

        while (true)
        {
            var success = await _redisClient.Db0.Database.SetAddAsync(redisKey, redisValue);
            if (success)
            {
                break;
            }

            await Task.Delay(500);
        }

        try
        {
            await task();
        }
        finally
        {
            await _redisClient.Db0.Database.SetRemoveAsync(redisKey, redisValue);
        }
    }
}

class RedisDogInfoProvider : IDogInfoProvider
{
    public RedisDogInfoProvider(IRedisClient redisClient)
    {
        _redisClient = redisClient;
    }

    private readonly IRedisClient _redisClient;

    public void SetMute(MuteInfo muteInfo)
    {

    }

    public void RemoveMuteByActive(string id)
    {
    }

    public bool ShouldMute(LastFeedDogInfo info, string dogId)
    {
        return false;
    }
}
