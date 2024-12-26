namespace WatchDog.Core;

/// <summary>
/// 禁言信息
/// </summary>
/// <param name="Id">禁掉哪一条</param>
/// <param name="DogId">属于哪只狗的</param>
/// <param name="ShouldRemove">是否应该移除掉记录。如果设置为 true 则无视 <paramref name="SilentSecond"/> 等选项，对所有狗生效，此属性为 true 时，无视后续其他属性</param>
/// <param name="SilentSecond">静默的时间，默认静默一个小时</param>
/// <param name="ActiveInNextFeed">自动下次喂狗时激活，默认是 true 用于服务自动恢复。为 false 则永久移除，无视服务状态</param>
/// <param name="MuteAll">对这一条禁用所有的狗，默认 false 只对当前狗生效</param>
public record MuteInfo
(
    string Id,
    string DogId,
    bool ShouldRemove = false,
    uint SilentSecond = 3600,
    bool ActiveInNextFeed = true,
    bool MuteAll = false
);