namespace WatchDog.Core.Context;

/// <summary>
/// 喂狗信息
/// </summary>
/// <param name="Name">名称</param>
/// <param name="Status">状态</param>
/// <param name="Id">Id号，如为空则自动分配</param>
/// <param name="DelaySecond">喂狗允许的延迟时间，超过时间就被狗咬</param>
/// <param name="MaxDelayCount">最多的次数，一般是 1 的值</param>
/// <param name="NotifyIntervalSecond">通知的间隔时间</param>
/// <param name="NotifyMaxCount">最多的通知次数，默认是无限通知</param>
/// <param name="MaxCleanTimeSecond">如果经过了多久都没有响应，则清除喂狗信息。默认是 7 天</param>
public record FeedDogInfo
(
    string Name,
    string Status,
    string? Id = null,
    uint DelaySecond = 60,
    uint MaxDelayCount = 1,
    uint NotifyIntervalSecond = 60 * 30,
    int NotifyMaxCount = -1,
    int MaxCleanTimeSecond = 60 * 60 * 24 * 7
);