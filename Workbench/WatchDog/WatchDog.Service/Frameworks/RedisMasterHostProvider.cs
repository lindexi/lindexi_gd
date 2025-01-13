using StackExchange.Redis.Extensions.Core.Abstractions;
using WatchDog.Service.Contexts;

namespace WatchDog.Service.Frameworks;

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