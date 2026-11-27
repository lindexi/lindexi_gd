namespace XiaoXiIme.Dictionary;

/// <summary>
/// Resolves installed dictionary package directories for supported input schemes.
/// </summary>
public static class DictionaryPackageLocations
{
    public const string FullPinyinPackageDirectoryName = "XiaoXiIme.DictionaryPackage";

    public const string PackageRootDirectoryName = "XiaoXiIme.DictionaryPackages";

    public const string FullPinyinInputScheme = DictionaryPackageFormat.FullPinyinInputScheme;

    public const string XiaoheDoublePinyinInputScheme = DictionaryPackageFormat.XiaoheDoublePinyinInputScheme;

    public const string InputSchemeEnvironmentVariableName = "XIAOXIIME_INPUT_SCHEME";

    public static readonly string XiaoheDoublePinyinPackageRelativePath = Path.Combine
    (
        PackageRootDirectoryName,
        XiaoheDoublePinyinInputScheme
    );

    /// <summary>
    /// Resolves the installed package directory for an input scheme.
    /// Explicit package paths are used as-is. Otherwise the default host layout is:
    /// <c>XiaoXiIme.DictionaryPackage</c> for full Pinyin and
    /// <c>XiaoXiIme.DictionaryPackages/xiaoheDoublePinyin</c> for Xiaohe double Pinyin.
    /// </summary>
    public static string ResolvePackageDirectory
    (
        string? packageDirectory = null,
        string? inputScheme = null,
        string? baseDirectory = null
    )
    {
        if (!string.IsNullOrWhiteSpace(packageDirectory))
        {
            return Path.GetFullPath(packageDirectory);
        }

        var scheme = ResolveInputScheme(inputScheme);
        var root = Path.GetFullPath(baseDirectory ?? AppContext.BaseDirectory);
        if (Directory.Exists(Path.Combine(root, "Dictionaries")))
        {
            return GetDistributionPackageDirectory(root, scheme);
        }

        if (baseDirectory is null && string.Equals(Path.GetFileName(Environment.ProcessPath), "XiaoXiIme.exe", StringComparison.OrdinalIgnoreCase))
        {
            return GetDistributionPackageDirectory(Path.GetDirectoryName(Environment.ProcessPath)!, scheme);
        }

        return Path.Combine(root, GetRelativePackageDirectory(scheme));
    }

    /// <summary>Gets the dictionary package for a scheme in the unified distribution.</summary>
    public static string GetDistributionPackageDirectory(string distributionRoot, string inputScheme)
    {
        return Path.Combine(distributionRoot, "Dictionaries", ResolveInputScheme(inputScheme));
    }

    /// <summary>
    /// Resolves an input scheme from an explicit value or the full Pinyin default.
    /// Unknown values fall back to full Pinyin so the host can keep a diagnosable package path.
    /// </summary>
    public static string ResolveInputScheme(string? inputScheme = null)
    {
        var requested = inputScheme;
        if (string.IsNullOrWhiteSpace(requested)
            || string.Equals(requested, FullPinyinInputScheme, StringComparison.OrdinalIgnoreCase))
        {
            return FullPinyinInputScheme;
        }

        var registered = PhoneticSchemeCatalog.All.FirstOrDefault
        (item => item.ProductScheme.HasValue && string.Equals
            (item.SchemeId, requested, StringComparison.OrdinalIgnoreCase)
        );
        if (registered is not null)
        {
            return registered.SchemeId;
        }

        return FullPinyinInputScheme;
    }

    /// <summary>
    /// Returns the payload-relative directory for a supported input scheme.
    /// </summary>
    public static string GetRelativePackageDirectory(string inputScheme)
    {
        var scheme = ResolveInputScheme(inputScheme);
        return scheme == FullPinyinInputScheme
            ? FullPinyinPackageDirectoryName
            : Path.Combine(PackageRootDirectoryName, scheme);
    }
}