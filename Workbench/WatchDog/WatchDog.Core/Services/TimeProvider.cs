namespace WatchDog.Core.Services;

public class TimeProvider : ITimeProvider
{
    public DateTimeOffset GetCurrentTime()
    {
        return DateTimeOffset.Now;
    }
}