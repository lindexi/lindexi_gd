//using StackExchange.Redis;
//using StackExchange.Redis.Extensions.Core.Abstractions;

//namespace WatchDog.Service.Frameworks;

//class RedisLocker
//{
//    public RedisLocker(IRedisClient redisClient, string lockKey, string lockValue)
//    {
//        _redisClient = redisClient;
//        _lockKey = lockKey;
//        _lockValue = lockValue;
//    }

//    private readonly IRedisClient _redisClient;
//    private readonly string _lockKey;
//    private readonly string _lockValue;

//    public async Task DoInLockAsync(Func<Task> task)
//    {
//        var redisKey = new RedisKey(_lockKey);
//        var redisValue = new RedisValue(_lockValue);

//        while (true)
//        {
//            var success = await _redisClient.Db0.Database.SetAddAsync(redisKey, redisValue);
//            if (success)
//            {
//                break;
//            }

//            await Task.Delay(500);
//        }

//        try
//        {
//            await task();
//        }
//        finally
//        {
//            await _redisClient.Db0.Database.SetRemoveAsync(redisKey, redisValue);
//        }
//    }
//}