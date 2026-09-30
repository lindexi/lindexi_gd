using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Identifies whether a ranked candidate came from user learning, the system package, or the fallback dictionary.
/// </summary>
public enum DictionaryCandidateSourceKind
{
    User,
    System,
    Fallback,
}

/// <summary>
/// Identifies whether a ranked candidate matched the query exactly or as a bounded prefix.
/// </summary>
public enum DictionaryCandidateMatchKind
{
    Exact,
    Prefix,
}

/// <summary>
/// Internal dictionary result used for layered ranking before mapping to <see cref="ImeCandidate"/>.
/// </summary>
public sealed record DictionaryCandidate(
    string Text,
    string Reading,
    int BaseFrequency,
    DictionaryCandidateSourceKind SourceKind,
    DictionaryCandidateMatchKind MatchKind,
    int UserFrequency = 0)
{
    public string Text
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = Text ?? throw new ArgumentNullException(nameof(Text));

    public string Reading
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = Reading ?? throw new ArgumentNullException(nameof(Reading));

    internal int Layer => (SourceKind, MatchKind) switch
    {
        (DictionaryCandidateSourceKind.User, DictionaryCandidateMatchKind.Exact) => 0,
        (DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Exact) => 1,
        (DictionaryCandidateSourceKind.System, DictionaryCandidateMatchKind.Prefix) => 2,
        (DictionaryCandidateSourceKind.Fallback, DictionaryCandidateMatchKind.Exact) => 3,
        _ => 4,
    };

    internal int SortFrequency => SourceKind == DictionaryCandidateSourceKind.User
        ? UserFrequency
        : BaseFrequency;

    /// <summary>
    /// Maps the ranked candidate to the core-facing <see cref="ImeCandidate"/> contract.
    /// </summary>
    public ImeCandidate ToImeCandidate()
    {
        return new ImeCandidate(Text, Reading, SortFrequency);
    }
}
