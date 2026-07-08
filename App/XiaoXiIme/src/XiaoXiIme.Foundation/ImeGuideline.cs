namespace XiaoXiIme.Foundation;

public sealed record ImeGuideline(
    ImeGuidelineLevel Level,
    string Text)
{
    public static ImeGuideline Empty { get; } = new(ImeGuidelineLevel.None, string.Empty);
}
