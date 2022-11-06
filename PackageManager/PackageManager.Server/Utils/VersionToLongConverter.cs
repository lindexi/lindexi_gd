namespace PackageManager.Server.Utils;

public static class VersionToLongConverter
{
    public static long VersionToLong(this Version version)
    {
        long value = ((long)version.Major) << (16 * 3);
        value += ((long)version.Minor) << (16 * 2);
        value += version.Build << 16;
        value += version.Revision;
        return value;
    }
}
