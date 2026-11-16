namespace XiaoXiIme.Dictionary;

/// <summary>
/// Reports whether a local dictionary package was recompiled or reused.
/// </summary>
public sealed record LocalDictionaryPackageUpdateResult
(
    DictionaryPackageManifest Manifest,
    bool ReusedExistingPackage
);