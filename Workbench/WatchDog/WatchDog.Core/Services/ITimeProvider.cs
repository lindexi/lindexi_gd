namespace WatchDog.Core;

public interface ITimeProvider
{
    DateTimeOffset GetCurrentTime();
}