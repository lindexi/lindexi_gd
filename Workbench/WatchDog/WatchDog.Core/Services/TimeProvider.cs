namespace WatchDog.Core;

public class TimeProvider : ITimeProvider
{
    public DateTimeOffset GetCurrentTime()
    {
        return DateTimeOffset.Now;
    }
}