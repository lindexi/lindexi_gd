//using StackExchange.Redis.Extensions.Core.Abstractions;
//using WatchDog.Core.Context;
//using WatchDog.Core.Services;

//namespace WatchDog.Service.Frameworks;

//class RedisDogInfoProvider : IDogInfoProvider
//{
//    public RedisDogInfoProvider(IRedisClient redisClient)
//    {
//        _redisClient = redisClient;
//    }

//    private readonly IRedisClient _redisClient;

//    public void SetMute(MuteInfo muteInfo)
//    {

//    }

//    public void RemoveMuteByActive(string id)
//    {
//    }

//    public bool ShouldMute(LastFeedDogInfo info, string dogId)
//    {
//        return false;
//    }
//}