namespace XiaoXiIme.Dictionary;

/// <summary>
/// Represents an invalid or unsupported XiaoXiIme dictionary package.
/// </summary>
public sealed class DictionaryPackageException : IOException
{
    /// <summary>
    /// Initializes an exception for the specified package.
    /// </summary>
    public DictionaryPackageException(string packagePath, string message)
        : base(string.Format(DictionaryResources.InvalidDictionaryPackage, packagePath, message))
    {
        PackagePath = packagePath;
    }

    /// <summary>
    /// Gets the package path associated with the error.
    /// </summary>
    public string PackagePath { get; }
}
