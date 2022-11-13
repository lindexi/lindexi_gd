namespace PackageManager.Server.Utils;

public class StringVersionComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        // 大于 返回1
        // 等于 返回0
        // 小于 返回负数
        if (x is null && y is null) return 0;
        if (x is null && y is not null) return -1;
        if (x is not null && y is null) return 1;

        var xSuccess = Version.TryParse(x, out var versionX);
        var ySuccess = Version.TryParse(y, out var versionY);

        if (!xSuccess && !ySuccess) return 0;
        if (!xSuccess && ySuccess) return -1;
        if (xSuccess && !ySuccess) return 1;

        if (versionX is not null && versionY is not null)
        {
            return versionX.CompareTo(versionY);
        }
        else
        {
            // 理论上不会进入这个逻辑
            return 0;
        }
    }
}