namespace WatchDog.Core.Context;

/// <summary>
/// 检查是否需要汪的结果
/// </summary>
/// <param name="IsOk">是否正常</param>
/// <param name="ShouldWang">是否需要汪</param>
/// <param name="ShouldMute">是否被静音</param>
/// <param name="InNotifyInterval">是否在通知间隔内</param>
/// <param name="OverNotifyMaxCount">是否超过最大通知数量</param>
public record CheckShouldWangResult(bool IsOk, bool ShouldWang, bool ShouldMute, bool InNotifyInterval, bool OverNotifyMaxCount);