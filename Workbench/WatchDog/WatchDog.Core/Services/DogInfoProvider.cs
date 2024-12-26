using WatchDog.Core.Context;

namespace WatchDog.Core.Services;

public class DogInfoProvider
{
    public const string MuteAllKey = "MuteAll_F4695EE6-5774-43A9-8427-FEE3D860D648";

    public void SetMute(MuteInfo muteInfo)
    {
        if (!MuteSet.TryGetValue(muteInfo.Id, out var value))
        {
            value = new Dictionary<string, MuteInfo>();
            MuteSet[muteInfo.Id] = value;
        }

        if (muteInfo.MuteAll)
        {
            value[MuteAllKey] = muteInfo;
        }
        else
        {
            value[muteInfo.DogId] = muteInfo;
        }
    }

    public void RemoveMuteByActive(string id)
    {
        if (MuteSet.TryGetValue(id, out var muteDictionary))
        {
            var removeList = new List<string>();
            foreach (var (key, muteInfo) in muteDictionary)
            {
                if (muteInfo.ActiveInNextFeed)
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
                //muteAllInfo.SilentSecond
                return true;
            }

            if (muteDictionary.TryGetValue(dogId, out var muteInfo))
            {
                //muteInfo.SilentSecond
                return true;
            }
        }

        return false;
    }

    private Dictionary<string /*id*/, Dictionary<string /*dogId*/, MuteInfo>> MuteSet { get; } = new();
}