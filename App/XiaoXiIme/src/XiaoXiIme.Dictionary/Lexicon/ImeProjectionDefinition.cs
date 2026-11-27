using System.Collections.Frozen;

namespace XiaoXiIme.Dictionary;

/// <summary>Defines component input aliases without assuming a unique reverse projection.</summary>
public sealed class ImeProjectionDefinition
{
    /// <summary>Creates an immutable projection definition.</summary>
    public ImeProjectionDefinition(string name, IReadOnlyList<ImeProjectionEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(entries);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(nameof(name), nameof(name));
        }

        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(entry.Initial);
            ArgumentNullException.ThrowIfNull(entry.Final);
            ArgumentNullException.ThrowIfNull(entry.Input);
            if (entry.Final.Length == 0 || entry.Input.Length == 0 ||
                (entry.Initial + entry.Final).Any(c => c is < 'a' or > 'z') ||
                entry.Input.Any(c => c is not (>= 'a' and <= 'z') and not ';'))
            {
                throw new ArgumentException(DictionaryResources.InvalidReading, nameof(entries));
            }
        }

        Name = name;
        Entries = Array.AsReadOnly(entries.Distinct().ToArray());
        IsFixedTwoKey = Entries.Count > 0 && Entries.All(entry => entry.Input.Length == 2);
    }

    private readonly FrozenSet<char>? _initialKeys;
    private readonly FrozenSet<string>? _completeSyllableCodes;

    private ImeProjectionDefinition(string name, IReadOnlyList<ImeProjectionEntry> entries, PhoneticLayout layout)
        : this(name, entries)
    {
        _initialKeys = layout.Initials.Values.ToFrozenSet();
        _completeSyllableCodes = Entries.Where(entry => entry.Initial.Length == 0 ||
                PhoneticInputProjection.IsLegalSyllable(entry.Initial + entry.Final))
            .Select(entry => entry.Input).ToFrozenSet(StringComparer.Ordinal);
    }

    /// <summary>Gets whether the layout provides initial keys for shorthand input.</summary>
    public bool SupportsInitialShorthand => _initialKeys is not null;

    /// <summary>Checks whether a normalized key represents an initial in this layout.</summary>
    public bool IsInitialKey(char key) => _initialKeys?.Contains(key) == true;

    /// <summary>Checks whether a normalized code denotes a legal complete syllable, regardless of dictionary entries.</summary>
    public bool IsCompleteSyllableCode(string code) => _completeSyllableCodes?.Contains(code) == true;

    public string Name { get; }
    public IReadOnlyList<ImeProjectionEntry> Entries { get; }
    public bool IsFixedTwoKey { get; }
    public static ImeProjectionDefinition FullPinyin { get; } = Create(false);
    public static ImeProjectionDefinition XiaoheDoublePinyin { get; } = Create(true);

    /// <summary>Gets the registered production projection, preserving colliding component evidence.</summary>
    public static ImeProjectionDefinition ForScheme(string scheme)
    {
        if (scheme == "fullPinyin")
        {
            return FullPinyin;
        }

        if (scheme == "xiaoheDoublePinyin")
        {
            return XiaoheDoublePinyin;
        }

        var layout = PhoneticSchemeCatalog.All.Single(item => item.SchemeId == scheme).Layout
                     ?? throw new NotSupportedException();
        var entries = new List<ImeProjectionEntry>();
        foreach (var initial in layout.Initials.Keys.Prepend(string.Empty))
        foreach (var final in layout.Finals.Keys.Concat(new[] { "ve", "ue" }).Distinct())
        {
            if (PhoneticInputProjection.TryProjectReading(initial + final, scheme, out var code))
            {
                entries.Add(new(initial, final, code));
            }
        }

        return new(scheme, entries, layout);
    }

    private static ImeProjectionDefinition Create(bool doublePinyin)
    {
        var entries = PhoneticInputProjection.GetComponentEntries(doublePinyin);
        return doublePinyin
            ? new("xiaohe-double-pinyin", entries, PhoneticInputProjection.Xiaohe)
            : new("full-pinyin", entries);
    }
}