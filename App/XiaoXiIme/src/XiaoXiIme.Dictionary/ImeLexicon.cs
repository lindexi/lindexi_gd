namespace XiaoXiIme.Dictionary;

/// <summary>Describes a canonical phonetic dictionary entry.</summary>
public sealed record ImePhoneticEntry(string Text, string CanonicalReading, int BaseFrequency,
    DictionaryCandidateSourceKind SourceKind = DictionaryCandidateSourceKind.System, int UserFrequency = 0);
/// <summary>Maps phonetic components to an input alias.</summary>
public sealed record ImeProjectionEntry(string Initial, string Final, string Input);
/// <summary>Describes an explicit lookup abbreviation for a canonical entry.</summary>
public sealed record ImeLookupAlias(string Text, string CanonicalReading, string Input);
/// <summary>Describes a complete registered segment selected for learning.</summary>
public sealed record ImeConfirmedSegment(string Text, string CanonicalReading, int InputStart, int InputLength, int OutputStart, int OutputLength);
/// <summary>Provides the complete canonical data used for sound-shape decoding.</summary>
public sealed record ImeLexicon(IReadOnlyList<ImePhoneticEntry> Entries, IReadOnlyList<ShapeDictionaryEntry> ShapeEntries,
    ImeProjectionDefinition Projection, long Version)
{
    public IReadOnlyList<ImeLookupAlias> LookupAliases { get; init; } = [];
}

/// <summary>Defines component input aliases without assuming a unique reverse projection.</summary>
public sealed class ImeProjectionDefinition
{
    /// <summary>Creates an immutable projection definition.</summary>
    public ImeProjectionDefinition(string name, IReadOnlyList<ImeProjectionEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(entries);
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name), nameof(name));
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(entry.Initial);
            ArgumentNullException.ThrowIfNull(entry.Final);
            ArgumentNullException.ThrowIfNull(entry.Input);
            if (entry.Final.Length == 0 || entry.Input.Length == 0 ||
                (entry.Initial + entry.Final + entry.Input).Any(c => c is < 'a' or > 'z'))
                throw new ArgumentException(DictionaryResources.InvalidReading, nameof(entries));
        }
        Name = name;
        Entries = Array.AsReadOnly(entries.Distinct().ToArray());
        IsFixedTwoKey = Entries.Count > 0 && Entries.All(entry => entry.Input.Length == 2);
    }
    public string Name { get; }
    public IReadOnlyList<ImeProjectionEntry> Entries { get; }
    public bool IsFixedTwoKey { get; }
    public static ImeProjectionDefinition FullPinyin { get; } = Create(false);
    public static ImeProjectionDefinition XiaoheDoublePinyin { get; } = Create(true);

    private static ImeProjectionDefinition Create(bool doublePinyin)
    {
        var entries = PhoneticInputProjection.GetComponentEntries(doublePinyin);
        return new(doublePinyin ? "xiaohe-double-pinyin" : "full-pinyin", entries);
    }
}
