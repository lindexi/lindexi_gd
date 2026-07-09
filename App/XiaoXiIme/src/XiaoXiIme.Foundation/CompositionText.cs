namespace XiaoXiIme.Foundation;

public sealed record CompositionText(
    string Reading,
    string DisplayText,
    int CaretIndex)
{
    public static CompositionText Empty { get; } = new(string.Empty, string.Empty, 0);
}