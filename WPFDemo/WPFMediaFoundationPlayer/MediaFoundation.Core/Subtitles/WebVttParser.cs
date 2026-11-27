namespace MediaFoundation.Core;

internal static class WebVttParser
{
    internal static SubtitleDocument Parse(string text)
    {
        const SubtitleFormat format = SubtitleFormat.WebVtt;
        string[] lines = SubtitleParsing.Lines(text);
        if (!(lines[0] == "WEBVTT" || lines[0].StartsWith("WEBVTT ", StringComparison.Ordinal) || lines[0].StartsWith("WEBVTT\t", StringComparison.Ordinal)))
            throw SubtitleParsing.Error(format, 1, "SubtitleHeader");
        int index = 1;
        if (index < lines.Length && !string.IsNullOrWhiteSpace(lines[index]))
            throw SubtitleParsing.Error(format, index + 1, "SubtitleLayout");
        var cues = new List<SubtitleCue>();
        while (index < lines.Length)
        {
            while (index < lines.Length && string.IsNullOrWhiteSpace(lines[index])) index++;
            if (index == lines.Length) break;
            if (lines[index] == "NOTE" || lines[index].StartsWith("NOTE ", StringComparison.Ordinal) || lines[index].StartsWith("NOTE\t", StringComparison.Ordinal))
            {
                while (index < lines.Length && !string.IsNullOrWhiteSpace(lines[index])) index++;
                continue;
            }
            if (lines[index] == "STYLE" || lines[index] == "REGION") throw SubtitleParsing.Error(format, index + 1, "SubtitleLayout");
            if (!lines[index].Contains("-->", StringComparison.Ordinal)) index++;
            if (index == lines.Length) throw SubtitleParsing.Error(format, index, "SubtitleTiming");
            int timingLine = index + 1;
            string[] times = lines[index++].Split("-->", StringSplitOptions.TrimEntries);
            if (times.Length != 2) throw SubtitleParsing.Error(format, timingLine, "SubtitleTiming");
            if (times[1].Any(char.IsWhiteSpace)) throw SubtitleParsing.Error(format, timingLine, "SubtitleLayout");
            if (!SubtitleParsing.TryTime(times[0], '.', true, out var start) ||
                !SubtitleParsing.TryTime(times[1], '.', true, out var end) || end <= start)
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
