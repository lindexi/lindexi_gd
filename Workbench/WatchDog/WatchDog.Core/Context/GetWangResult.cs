namespace WatchDog.Core;

/// <summary>
/// 获取 Wang 咬人的结果
/// </summary>
/// <param name="DogId"></param>
public record GetWangResult(string DogId, List<WangInfo> WangList);