namespace XiaoXiIme.Dictionary;

/// <summary>
/// Inspects a compiled dictionary package for diagnostics.
/// </summary>
public sealed record DictionaryPackageInspection(
    string PackagePath,
    DictionaryPackageManifest Manifest,
    IReadOnlyDictionary<string, string> ShardPaths);