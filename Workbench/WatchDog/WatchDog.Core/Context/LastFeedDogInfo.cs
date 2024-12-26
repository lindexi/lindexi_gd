namespace WatchDog.Core.Context;

public record LastFeedDogInfo(string Id, DateTimeOffset RegisterTime, DateTimeOffset LastUpdateTime, FeedDogInfo FeedDogInfo);