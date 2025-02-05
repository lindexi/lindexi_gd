using WatchDog.Core.Context;

namespace WatchDog.Core.Services;

public class DogInfoProvider : IDogInfoProvider
{
    public DogInfoProvider(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    private readonly ITimeProvider _timeProvider;

    public const string MuteAllKey = "MuteAll_F4695EE6-5774-43A9-8427-FEE3D860D648";

    public void SetMute(MuteInfo muteInfo)
    {
        if (!MuteSet.TryGetValue(muteInfo.Id, out var value))
        {
            value = new Dictionary<string, MuteFeedInfo>();
            MuteSet[muteInfo.Id] = value;
        }

        var currentTime = _timeProvider.GetCurrentTime();
        if (muteInfo.MuteAll)
        {
            value[MuteAllKey] = new MuteFeedInfo(muteInfo, currentTime);
        }
        else
        {
            value[muteInfo.DogId] = new MuteFeedInfo(muteInfo, currentTime);
        }
    }

    public void SetActive(string id)
    {
        if (MuteSet.TryGetValue(id, out var muteDictionary))
        {
            var removeList = new List<string>();
            foreach (var (key, muteInfo) in muteDictionary)
            {
                if (muteInfo.MuteInfo.ActiveInNextFeed)
                {
                    removeList.Add(key);
                }
            }

            foreach (var key in removeList)
            {
                muteDictionary.Remove(key);
            }
        }

        CleanWangList(new[] { id });
    }

    public void CleanWangList(IReadOnlyList<string> idList)
    {
        var removeList = new List<string>();
        foreach (var (dogId, lastWangInfoSet) in _wangSet)
        {
            foreach (var id in idList)
            {
                lastWangInfoSet.Remove(id);
            }

            if (lastWangInfoSet.Count == 0)
            {
                removeList.Add(dogId);
            }
        }

        foreach (var dogId in removeList)
        {
            _wangSet.Remove(dogId);
        }
    }

    public CheckShouldWangResult RegisterAndCheckShouldWang(LastFeedDogInfo info, string dogId)
    {
        if (ShouldMute(info, dogId))
        {
            // 需要静音
            return new(IsOk: false, ShouldWang: false, ShouldMute: true, InNotifyInterval: false,
                OverNotifyMaxCount: false);
        }

        if (!_wangSet.TryGetValue(dogId, out var lastWangInfoDictionary))
        {
            lastWangInfoDictionary = new Dictionary<string, LastWangInfo>();
            _wangSet[dogId] = lastWangInfoDictionary;
        }

        var currentTime = _timeProvider.GetCurrentTime();
        if (lastWangInfoDictionary.TryGetValue(info.Id, out var lastWangInfo))
        {
            var timeSpan = currentTime - lastWangInfo.WangTime;
            if (timeSpan.TotalSeconds < info.FeedDogInfo.NotifyIntervalSecond)
            {
                // 在通知的间隔时间内，不需要再次通知
                return new CheckShouldWangResult(IsOk: false, ShouldWang: false, ShouldMute: false,
                    InNotifyInterval: true, OverNotifyMaxCount: false);
            }

            if (info.FeedDogInfo.NotifyMaxCount != -1/*如果是 -1 就是不限制通知次数*/  && lastWangInfo.WangCount > info.FeedDogInfo.NotifyMaxCount)
            {
                // 超过了最大通知次数
                return new CheckShouldWangResult(IsOk: false, ShouldWang: false, ShouldMute: false,
                    InNotifyInterval: false, OverNotifyMaxCount: true);
            }
        }

        var wangCount = lastWangInfo?.WangCount ?? 0;
        lastWangInfoDictionary[info.Id] = new LastWangInfo(currentTime, wangCount + 1);

        return new CheckShouldWangResult(IsOk: false, ShouldWang: true, ShouldMute: false, InNotifyInterval: false,
            OverNotifyMaxCount: false);
    }

    private readonly Dictionary<string /*DogId*/, Dictionary<string/*Id*/, LastWangInfo>> _wangSet = new();

    record LastWangInfo(DateTimeOffset WangTime, int WangCount);

    private bool ShouldMute(LastFeedDogInfo info, string dogId)
    {
        if (MuteSet.TryGetValue(info.Id, out var muteDictionary))
        {
            if (muteDictionary.TryGetValue(MuteAllKey, out var muteAllInfo))
            {
                return ShouldSilent(muteAllInfo);
            }

            if (muteDictionary.TryGetValue(dogId, out var muteInfo))
            {
                return ShouldSilent(muteInfo);
            }

            bool ShouldSilent(MuteFeedInfo muteFeedInfo)
            {
                var currentTime = _timeProvider.GetCurrentTime();
                if ((currentTime - muteFeedInfo.LastMuteTime).TotalSeconds > muteFeedInfo.MuteInfo.SilentSecond)
                {
                    return false;
                }
                return true;
            }
        }

        return false;
    }

    private Dictionary<string /*Id*/, Dictionary<string /*DogId*/, MuteFeedInfo>> MuteSet { get; } = new();
}