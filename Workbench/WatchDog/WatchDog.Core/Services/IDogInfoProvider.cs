using WatchDog.Core.Context;

namespace WatchDog.Core.Services;

public interface IDogInfoProvider
{
    void SetMute(MuteInfo muteInfo);
    void RemoveMuteByActive(string id);
    bool ShouldMute(LastFeedDogInfo info,string dogId);
}