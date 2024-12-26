namespace WatchDog.Core;

public class WatchDogProvider
{
    private readonly DogInfoProvider _dogInfoProvider = new DogInfoProvider();
    private ITimeProvider _timeProvider = new TimeProvider();

    public FeedDogResult Feed(FeedDogInfo feedDogInfo)
    {
        var id = feedDogInfo.Id ?? Guid.NewGuid().ToString("N");

        bool isRegister = false;
        if (LastFeedDogInfoDictionary.TryGetValue(id, out var lastFeedDogInfo))
        {
            lastFeedDogInfo = lastFeedDogInfo with
            {
                FeedDogInfo = feedDogInfo,
                LastUpdateTime = _timeProvider.GetCurrentTime(),
            };
        }
        else
        {
            // 新注册
            isRegister = true;
            var currentTime = _timeProvider.GetCurrentTime();
            lastFeedDogInfo = new LastFeedDogInfo(id, currentTime, currentTime, feedDogInfo);
        }
        LastFeedDogInfoDictionary[id] = lastFeedDogInfo;

        _dogInfoProvider.RemoveMuteByActive(id);

        return new FeedDogResult(id, feedDogInfo.DelaySecond, feedDogInfo.MaxDelayCount, feedDogInfo.NotifyIntervalSecond, feedDogInfo.NotifyMaxCount, lastFeedDogInfo.RegisterTime, isRegister);
    }

    private Dictionary<string /*Id*/, LastFeedDogInfo> LastFeedDogInfoDictionary { get; } = new();

    public GetWangResult GetWang(GetWangInfo wangInfo)
    {
        DateTimeOffset currentTime = _timeProvider.GetCurrentTime();

        var wangList = new List<WangInfo>();

        foreach (var lastFeedDogInfo in LastFeedDogInfoDictionary.Values)
        {
            var timeSpan = currentTime - lastFeedDogInfo.LastUpdateTime;
            if (timeSpan.TotalSeconds > lastFeedDogInfo.FeedDogInfo.DelaySecond)
            {
                // 咬人
                if (_dogInfoProvider.ShouldMute(lastFeedDogInfo,wangInfo.DogId))
                {
                    continue;
                }

                wangList.Add(new WangInfo(lastFeedDogInfo.Id, lastFeedDogInfo.FeedDogInfo.Name, lastFeedDogInfo.FeedDogInfo.Status, lastFeedDogInfo.FeedDogInfo.DelaySecond, lastFeedDogInfo.LastUpdateTime, lastFeedDogInfo.RegisterTime));
            }
        }

        return new GetWangResult(wangInfo.DogId, wangList);
    }

    public void Mute(MuteInfo muteInfo)
    {
        if (muteInfo.ShouldRemove)
        {
            LastFeedDogInfoDictionary.Remove(muteInfo.Id);
            return;
        }

        _dogInfoProvider.SetMute(muteInfo);
    }
}