using System.Globalization;
using System.Text.RegularExpressions;

namespace MediaFoundation.Core;

internal static class SubtitleParsing
{
    private static readonly Regex Markup = new(@"</?(?:b|i|u|c|v|lang|ruby|rt)(?:[.\s][^<>]*)?>|<\d{2}:\d{2}(?::\d{2})?\.\d{3}>", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    internal static string[] Lines(string text) => text.TrimStart('\uFEFF').ReplaceLineEndings("\n").Split('\n');
    internal static string PlainText(IEnumerable<string> lines) => System.Net.WebUtility.HtmlDecode(Markup.Replace(string.Join('\n', lines), ""));
    internal static FormatException Error(SubtitleFormat format, int line, string reason) =>
        new(SR.Current.SubtitleParse.ToString(format, line, reason switch
        {
            "SubtitleHeader" => SR.Current.SubtitleHeader,
            "SubtitleLayout" => SR.Current.SubtitleLayout,
            "SubtitleTiming" => SR.Current.SubtitleTiming,
            "SubtitleText" => SR.Current.SubtitleText,
            "SubtitleEmpty" => SR.Current.SubtitleEmpty,
            _ => throw new ArgumentOutOfRangeException(nameof(reason))
        }));

    internal static bool TryTime(string text, char separator, bool allowMinutesOnly, out TimeSpan time)
    {
        time = default;
        string[] parts = text.Split(':');
        if (parts.Length != 3 && !(allowMinutesOnly && parts.Length == 2)) return false;
        int minuteIndex = parts.Length - 2;
        if (parts[minuteIndex].Length != 2 || parts[^1].Length != 6 || parts[^1][2] != separator) return false;
        if (!int.TryParse(parts[minuteIndex], NumberStyles.None, CultureInfo.InvariantCulture, out int minutes) || minutes > 59 ||
            !int.TryParse(parts[^1].AsSpan(0, 2), NumberStyles.None, CultureInfo.InvariantCulture, out int seconds) || seconds > 59 ||
            !int.TryParse(parts[^1].AsSpan(3), NumberStyles.None, CultureInfo.InvariantCulture, out int milliseconds)) return false;
        int hours = 0;
        if (parts.Length == 3 && (parts[0].Length < 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out hours))) return false;
        long totalMilliseconds = (long)hours * 3600000 + minutes * 60000L + seconds * 1000L + milliseconds;
        if (totalMilliseconds > TimeSpan.MaxValue.Ticks / TimeSpan.TicksPerMillisecond) return false;
        time = TimeSpan.FromTicks(totalMilliseconds * TimeSpan.TicksPerMillisecond);
        return true;
    }
}
