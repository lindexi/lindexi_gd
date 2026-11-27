using System.Globalization;
using System.Text.RegularExpressions;

namespace MediaFoundation.Core;

internal static class LrcParser
{
    private static readonly Regex WordTiming = new(@"<[0-9]+:[^<>]*>", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    internal static SubtitleDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string[] lines = SubtitleParsing.Lines(text);
        var entries = new List<(long Ticks, string Text, int Line)>();
        long offset = 0;
        for (int index = 0; index < lines.Length; index++)
        {
            string remaining = lines[index].Trim();
            if (remaining.Length == 0) continue;
            int line = index + 1;
            if (WordTiming.IsMatch(remaining)) throw Error(line, "Enhanced");
            var times = new List<long>();
            bool metadata = false;
            while (remaining.StartsWith('['))
            {
                int close = remaining.IndexOf(']');
                if (close < 0) throw Error(line, "Timing");
                string tag = remaining[1..close];
                int colon = tag.IndexOf(':');
                if (colon > 0 && tag[..colon].All(char.IsAsciiLetter))
                {
                    if (times.Count != 0) throw Error(line, "Timing");
                    metadata = true;
                    if (tag[..colon].Equals("offset", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!long.TryParse(tag.AsSpan(colon + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long milliseconds) ||
                            milliseconds < long.MinValue / TimeSpan.TicksPerMillisecond || milliseconds > long.MaxValue / TimeSpan.TicksPerMillisecond)
                            throw Error(line, "Offset");
                        offset = milliseconds * TimeSpan.TicksPerMillisecond;
                    }
                }
                else
                {
                    if (metadata || !TryTime(tag, out long ticks)) throw Error(line, "Timing");
                    times.Add(ticks);
                }
                remaining = remaining[(close + 1)..].TrimStart();
            }
            if (metadata)
            {
                if (remaining.Length != 0) throw Error(line, "Timing");
                continue;
            }
            if (times.Count == 0) throw Error(line, "Timing");
            foreach (long ticks in times) entries.Add((ticks, remaining, line));
        }

        // Offset is global, including declarations after lyrics; positive values advance playback.
        for (int index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            decimal adjusted = (decimal)entry.Ticks - offset;
            if (adjusted >= TimeSpan.MaxValue.Ticks) throw Error(entry.Line, "Range");
            entries[index] = ((long)Math.Max(0m, adjusted), entry.Text, entry.Line);
        }
        var groups = entries.OrderBy(entry => entry.Ticks).GroupBy(entry => entry.Ticks).ToArray();
        var cues = new List<SubtitleCue>();
        for (int index = 0; index < groups.Length; index++)
        {
            string content = string.Join('\n', groups[index].Where(entry => !string.IsNullOrWhiteSpace(entry.Text)).Select(entry => entry.Text));
            if (content.Length == 0) continue;
            var start = TimeSpan.FromTicks(groups[index].Key);
            var end = index + 1 < groups.Length ? TimeSpan.FromTicks(groups[index + 1].Key) : TimeSpan.MaxValue;
            cues.Add(new SubtitleCue(start, end, content));
        }
        if (cues.Count == 0) throw Error(1, "Empty");
        return new SubtitleDocument(cues);
    }

    private static bool TryTime(string text, out long ticks)
    {
        ticks = 0;
        int colon = text.IndexOf(':');
        if (colon < 1 || !long.TryParse(text.AsSpan(0, colon), NumberStyles.None, CultureInfo.InvariantCulture, out long minutes) ||
            minutes > TimeSpan.MaxValue.Ticks / TimeSpan.TicksPerMinute) return false;
        ReadOnlySpan<char> seconds = text.AsSpan(colon + 1);
        if (seconds.Length != 2 && seconds.Length != 5 && seconds.Length != 6) return false;
        if (!int.TryParse(seconds[..2], NumberStyles.None, CultureInfo.InvariantCulture, out int wholeSeconds) || wholeSeconds > 59) return false;
        int milliseconds = 0;
        if (seconds.Length > 2)
        {
            if (seconds[2] != '.' || !int.TryParse(seconds[3..], NumberStyles.None, CultureInfo.InvariantCulture, out milliseconds)) return false;
            if (seconds.Length == 5) milliseconds *= 10;
        }
        decimal total = ((decimal)minutes * 60000 + wholeSeconds * 1000 + milliseconds) * TimeSpan.TicksPerMillisecond;
        if (total >= TimeSpan.MaxValue.Ticks) return false;
        ticks = (long)total;
        return true;
    }

    private static FormatException Error(int line, string reason) => new(SR.Current.LrcParse.ToString(line, reason switch
    {
        "Timing" => SR.Current.LrcTiming,
        "Offset" => SR.Current.LrcOffset,
        "Enhanced" => SR.Current.LrcEnhanced,
        "Range" => SR.Current.LrcRange,
        "Empty" => SR.Current.LrcEmpty,
        _ => throw new ArgumentOutOfRangeException(nameof(reason))
    }));
}
