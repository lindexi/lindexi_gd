namespace XiaoXiIme.Dictionary;

/// <summary>
/// Represents one persisted user dictionary entry.
/// </summary>
public sealed record UserDictionaryEntry(string Text, string Reading, int SelectionCount);