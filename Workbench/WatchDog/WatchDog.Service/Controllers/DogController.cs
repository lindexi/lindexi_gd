using Microsoft.AspNetCore.Mvc;

using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

using WatchDog.Core.Context;
using WatchDog.Core.Services;

namespace WatchDog.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class DogController : ControllerBase
{
    private readonly ILogger<DogController> _logger;

    public DogController(ILogger<DogController> logger, IRedisClient redisClient)
    {
        _logger = logger;
    }
}

public interface IMasterHostProvider
{
    Task<MasterHostResult> GetMasterHostAsync();
}

public class RedisMasterHostProvider : IMasterHostProvider
{
    public RedisMasterHostProvider(IRedisClient redisClient, ISelfHostProvider selfHostProvider)
    {
        _redisClient = redisClient;
        _selfHostProvider = selfHostProvider;
    }

    private readonly IRedisClient _redisClient;

    private readonly ISelfHostProvider _selfHostProvider;

    public async Task<MasterHostResult> GetMasterHostAsync()
    {
        var selfHost = await _selfHostProvider.GetSelfHostAsync();
        var lockKey = "WatchDog-MasterLock-4ACF7B3F-222D-469C-B99D-5E3966FFB422";
        // 先获取一遍，发现存在了，则尝试访问一下。如果能够通过，证明主设备存在
        // 如果发现存在的就是自己，那自己就是主设备

        var redisKey = new RedisKey(lockKey);
        var redisValue = new RedisValue(selfHost);

        for (int i = 0; i < 1000; i++)
        {
            var masterHost = await _redisClient.Db0.GetAsync<string>(lockKey);
            if (masterHost == selfHost)
            {
                // 可能就是自己就是主设备了
                // 先等等再获取，看对不对
                await Task.Delay(300);

                masterHost = await _redisClient.Db0.GetAsync<string>(lockKey);
                if (masterHost == selfHost)
                {
                    // 确定自己就是主设备
                    return new MasterHostResult(true, selfHost);
                }
                else
                {
                    // 别人是主设备
                }
            }
            //var success = await _redisClient.Db0.Database.SetAddAsync(redisKey, redisValue);
        }

        throw new InvalidOperationException();
    }
}

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

public class FakeSelfHostProvider : ISelfHostProvider
{
    public FakeSelfHostProvider()
    {
        _hostName = Random.Shared.Next().ToString();
    }

    private readonly string _hostName;

    public Task<string> GetSelfHostAsync()
    {
        return Task.FromResult(_hostName);
    }
}

public static class WatchDogStartup
{
    public static void AddWatchDog(this IServiceCollection services)
    {
        services.AddSingleton<ISelfHostProvider, FakeSelfHostProvider>();
        services.AddSingleton<IMasterHostProvider, RedisMasterHostProvider>();
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
