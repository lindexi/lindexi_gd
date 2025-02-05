using WatchDog.Core.Context;
using WatchDog.Core.Services;

namespace WatchDog.Core;

public class WatchDogProvider
{
    public WatchDogProvider(IDogInfoProvider? dogInfoProvider = null, ITimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? new TimeProvider();
        _dogInfoProvider = dogInfoProvider ?? new DogInfoProvider(_timeProvider);
    }

    private readonly IDogInfoProvider _dogInfoProvider;
    private readonly ITimeProvider _timeProvider;

    public FeedDogResult Feed(FeedDogInfo feedDogInfo)
    {
        var id = feedDogInfo.Id;
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString("N");
        }

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

        _dogInfoProvider.SetActive(id);

        return new FeedDogResult(id, feedDogInfo.DelaySecond, feedDogInfo.MaxDelayCount, feedDogInfo.NotifyIntervalSecond, feedDogInfo.NotifyMaxCount, lastFeedDogInfo.RegisterTime, isRegister);
    }

    private Dictionary<string /*Id*/, LastFeedDogInfo> LastFeedDogInfoDictionary { get; } = new();

    public GetWangResult GetWang(GetWangInfo wangInfo)
    {
        DateTimeOffset currentTime = _timeProvider.GetCurrentTime();

        var wangList = new List<WangInfo>();

        var cleanIdList = new List<string>();

        foreach (var lastFeedDogInfo in LastFeedDogInfoDictionary.Values)
        {
            var timeSpan = currentTime - lastFeedDogInfo.LastUpdateTime;
            CheckShouldWangResult checkShouldWangResult;
            if (timeSpan.TotalSeconds > lastFeedDogInfo.FeedDogInfo.DelaySecond)
            {
                if (timeSpan.TotalSeconds > lastFeedDogInfo.FeedDogInfo.MaxCleanTimeSecond)
                {
                    // 如果狗忘记这个了，那就加入清理列表
                    cleanIdList.Add(lastFeedDogInfo.Id);
                    continue;
                }

                // 咬人
                checkShouldWangResult = _dogInfoProvider.RegisterAndCheckShouldWang(lastFeedDogInfo, wangInfo.DogId);
            }
            else
            {
                checkShouldWangResult = new CheckShouldWangResult(ShouldWang: false, ShouldMute: false, InNotifyInterval: false, OverNotifyMaxCount: false);
            }

            wangList.Add(new WangInfo(lastFeedDogInfo, checkShouldWangResult));
        }

        foreach (var id in cleanIdList)
        {
            LastFeedDogInfoDictionary.Remove(id);
        }

        _dogInfoProvider.CleanWangList(cleanIdList);

        return new GetWangResult(wangInfo.DogId, wangList);
    }

    public void Sync(GetWangResult result)
    {
        LastFeedDogInfoDictionary.Clear();
        foreach (var wangInfo in result.WangList)
        {
            var id = wangInfo.FeedDogInfo.Id;
            LastFeedDogInfoDictionary[id] = wangInfo.FeedDogInfo;
        }
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