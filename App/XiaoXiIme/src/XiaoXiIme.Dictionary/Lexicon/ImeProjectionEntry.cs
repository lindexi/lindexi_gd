namespace XiaoXiIme.Dictionary;

/// <summary>Maps phonetic components to an input alias.</summary>
public sealed record ImeProjectionEntry(string Initial, string Final, string Input);