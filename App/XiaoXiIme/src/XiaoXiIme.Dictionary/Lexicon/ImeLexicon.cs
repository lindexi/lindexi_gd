namespace XiaoXiIme.Dictionary;

/// <summary>Provides the complete canonical data used for sound-shape decoding.</summary>
public sealed record ImeLexicon
(
    IReadOnlyList<ImePhoneticEntry> Entries,
    IReadOnlyList<ShapeDictionaryEntry> ShapeEntries,
    ImeProjectionDefinition Projection,
    long Version
)
{
    public IReadOnlyList<ImeLookupAlias> LookupAliases { get; init; } = [];

    /// <summary>Gets immutable selection counts independently of the spelling topology.</summary>
    public IReadOnlyDictionary<(string Text, string Reading), int> UserFrequencies { get; init; }
        = new System.Collections.ObjectModel.ReadOnlyDictionary<(string, string), int>
            (new Dictionary<(string, string), int>());

    /// <summary>Gets the background-prepared normalization mass for the ranking layer.</summary>
    public double? RankingLogFrequencyMass { get; init; }
}