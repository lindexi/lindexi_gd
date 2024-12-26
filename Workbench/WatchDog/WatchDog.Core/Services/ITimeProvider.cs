namespace WatchDog.Core.Services;

public interface ITimeProvider
{
    DateTimeOffset GetCurrentTime();
}