using System.Globalization;

namespace MediaFoundation.Core;

internal static class Mpl2Parser
{
    private const long TicksPerDecisecond = TimeSpan.TicksPerSecond / 10;

    internal static SubtitleDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string[] lines = SubtitleParsing.Lines(text);
        var cues = new List<SubtitleCue>();
        for (int index = 0; index < lines.Length; index++)
        {
            string content = lines[index].TrimStart();
            if (content.Length == 0) continue;
            int line = index + 1;
            TimeSpan start = ReadTime(ref content, line);
            TimeSpan end = ReadTime(ref content, line);
            if (end <= start) throw Error(line, "Timing");
            string[] body = content.Split('|');
            foreach (string part in body)
            {
                // A leading slash is MPL2 italics, not literal text in this position.
                if (part.TrimStart().StartsWith('/')) throw Error(line, "Italic");
            }
            string plainText = string.Join('\n', body);
            if (string.IsNullOrWhiteSpace(plainText)) throw Error(line, "Text");
            cues.Add(new SubtitleCue(start, end, plainText));
        }
        if (cues.Count == 0) throw Error(1, "Empty");
        return new SubtitleDocument(cues);
    }

    private static TimeSpan ReadTime(ref string content, int line)
    {
        if (!content.StartsWith('[')) throw Error(line, "Timing");
        int close = content.IndexOf(']');
        if (close < 2) throw Error(line, "Timing");
        ReadOnlySpan<char> digits = content.AsSpan(1, close - 1);
        foreach (char digit in digits)
        {
            if (digit < '0' || digit > '9') throw Error(line, "Timing");
        }
        if (!long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out long value) ||
            value > TimeSpan.MaxValue.Ticks / TicksPerDecisecond)
            throw Error(line, "Range");
        content = content[(close + 1)..];
        return TimeSpan.FromTicks(value * TicksPerDecisecond);
    }

    private static FormatException Error(int line, string reason) => new(SR.Current.Mpl2Parse.ToString(line, reason switch
    {
        "Timing" => SR.Current.Mpl2Timing,
        "Range" => SR.Current.Mpl2Range,
        "Italic" => SR.Current.Mpl2Italic,
        "Text" => SR.Current.SubtitleText,
        "Empty" => SR.Current.SubtitleEmpty,
        _ => throw new ArgumentOutOfRangeException(nameof(reason))
    }));
}
