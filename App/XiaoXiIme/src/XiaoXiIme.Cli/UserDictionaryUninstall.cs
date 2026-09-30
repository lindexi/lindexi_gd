using XiaoXiIme.Dictionary;

namespace XiaoXiIme.Cli;

internal static class UserDictionaryUninstall
{
    internal static string Complete(bool purgeUserData, string? userDictionaryPath = null)
    {
        var path = Path.GetFullPath(userDictionaryPath ?? UserDictionaryLocations.GetDefaultFilePath());
        if (!purgeUserData)
        {
            return $"User dictionary retained: {path}";
        }

        UserDictionaryLocations.Purge(path);
        return $"User dictionary purged: {path}";
    }
}
