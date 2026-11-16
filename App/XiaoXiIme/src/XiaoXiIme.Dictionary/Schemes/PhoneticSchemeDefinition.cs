using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

/// <summary>Source evidence and stable product identity for a phonetic scheme. A null product identity cannot be selected.</summary>
public sealed record PhoneticSchemeDefinition
(
    string SchemeId,
    string DisplayResourceKey,
    ImeProductInputScheme? ProductScheme,
    string SourceKind,
    string SourcePath,
    string? SourceVersion,
    string ProjectionVersion,
    string? Rules = null,
    string? ContentSha256 = null,
    PhoneticLayout? Layout = null,
    IReadOnlyList<string>? UnresolvedItems = null
);