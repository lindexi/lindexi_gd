using System.Globalization;

namespace MediaFoundation.Core;

internal static class SubViewerParser
{

    internal static SubtitleDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string[] lines = SubtitleParsing.Lines(text);
        int index = 0;
        SkipBlank(lines, ref index);
        if (index == lines.Length) throw Error(1, "Empty");
        if (lines[index].Trim() != "[INFORMATION]") throw Error(index + 1, "Header");
        int informationLine = ++index;
        var metadata = new HashSet<string>(StringComparer.Ordinal);
        while (index < lines.Length && lines[index].Trim() != "[END INFORMATION]")
        {
            string header = lines[index].Trim();
            int line = index++ + 1;
            if (header.Length == 0) continue;
            int close = header.IndexOf(']');
            if (!header.StartsWith('[') || close < 2) throw Error(line, "Metadata");
            string key = header[1..close];
            string value = header[(close + 1)..].Trim();
            if (!metadata.Add(key)) throw Error(line, "Metadata");
            switch (key)
            {
                case "TITLE":
                case "AUTHOR":
                case "SOURCE":
                case "PRG":
                case "FILEPATH":
                case "COMMENT":
                case "DATE":
                    break;
                case "VERSION":
                    if (value != "2.0") throw Error(line, "Version");
                    break;
                case "DELAY":
                case "CD TRACK":
                    if (value != "0") throw Error(line, "Setting");
                    break;
                case "COLF":
                case "SIZE":
                case "FONT":
                case "STYLE":
                    throw Error(line, "Style");
                default:
                    throw Error(line, "Metadata");
            }
        }
        if (index == lines.Length) throw Error(informationLine, "InformationEnd");
        index++;
        SkipBlank(lines, ref index);
        if (index == lines.Length || lines[index].Trim() != "[SUBTITLE]")
            throw Error(Math.Min(index + 1, lines.Length), "SubtitleHeader");
        index++;
        var cues = new List<SubtitleCue>();
        while (index < lines.Length)
        {
            SkipBlank(lines, ref index);
            if (index == lines.Length) break;
            int timingLine = index + 1;
            string timing = lines[index++].Trim();
            if (timing.StartsWith('[')) throw Error(timingLine, "Style");
            string[] times = timing.Split(',', StringSplitOptions.TrimEntries);
            if (times.Length != 2 || !TryTime(times[0], out var start) ||
                !TryTime(times[1], out var end) || end <= start)
                throw Error(timingLine, "Timing");
            var body = new List<string>();
            while (index < lines.Length && !string.IsNullOrWhiteSpace(lines[index]))
            {
                string content = lines[index];
                int line = index++ + 1;
                if (content.Contains(',') && content.Contains(':'))
                {
                    string[] candidate = content.Trim().Split(',', StringSplitOptions.TrimEntries);
                    if (candidate.Length == 2 && TryTime(candidate[0], out _) && TryTime(candidate[1], out _))
                        throw Error(line, "Separator");
                }
                content = content.Replace("[br]", "\n", StringComparison.OrdinalIgnoreCase);
                if (content.Contains('[') || content.Contains(']')) throw Error(line, "Style");
                body.Add(content);
            }
            string plainText = string.Join('\n', body);
            if (string.IsNullOrWhiteSpace(plainText)) throw Error(timingLine, "Text");
            cues.Add(new SubtitleCue(start, end, plainText));
        }
        if (cues.Count == 0) throw Error(1, "Empty");
        return new SubtitleDocument(cues);
    }

    private static void SkipBlank(string[] lines, ref int index)
    {
        while (index < lines.Length && string.IsNullOrWhiteSpace(lines[index])) index++;
    }

    private static bool TryTime(string text, out TimeSpan time)
    {
        time = default;
        if (text.Length != 11 || text[2] != ':' || text[5] != ':' || text[8] != '.') return false;
        for (int index = 0; index < text.Length; index++)
        {
            if (index is 2 or 5 or 8) continue;
            if (!char.IsAsciiDigit(text[index])) return false;
        }
        int hours = int.Parse(text.AsSpan(0, 2), CultureInfo.InvariantCulture);
        int minutes = int.Parse(text.AsSpan(3, 2), CultureInfo.InvariantCulture);
        int seconds = int.Parse(text.AsSpan(6, 2), CultureInfo.InvariantCulture);
        int centiseconds = int.Parse(text.AsSpan(9, 2), CultureInfo.InvariantCulture);
        if (minutes > 59 || seconds > 59) return false;
        long ticks = (hours * 3600L + minutes * 60L + seconds) * TimeSpan.TicksPerSecond +
            centiseconds * (TimeSpan.TicksPerMillisecond * 10);
        time = TimeSpan.FromTicks(ticks);
        return true;
    }

    private static FormatException Error(int line, string reason) => new(SR.Current.SubViewerParse.ToString(line, reason switch
    {
        "Empty" => SR.Current.SubtitleEmpty,
        "Header" => SR.Current.SubViewerHeader,
        "Metadata" => SR.Current.SubViewerMetadata,
        "Version" => SR.Current.SubViewerVersion,
        "Setting" => SR.Current.SubViewerSetting,
        "Style" => SR.Current.SubViewerStyle,
        "InformationEnd" => SR.Current.SubViewerInformationEnd,
        "SubtitleHeader" => SR.Current.SubViewerSubtitleHeader,
        "Timing" => SR.Current.SubViewerTiming,
        "Separator" => SR.Current.SubViewerSeparator,
        "Text" => SR.Current.SubtitleText,
        _ => throw new ArgumentOutOfRangeException(nameof(reason))
    }));
}
