using XiaoXiIme.Dictionary;

namespace XiaoXiIme.Cli;

internal static class DefaultDictionaryPackageBuilder
{
    internal const string FullPinyinPackageDirectoryName = DictionaryPackageLocations.FullPinyinPackageDirectoryName;
    internal const string FullPinyinInputScheme = DictionaryPackageLocations.FullPinyinInputScheme;
    internal const string XiaoheDoublePinyinInputScheme = DictionaryPackageLocations.XiaoheDoublePinyinInputScheme;
    internal static readonly string XiaoheDoublePinyinPackageRelativePath = DictionaryPackageLocations.XiaoheDoublePinyinPackageRelativePath;

    internal static void Build(string sourceDirectory, string hostOutputDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            throw new ArgumentException("A dictionary source directory is required.", nameof(sourceDirectory));
        }

        if (string.IsNullOrWhiteSpace(hostOutputDirectory))
        {
            throw new ArgumentException("A host output directory is required.", nameof(hostOutputDirectory));
        }

        var sourceRoot = EnsureRequiredSources(sourceDirectory);
        var hostRoot = Path.GetFullPath(hostOutputDirectory);

        var phoneticSources = FindSources(sourceRoot, "*.phonetic.tsv");
        var shapeSources = FindSources(sourceRoot, "*.shape.tsv");
        var symbolSources = FindSources(sourceRoot, "*.symbols.tsv");
        Directory.CreateDirectory(hostRoot);
        BuildPackage(
            hostRoot,
            FullPinyinPackageDirectoryName,
            FullPinyinInputScheme,
            phoneticSources,
            shapeSources,
            symbolSources);
        BuildPackage(
            hostRoot,
            XiaoheDoublePinyinPackageRelativePath,
            XiaoheDoublePinyinInputScheme,
            phoneticSources,
            shapeSources,
            symbolSources);
    }

    private static void BuildPackage(
        string hostRoot,
        string relativePackagePath,
        string inputScheme,
        IReadOnlyList<string> phoneticSources,
        IReadOnlyList<string> shapeSources,
        IReadOnlyList<string> symbolSources)
    {
        var packagePath = Path.Combine(hostRoot, relativePackagePath);
        DeleteDirectoryIfExists(packagePath);
        DeleteDirectoryIfExists($"{packagePath}.previous");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate
            {
                PhoneticSourcePaths = phoneticSources,
                ShapeSourcePaths = shapeSources,
                SymbolSourcePaths = symbolSources,
                Parameters = new DictionaryPackageParameters { InputScheme = inputScheme },
            },
            packagePath);
    }

    internal static string EnsureRequiredSources(string sourceDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            throw new ArgumentException("A dictionary source directory is required.", nameof(sourceDirectory));
        }

        var sourceRoot = Path.GetFullPath(sourceDirectory);
        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"Default dictionary source directory was not found: {sourceRoot}");
        }

        RequireSource(sourceRoot, "phonetic/sewzc-default.phonetic.tsv");
        RequireSource(sourceRoot, XiaoXiImeProjectTerms.OutputRelativePath);
        RequireSource(sourceRoot, "shape/sewzc-moqi.shape.tsv");
        RequireSource(sourceRoot, "symbols/sewzc-default.symbols.tsv");
        return sourceRoot;
    }

    private static void RequireSource(string sourceRoot, string relativePath)
    {
        var sourcePath = Path.Combine(sourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourcePath))
        {
            throw new InvalidOperationException($"Required XiaoXiIme default dictionary source was not found: {relativePath}. Run dictionary-convert-sewzc first.");
        }
    }

    private static IReadOnlyList<string> FindSources(string sourceDirectory, string pattern)
    {
        return Directory.EnumerateFiles(sourceDirectory, pattern, SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }
}
