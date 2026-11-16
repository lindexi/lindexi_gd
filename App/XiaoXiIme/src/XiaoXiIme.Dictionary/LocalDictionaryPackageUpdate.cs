namespace XiaoXiIme.Dictionary;

/// <summary>
/// Describes local XiaoXiIme dictionary sources and compilation settings.
/// </summary>
public sealed record LocalDictionaryPackageUpdate
{
    /// <summary>
    /// Gets the phonetic TSV source paths.
    /// </summary>
    public required IReadOnlyList<string> PhoneticSourcePaths { get; init; }

    /// <summary>
    /// Gets the optional shape TSV source paths.
    /// </summary>
    public IReadOnlyList<string> ShapeSourcePaths { get; init; } = [];

    /// <summary>
    /// Gets the optional symbol TSV source paths.
    /// </summary>
    public IReadOnlyList<string> SymbolSourcePaths { get; init; } = [];

    /// <summary>
    /// Gets the package compilation parameters.
    /// </summary>
    public DictionaryPackageParameters Parameters { get; init; } = new();
}