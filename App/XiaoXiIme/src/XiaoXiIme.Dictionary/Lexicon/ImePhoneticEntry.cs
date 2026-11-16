namespace XiaoXiIme.Dictionary;

/// <summary>Describes a canonical phonetic dictionary entry.</summary>
public sealed record ImePhoneticEntry
(
    string Text,
    string CanonicalReading,
    int BaseFrequency,
    DictionaryCandidateSourceKind SourceKind = DictionaryCandidateSourceKind.System,
    int UserFrequency = 0
);