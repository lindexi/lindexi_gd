namespace WatchDog.Core.Context;

public record WangInfo(string Id, string Name, string LastStatus, uint DelaySecond, DateTimeOffset LastUpdateTime, DateTimeOffset RegisterTime);