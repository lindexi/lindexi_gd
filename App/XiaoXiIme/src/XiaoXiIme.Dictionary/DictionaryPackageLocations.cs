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

    public static readonly string XiaoheDoublePinyinPackageRelativePath = Path.Combine(
        PackageRootDirectoryName,
        XiaoheDoublePinyinInputScheme);

    /// <summary>
    /// Resolves the installed package directory for an input scheme.
    /// Explicit package paths are used as-is. Otherwise the default host layout is:
    /// <c>XiaoXiIme.DictionaryPackage</c> for full Pinyin and
    /// <c>XiaoXiIme.DictionaryPackages/xiaoheDoublePinyin</c> for Xiaohe double Pinyin.
    /// </summary>
    public static string ResolvePackageDirectory(
        string? packageDirectory = null,
        string? inputScheme = null,
        string? baseDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(packageDirectory))
        {
            return Path.GetFullPath(packageDirectory);
        }

        var scheme = ResolveInputScheme(inputScheme);
        var root = Path.GetFullPath(baseDirectory ?? AppContext.BaseDirectory);
        return Path.GetFullPath(Path.Combine(root, GetRelativePackageDirectory(scheme)));
    }

    /// <summary>
    /// Resolves an input scheme from an explicit value or <c>XIAOXIIME_INPUT_SCHEME</c>.
    /// Unknown values fall back to full Pinyin so the host can keep a diagnosable package path.
    /// </summary>
    public static string ResolveInputScheme(string? inputScheme = null)
    {
        var requested = string.IsNullOrWhiteSpace(inputScheme)
            ? Environment.GetEnvironmentVariable(InputSchemeEnvironmentVariableName)
            : inputScheme;
        if (string.IsNullOrWhiteSpace(requested)
            || string.Equals(requested, FullPinyinInputScheme, StringComparison.OrdinalIgnoreCase))
        {
            return FullPinyinInputScheme;
        }

        if (string.Equals(requested, XiaoheDoublePinyinInputScheme, StringComparison.OrdinalIgnoreCase))
        {
            return XiaoheDoublePinyinInputScheme;
        }

        return FullPinyinInputScheme;
    }

    /// <summary>
    /// Returns the payload-relative directory for a supported input scheme.
    /// </summary>
    public static string GetRelativePackageDirectory(string inputScheme)
    {
        return string.Equals(ResolveInputScheme(inputScheme), XiaoheDoublePinyinInputScheme, StringComparison.Ordinal)
            ? XiaoheDoublePinyinPackageRelativePath
            : FullPinyinPackageDirectoryName;
    }
}
