namespace WatchDog.Core.Context;

/// <summary>
/// 喂狗的结果，返回的是喂狗的信息
/// </summary>
/// <param name="Id"></param>
/// <param name="DelaySecond"></param>
/// <param name="MaxDelayCount"></param>
/// <param name="NotifyIntervalSecond"></param>
/// <param name="NotifyMaxCount"></param>
/// <param name="RegisterTime">注册时间</param>
/// <param name="IsRegister">这一条是否输出注册的，首次喂狗</param>
public record FeedDogResult(string Id, uint DelaySecond, uint MaxDelayCount, uint NotifyIntervalSecond, int NotifyMaxCount, DateTimeOffset RegisterTime, bool IsRegister);