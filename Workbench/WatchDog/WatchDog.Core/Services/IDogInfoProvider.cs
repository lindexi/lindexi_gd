using WatchDog.Core.Context;

namespace WatchDog.Core.Services;

public interface IDogInfoProvider
{
    void SetMute(MuteInfo muteInfo);
    void SetActive(string id);
    void CleanWangList(IReadOnlyList<string> idList);

    /// <summary>
    /// 注册并检查是否需要汪
    /// </summary>
    /// <param name="info"></param>
    /// <param name="dogId"></param>
    /// <returns></returns>
    CheckShouldWangResult RegisterAndCheckShouldWang(LastFeedDogInfo info, string dogId);
}