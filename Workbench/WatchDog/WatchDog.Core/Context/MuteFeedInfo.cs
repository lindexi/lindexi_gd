namespace WatchDog.Core.Context;

public record MuteFeedInfo(MuteInfo MuteInfo, DateTimeOffset LastMuteTime);