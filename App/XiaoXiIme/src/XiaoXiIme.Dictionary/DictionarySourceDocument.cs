namespace XiaoXiIme.Dictionary;

/// <summary>
/// A native dictionary source with its YAML metadata and normalized entries.
/// </summary>
public sealed record DictionarySourceDocument<T>(DictionarySourceMetadata Metadata, IReadOnlyList<T> Entries);