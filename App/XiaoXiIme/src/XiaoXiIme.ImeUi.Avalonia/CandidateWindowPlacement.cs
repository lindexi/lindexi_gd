using Avalonia;

namespace XiaoXiIme.ImeUi.Avalonia;

/// <summary>Places a logical-size candidate window in a physical-pixel working area.</summary>
public static class CandidateWindowPlacement
{
    /// <summary>Flips above the exclusion top when needed, then clamps to the working area.</summary>
    public static PixelPoint Calculate(PixelPoint anchor, int excludeTop, Size logicalSize, double scaling, PixelRect workArea,
        PixelRect? exclusion = null)
    {
        if (!double.IsFinite(scaling) || scaling <= 0)
            throw new ArgumentOutOfRangeException(nameof(scaling));
        var width = (int)Math.Ceiling(Math.Max(0, logicalSize.Width) * scaling);
        var height = (int)Math.Ceiling(Math.Max(0, logicalSize.Height) * scaling);
        var y = (long)anchor.Y + height > workArea.Bottom ? (long)excludeTop - height : anchor.Y;
        PixelPoint Clamp(long x, long top) => new(
            (int)Math.Clamp(x, workArea.X, Math.Max((long)workArea.X, (long)workArea.Right - width)),
            (int)Math.Clamp(top, workArea.Y, Math.Max((long)workArea.Y, (long)workArea.Bottom - height)));
        var position = Clamp(anchor.X, y);
        if (exclusion is not { } area || area.Width <= 0 || area.Height <= 0)
            return position;
        bool Overlaps(PixelPoint p) => (long)p.X < area.Right && (long)p.X + width > area.X
            && (long)p.Y < area.Bottom && (long)p.Y + height > area.Y;
        if (!Overlaps(position))
            return position;
        // Clamping can move a below/above placement back into the excluded region; try every free side.
        foreach (var candidate in new[] { Clamp(anchor.X, area.Bottom), Clamp(anchor.X, (long)area.Y - height),
            Clamp(area.Right, anchor.Y), Clamp((long)area.X - width, anchor.Y) })
            if (!Overlaps(candidate))
                return candidate;
        return position;
    }
}
