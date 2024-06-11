namespace UsingHardLinkToZipNtfsDiskSize;

public static class UnitConverter
{
    /// <summary>
    /// 将文件大小转换为用于显示的大小
    /// </summary>
    /// <param name="value">文件大小，单位：B</param>
    /// <param name="isRound">是否四舍五入</param>
    /// <param name="separators">数字和单位间的分隔符</param>
    /// <returns>用于显示的字符串，只保留GB和TB的小数</returns>
    public static string ConvertSize(long value, bool isRound = true, string separators = "")
    {
        var (result, unit) = ConvertInner(value, 1024, 1, isRound, separators, DefaultSizeUnits);

        //只保留GB和TB的小数
        if (unit == "GB" || unit == "TB")
        {
            return $"{result}{unit}";
        }
        else
        {
            // 其他，例如 MB 单位，不保留小数点。用 int 的强转去掉小数点，不能用 `ToString("0")` 的方式，因为此方式默认调用四舍五入的方式
            return $"{(int) result}{unit}";
        }
    }

    private static readonly string[] DefaultSizeUnits = new string[] { "B", "KB", "MB", "GB", "TB" };

    /// <summary>
    /// 将数值转换为用于显示的数值
    /// </summary>
    /// <param name="value">需要转换的值</param>
    /// <param name="interval">进制</param>
    /// <param name="digits">小数位数</param>
    /// <param name="isRound">是否四舍五入</param>
    /// <param name="separators">数字和单位间的分隔符</param>
    /// <param name="units">单位</param>
    /// <returns>
    /// - result 数值结果
    /// <para></para>
    /// - unit 单位
    /// </returns>
    private static (double result, string unit) ConvertInner(long value, long interval, int digits, bool isRound,
        string separators, string[] units)
    {
        var current = 0;
        var temp = value;

        while (current < units.Length - 1 && temp >= interval)
        {
            current++;
            temp /= interval;
        }
        var result = value * 1.0 / Math.Pow(interval, current);

        if (!isRound)
        {
            var pow = Math.Pow(10, digits);
            return (Math.Truncate(result * pow) / pow, units[current]);
        }

        if (string.IsNullOrEmpty(separators))
        {
            return (Math.Round(result, digits), units[current]);
        }

        return (Math.Round(result, digits), $"{separators}{units[current]}");
    }
}