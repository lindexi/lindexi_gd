namespace MediaFoundation.Core;

internal static class SubRipParser
{
    internal static SubtitleDocument Parse(string text)
    {
        const SubtitleFormat format = SubtitleFormat.SubRip;
        string[] lines = SubtitleParsing.Lines(text);
        var cues = new List<SubtitleCue>();
        int index = 0;
        while (index < lines.Length)
        {
            while (index < lines.Length && string.IsNullOrWhiteSpace(lines[index])) index++;
            if (index == lines.Length) break;
            if (!lines[index].Contains("-->", StringComparison.Ordinal)) index++;
            if (index == lines.Length) throw SubtitleParsing.Error(format, index, "SubtitleTiming");
            int timingLine = index + 1;
            string[] times = lines[index++].Split("-->", StringSplitOptions.TrimEntries);
            if (times.Length != 2 ||
                !SubtitleParsing.TryTime(times[0], ',', false, out var start) ||
                !SubtitleParsing.TryTime(times[1], ',', false, out var end) || end <= start)
                throw SubtitleParsing.Error(format, timingLine, "SubtitleTiming");
            int bodyStart = index;
            while (index < lines.Length && !string.IsNullOrWhiteSpace(lines[index]))
            {
                if (lines[index].Contains("-->", StringComparison.Ordinal)) throw SubtitleParsing.Error(format, index + 1, "SubtitleTiming");
                index++;
            }
            string content = SubtitleParsing.PlainText(lines[bodyStart..index]);
            if (string.IsNullOrWhiteSpace(content)) throw SubtitleParsing.Error(format, timingLine, "SubtitleText");
            cues.Add(new SubtitleCue(start, end, content));
        }
        if (cues.Count == 0) throw SubtitleParsing.Error(format, 1, "SubtitleEmpty");
        return new SubtitleDocument(cues);
    }
}
