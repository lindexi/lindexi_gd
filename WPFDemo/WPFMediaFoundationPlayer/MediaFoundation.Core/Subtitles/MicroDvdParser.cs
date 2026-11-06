using System.Globalization;
using System.Resources;

namespace MediaFoundation.Core;

internal static class MicroDvdParser
{
    private static readonly ResourceManager Resources = new("MediaFoundation.Core.Subtitles.MicroDvdStrings", typeof(MicroDvdParser).Assembly);

    internal static SubtitleDocument Parse(string text, double? frameRate)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (frameRate.HasValue && !ValidFrameRate(frameRate.Value)) throw Error(1, "FrameRate");
        string[] lines = SubtitleParsing.Lines(text);
        var cues = new List<SubtitleCue>();
        bool first = true;
        for (int index = 0; index < lines.Length; index++)
        {
            string content = lines[index].Trim();
            if (content.Length == 0) continue;
            int line = index + 1;
            if (!TryFrame(ref content, out long startFrame) || !TryFrame(ref content, out long endFrame))
                throw Error(line, "Frames");
            if (first && startFrame == endFrame && (startFrame == 0 || startFrame == 1))
            {
                if (!double.TryParse(content, NumberStyles.Float, CultureInfo.InvariantCulture, out double declared) || !ValidFrameRate(declared))
                    throw Error(line, "FrameRate");
                if (frameRate.HasValue && frameRate.Value != declared) throw Error(line, "Conflict");
                frameRate = declared;
                first = false;
                continue;
            }
            first = false;
            if (endFrame <= startFrame) throw Error(line, "Frames");
            if (!frameRate.HasValue) throw Error(line, "MissingFrameRate");
            if (content.Contains('{') || content.Contains('}')) throw Error(line, "Style");
            content = content.Replace('|', '\n');
            if (string.IsNullOrWhiteSpace(content)) throw Error(line, "Text");
            TimeSpan start = ConvertFrame(startFrame, frameRate.Value, line);
            TimeSpan end = ConvertFrame(endFrame, frameRate.Value, line);
            if (end <= start) throw Error(line, "Range");
            cues.Add(new SubtitleCue(start, end, content));
        }
        if (cues.Count == 0) throw Error(1, "Empty");
        return new SubtitleDocument(cues);
    }

    private static bool ValidFrameRate(double value) => double.IsFinite(value) && value > 0;

    private static bool TryFrame(ref string content, out long frame)
    {
        frame = 0;
        if (!content.StartsWith('{')) return false;
        int close = content.IndexOf('}');
        if (close < 0 || !long.TryParse(content.AsSpan(1, close - 1), NumberStyles.None, CultureInfo.InvariantCulture, out frame))
            return false;
        content = content[(close + 1)..];
        return true;
    }

    private static TimeSpan ConvertFrame(long frame, double frameRate, int line)
    {
        double ticks = Math.Round(frame * (double)TimeSpan.TicksPerSecond / frameRate, MidpointRounding.ToEven);
        // The double representation of long.MaxValue is 2^63, outside the valid tick range.
        if (!double.IsFinite(ticks) || ticks < 0 || ticks >= 9223372036854775808d) throw Error(line, "Range");
        return TimeSpan.FromTicks(checked((long)ticks));
    }

    private static FormatException Error(int line, string reason) =>
        new(string.Format(CultureInfo.CurrentCulture, Resources.GetString("Parse", CultureInfo.CurrentUICulture)!,
            line, Resources.GetString(reason, CultureInfo.CurrentUICulture)));
}
