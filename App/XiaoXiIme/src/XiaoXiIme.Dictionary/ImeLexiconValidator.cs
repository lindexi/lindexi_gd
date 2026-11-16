namespace XiaoXiIme.Dictionary;

internal static class ImeLexiconValidator
{
    internal static void Validate(ImeLexicon lexicon)
    {
        ArgumentNullException.ThrowIfNull(lexicon);
        ArgumentNullException.ThrowIfNull(lexicon.Entries);
        ArgumentNullException.ThrowIfNull(lexicon.ShapeEntries);
        ArgumentNullException.ThrowIfNull(lexicon.Projection);
        ArgumentNullException.ThrowIfNull(lexicon.LookupAliases);
        foreach (var entry in lexicon.Entries)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Text) || !IsReading(entry.CanonicalReading)
                || entry.BaseFrequency < 0 || entry.UserFrequency < 0 || !Enum.IsDefined(entry.SourceKind))
            {
                throw new ArgumentException(DictionaryResources.InvalidCompilerEntry, nameof(lexicon));
            }
        }

        foreach (var alias in lexicon.LookupAliases)
        {
            if (alias is null || string.IsNullOrWhiteSpace(alias.Input) || alias.Input.Any(c => c is < 'a' or > 'z')
                || !lexicon.Entries.Any(e => e.Text == alias.Text && e.CanonicalReading == alias.CanonicalReading))
            {
                throw new ArgumentException(DictionaryResources.InvalidCompilerEntry, nameof(lexicon));
            }
        }

        foreach (var shape in lexicon.ShapeEntries)
        {
            if (shape is null || string.IsNullOrWhiteSpace(shape.Text) || string.IsNullOrWhiteSpace(shape.ShapeCode)
                || shape.ShapeCode.Any(c => c is < 'a' or > 'z'))
            {
                throw new ArgumentException(DictionaryResources.InvalidCompilerEntry, nameof(lexicon));
            }
        }
    }

    internal static bool IsReading(string? reading) => !string.IsNullOrWhiteSpace(reading)
                                                       && reading.Split(' ').All
                                                           (s => s.Length > 0 && s.All(c => c is >= 'a' and <= 'z'));
}