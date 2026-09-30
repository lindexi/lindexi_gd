namespace XiaoXiIme.Dictionary;

/// <summary>
/// Represents a normalized phonetic dictionary source entry.
/// </summary>
public sealed record PhoneticDictionaryEntry(
    string Text,
    string Reading,
    int Frequency);
