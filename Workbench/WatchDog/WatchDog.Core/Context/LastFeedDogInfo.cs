namespace WatchDog.Core;

public record LastFeedDogInfo(string Id, DateTimeOffset RegisterTime, DateTimeOffset LastUpdateTime, FeedDogInfo FeedDogInfo);