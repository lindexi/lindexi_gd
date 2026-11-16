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