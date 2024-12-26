using WatchDog.Core.Context;

namespace WatchDog.Core.Services;

public class DogInfoProvider
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

    public void RemoveMuteByActive(string id)
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
    }

    public bool ShouldMute(LastFeedDogInfo info,string dogId)
    {
        if (MuteSet.TryGetValue(info.Id,out var muteDictionary))
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

    private Dictionary<string /*id*/, Dictionary<string /*dogId*/, MuteFeedInfo>> MuteSet { get; } = new();
}