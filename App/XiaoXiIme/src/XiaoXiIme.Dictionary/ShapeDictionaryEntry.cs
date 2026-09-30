namespace XiaoXiIme.Dictionary;

/// <summary>
/// Represents a normalized shape dictionary source entry.
/// </summary>
public sealed record ShapeDictionaryEntry(
    string Text,
    string ShapeCode,
    string Decomposition);
