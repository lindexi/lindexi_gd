using Avalonia;

namespace XiaoXiIme.ImeUi.Avalonia;

/// <summary>Places a logical-size candidate window in a physical-pixel working area.</summary>
public static class CandidateWindowPlacement
{
    /// <summary>Flips above the exclusion top when needed, then clamps to the working area.</summary>
    public static PixelPoint Calculate(PixelPoint anchor, int excludeTop, Size logicalSize, double scaling, PixelRect workArea)
    {
        if (!double.IsFinite(scaling) || scaling <= 0)
            throw new ArgumentOutOfRangeException(nameof(scaling));
        var width = (int)Math.Ceiling(Math.Max(0, logicalSize.Width) * scaling);
        var height = (int)Math.Ceiling(Math.Max(0, logicalSize.Height) * scaling);
        var y = (long)anchor.Y + height > workArea.Bottom ? (long)excludeTop - height : anchor.Y;
        return new PixelPoint(
            (int)Math.Clamp((long)anchor.X, workArea.X, Math.Max((long)workArea.X, (long)workArea.Right - width)),
            (int)Math.Clamp(y, workArea.Y, Math.Max((long)workArea.Y, (long)workArea.Bottom - height)));
    }
}
