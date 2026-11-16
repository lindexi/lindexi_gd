namespace XiaoXiIme.Dictionary;

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
                (entry.Initial + entry.Final).Any(c => c is < 'a' or > 'z') ||
                entry.Input.Any(c => c is not (>= 'a' and <= 'z') and not ';'))
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

    /// <summary>Gets the registered production projection, preserving colliding component evidence.</summary>
    public static ImeProjectionDefinition ForScheme(string scheme)
    {
        if (scheme == "fullPinyin") return FullPinyin;
        if (scheme == "xiaoheDoublePinyin") return XiaoheDoublePinyin;
        var layout = PhoneticSchemeCatalog.All.Single(item => item.SchemeId == scheme).Layout
                     ?? throw new NotSupportedException();
        var entries = new List<ImeProjectionEntry>();
        foreach (var initial in layout.Initials.Keys.Prepend(string.Empty))
        foreach (var final in layout.Finals.Keys.Concat(new[] { "ve", "ue" }).Distinct())
        {
            if (PhoneticInputProjection.TryProjectReading(initial + final, scheme, out var code))
                entries.Add(new(initial, final, code));
        }
        return new(scheme, entries);
    }

    private static ImeProjectionDefinition Create(bool doublePinyin)
    {
        var entries = PhoneticInputProjection.GetComponentEntries(doublePinyin);
        return new(doublePinyin ? "xiaohe-double-pinyin" : "full-pinyin", entries);
    }
}