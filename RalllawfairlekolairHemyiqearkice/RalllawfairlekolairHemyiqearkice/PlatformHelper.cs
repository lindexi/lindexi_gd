namespace RalllawfairlekolairHemyiqearkice;

public static class PlatformHelper
{
    public static IPlatformProvider? PlatformProvider
    {
        get => _platformProvider;
        set
        {
            if (_platformProvider != null)
            {
                throw new InvalidOperationException("PlatformProvider can only be set once");
            }

            _platformProvider = value;
        }
    }

    private static IPlatformProvider? _platformProvider;
}
