using System.Globalization;
using System.Resources;

namespace XiaoXiIme.Dictionary;

internal static class DictionaryResources
{
    private static readonly ResourceManager ResourceManager = new(
        "XiaoXiIme.Dictionary.DictionaryResources",
        typeof(DictionaryResources).Assembly);

    internal static string EmptyReading => GetString(nameof(EmptyReading));
    internal static string EmptyText => GetString(nameof(EmptyText));
    internal static string InvalidColumnCount => GetString(nameof(InvalidColumnCount));
    internal static string InvalidDecomposition => GetString(nameof(InvalidDecomposition));
    internal static string InvalidDictionarySourceLine => GetString(nameof(InvalidDictionarySourceLine));
    internal static string InvalidShapeCode => GetString(nameof(InvalidShapeCode));
    internal static string InvalidShapeText => GetString(nameof(InvalidShapeText));
    internal static string InvalidSymbolCandidate => GetString(nameof(InvalidSymbolCandidate));
    internal static string InvalidSymbolColumnCount => GetString(nameof(InvalidSymbolColumnCount));
    internal static string InvalidSymbolInput => GetString(nameof(InvalidSymbolInput));
    internal static string InvalidFrequency => GetString(nameof(InvalidFrequency));
    internal static string InvalidPhoneticSourceLine => GetString(nameof(InvalidPhoneticSourceLine));
    internal static string InvalidReading => GetString(nameof(InvalidReading));
    internal static string LineTooLong => GetString(nameof(LineTooLong));
    internal static string ReadingTooLong => GetString(nameof(ReadingTooLong));
    internal static string TextTooLong => GetString(nameof(TextTooLong));
    internal static string InvalidCandidateData => GetString(nameof(InvalidCandidateData));
    internal static string InvalidCandidateId => GetString(nameof(InvalidCandidateId));
    internal static string InvalidCompilerEntry => GetString(nameof(InvalidCompilerEntry));
    internal static string UnsupportedInputSchemeReading => GetString(nameof(UnsupportedInputSchemeReading));
    internal static string InvalidCompilerParameters => GetString(nameof(InvalidCompilerParameters));
    internal static string InvalidDictionaryPackage => GetString(nameof(InvalidDictionaryPackage));
    internal static string InvalidIndexData => GetString(nameof(InvalidIndexData));
    internal static string InvalidManifest => GetString(nameof(InvalidManifest));
    internal static string InvalidManifestLength => GetString(nameof(InvalidManifestLength));
    internal static string InvalidPackageCount => GetString(nameof(InvalidPackageCount));
    internal static string InvalidPackageKind => GetString(nameof(InvalidPackageKind));
    internal static string InvalidShardLength => GetString(nameof(InvalidShardLength));
    internal static string InvalidShardMagic => GetString(nameof(InvalidShardMagic));
    internal static string InvalidShardPath => GetString(nameof(InvalidShardPath));
    internal static string InvalidShardSet => GetString(nameof(InvalidShardSet));
    internal static string PackageCountTooLarge => GetString(nameof(PackageCountTooLarge));
    internal static string PackageStringTooLong => GetString(nameof(PackageStringTooLong));
    internal static string UnsupportedPackageVersion => GetString(nameof(UnsupportedPackageVersion));
    internal static string UnsupportedShardVersion => GetString(nameof(UnsupportedShardVersion));
    internal static string InvalidUserDictionary => GetString(nameof(InvalidUserDictionary));
    internal static string InvalidUserDictionaryPath => GetString(nameof(InvalidUserDictionaryPath));
    internal static string UserDictionaryEntryCountTooLarge => GetString(nameof(UserDictionaryEntryCountTooLarge));

    private static string GetString(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
    }
}
