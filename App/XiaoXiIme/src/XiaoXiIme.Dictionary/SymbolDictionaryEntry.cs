namespace XiaoXiIme.Dictionary;

/// <summary>
/// Represents a normalized symbol dictionary source entry.
/// </summary>
public sealed record SymbolDictionaryEntry(
    string Input,
    IReadOnlyList<string> Candidates);
