namespace XiaoXiIme.Foundation;

/// <summary>
/// Attribution shown by the about UI, diagnostics, and dictionary inspection.
/// </summary>
public sealed record ImeAboutState(string SourceName, string Notice)
{
    public const string SeWzcSourceName = "SeWZC";

    public const string SeWzcNotice =
        "Dictionary data and dictionary-logic contributions are attributed to SeWZC.";

    public static ImeAboutState Empty { get; } = new(string.Empty, string.Empty);

    public static ImeAboutState SeWzc { get; } = new(SeWzcSourceName, SeWzcNotice);

    public bool IsEmpty => string.IsNullOrWhiteSpace(SourceName) && string.IsNullOrWhiteSpace(Notice);
}
