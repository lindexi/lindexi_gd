namespace UnoInk.Inking.InkCore.Settings;

public record CleanStrokeSettings
{
    public bool ShouldDrawBackground { get; init; } = false;
    public bool ShouldUpdateBackground { get; init; } = true;
}
