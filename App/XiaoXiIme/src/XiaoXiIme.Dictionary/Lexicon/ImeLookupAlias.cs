namespace XiaoXiIme.Dictionary;

/// <summary>Describes an explicit lookup abbreviation for a canonical entry.</summary>
public sealed record ImeLookupAlias(string Text, string CanonicalReading, string Input);