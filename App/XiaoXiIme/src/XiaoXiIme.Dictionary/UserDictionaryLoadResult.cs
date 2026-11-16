namespace XiaoXiIme.Dictionary;

/// <summary>
/// Reports loaded entries and any recovery action taken for an invalid user dictionary.
/// </summary>
public sealed record UserDictionaryLoadResult(
    IReadOnlyList<UserDictionaryEntry> Entries,
    string? Error,
    string? IsolatedCorruptFilePath);