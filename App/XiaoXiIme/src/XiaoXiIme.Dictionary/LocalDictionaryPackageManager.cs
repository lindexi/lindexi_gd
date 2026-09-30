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

/// <summary>
/// Reports whether a local dictionary package was recompiled or reused.
/// </summary>
public sealed record LocalDictionaryPackageUpdateResult(
    DictionaryPackageManifest Manifest,
    bool ReusedExistingPackage);

/// <summary>
/// Inspects a compiled dictionary package for diagnostics.
/// </summary>
public sealed record DictionaryPackageInspection(
    string PackagePath,
    DictionaryPackageManifest Manifest,
    IReadOnlyDictionary<string, string> ShardPaths);

/// <summary>
/// Compiles, validates, updates, and rolls back a local dictionary package.
/// </summary>
public static class LocalDictionaryPackageManager
{
    /// <summary>
    /// Compiles local TSV sources and atomically replaces the active package while retaining one previous version.
    /// Unchanged sources, compiler version, format version, and parameters reuse the existing package.
    /// </summary>
    public static DictionaryPackageManifest Update(LocalDictionaryPackageUpdate update, string packageDirectory)
    {
        return UpdateWithReuse(update, packageDirectory).Manifest;
    }

    /// <summary>
    /// Compiles local TSV sources when the existing package cannot be reused.
    /// </summary>
    public static LocalDictionaryPackageUpdateResult UpdateWithReuse(
        LocalDictionaryPackageUpdate update,
        string packageDirectory)
    {
        ArgumentNullException.ThrowIfNull(update);
        ValidatePackageDirectory(packageDirectory);
        if (update.PhoneticSourcePaths.Count == 0)
        {
            throw new ArgumentException("At least one phonetic source is required.", nameof(update));
        }

        var packagePath = Path.GetFullPath(packageDirectory);
        var parentPath = Directory.GetParent(packagePath)?.FullName
            ?? throw new ArgumentException("The package directory must have a parent directory.", nameof(packageDirectory));
        Directory.CreateDirectory(parentPath);
        var sources = CreateSourceRecords(update);
        if (TryReuseExistingPackage(packagePath, update.Parameters, sources, out var reusedManifest))
        {
            return new LocalDictionaryPackageUpdateResult(reusedManifest, ReusedExistingPackage: true);
        }

        var stagingPath = Path.Combine(parentPath, $".{Path.GetFileName(packagePath)}.staging-{Guid.NewGuid():N}");
        try
        {
            var phoneticEntries = ParseSources(update.PhoneticSourcePaths, PhoneticDictionarySourceParser.Parse);
            var shapeEntries = ParseSources(update.ShapeSourcePaths, ShapeDictionarySourceParser.Parse);
            var symbolEntries = ParseSources(update.SymbolSourcePaths, SymbolDictionarySourceParser.Parse);
            var manifest = DictionaryPackageCompiler.Compile(
                phoneticEntries,
                stagingPath,
                update.Parameters,
                sources,
                shapeEntries,
                symbolEntries);

            DictionaryPackageLoader.Load(stagingPath);
            ReplacePackage(packagePath, stagingPath);
            return new LocalDictionaryPackageUpdateResult(manifest, ReusedExistingPackage: false);
        }
        finally
        {
            DeleteDirectoryIfExists(stagingPath);
        }
    }

    /// <summary>
    /// Exchanges the active package with its retained previous version.
    /// </summary>
    public static void Rollback(string packageDirectory)
    {
        ValidatePackageDirectory(packageDirectory);
        var packagePath = Path.GetFullPath(packageDirectory);
        var backupPath = GetBackupPath(packagePath);
        if (!Directory.Exists(backupPath))
        {
            throw new InvalidOperationException("No previous dictionary package is available for rollback.");
        }

        DictionaryPackageLoader.Load(backupPath);
        var transactionPath = $"{packagePath}.rollback-{Guid.NewGuid():N}";
        var activeMoved = false;
        try
        {
            if (Directory.Exists(packagePath))
            {
                Directory.Move(packagePath, transactionPath);
                activeMoved = true;
            }

            Directory.Move(backupPath, packagePath);
            if (activeMoved)
            {
                Directory.Move(transactionPath, backupPath);
            }
        }
        catch
        {
            if (!Directory.Exists(packagePath) && Directory.Exists(backupPath))
            {
                Directory.Move(backupPath, packagePath);
            }

            if (activeMoved && Directory.Exists(transactionPath) && !Directory.Exists(packagePath))
            {
                Directory.Move(transactionPath, packagePath);
            }

            throw;
        }
        finally
        {
            DeleteDirectoryIfExists(transactionPath);
        }
    }

    private static IReadOnlyList<TEntry> ParseSources<TEntry>(
        IReadOnlyList<string> sourcePaths,
        Func<TextReader, string, IReadOnlyList<TEntry>> parser)
    {
        var entries = new List<TEntry>();
        foreach (var sourcePath in NormalizeSourcePaths(sourcePaths))
        {
            using var reader = File.OpenText(sourcePath);
            entries.AddRange(parser(reader, sourcePath));
        }

        return entries;
    }

    private static bool TryReuseExistingPackage(
        string packagePath,
        DictionaryPackageParameters parameters,
        IReadOnlyList<DictionaryPackageSource> sources,
        out DictionaryPackageManifest manifest)
    {
        manifest = null!;
        if (!Directory.Exists(packagePath))
        {
            return false;
        }

        try
        {
            var existing = DictionaryPackageLoader.Inspect(packagePath).Manifest;
            if (!CanReuse(existing, parameters, sources))
            {
                return false;
            }

            manifest = existing;
            return true;
        }
        catch (DictionaryPackageException)
        {
            return false;
        }
    }

    private static bool CanReuse(
        DictionaryPackageManifest existing,
        DictionaryPackageParameters parameters,
        IReadOnlyList<DictionaryPackageSource> sources)
    {
        return existing.FormatVersion == DictionaryPackageManifest.CurrentFormatVersion
            && string.Equals(existing.PackageKind, DictionaryPackageManifest.ExpectedPackageKind, StringComparison.Ordinal)
            && string.Equals(existing.CompilerVersion, DictionaryPackageManifest.CurrentCompilerVersion, StringComparison.Ordinal)
            && string.Equals(existing.Parameters.InputScheme, parameters.InputScheme, StringComparison.Ordinal)
            && existing.Parameters.EnablePrefixIndex == parameters.EnablePrefixIndex
            && existing.Parameters.MaxPrefixCandidatesPerKey == parameters.MaxPrefixCandidatesPerKey
            && SourcesMatch(existing.Sources, sources);
    }

    private static bool SourcesMatch(
        IReadOnlyList<DictionaryPackageSource> existing,
        IReadOnlyList<DictionaryPackageSource> current)
    {
        if (existing.Count != current.Count)
        {
            return false;
        }

        for (var index = 0; index < existing.Count; index++)
        {
            var left = existing[index];
            var right = current[index];
            if (!string.Equals(left.Path, right.Path, StringComparison.Ordinal)
                || left.Length != right.Length
                || left.LastWriteTimeUtcTicks != right.LastWriteTimeUtcTicks)
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<DictionaryPackageSource> CreateSourceRecords(LocalDictionaryPackageUpdate update)
    {
        var paths = NormalizeSourcePaths(update.PhoneticSourcePaths)
            .Concat(NormalizeSourcePaths(update.ShapeSourcePaths))
            .Concat(NormalizeSourcePaths(update.SymbolSourcePaths))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var commonDirectory = FindCommonDirectory(paths);
        return paths
            .Select(path =>
            {
                var info = new FileInfo(path);
                return new DictionaryPackageSource(
                    Path.GetRelativePath(commonDirectory, path).Replace('\\', '/'),
                    info.Length,
                    info.LastWriteTimeUtc.Ticks);
            })
            .ToArray();
    }

    private static string FindCommonDirectory(IReadOnlyList<string> paths)
    {
        var commonDirectory = Path.GetDirectoryName(paths[0])!;
        while (paths.Any(path => !Path.GetFullPath(path).StartsWith(
                   commonDirectory + Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase)))
        {
            commonDirectory = Directory.GetParent(commonDirectory)?.FullName
                ?? Path.GetPathRoot(paths[0])!;
        }
        return commonDirectory;
    }

    private static IReadOnlyList<string> NormalizeSourcePaths(IReadOnlyList<string> sourcePaths)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);
        return sourcePaths
            .Select(path =>
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new ArgumentException("Dictionary source paths cannot be empty.", nameof(sourcePaths));
                }

                return Path.GetFullPath(path);
            })
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static void ReplacePackage(string packagePath, string stagingPath)
    {
        var backupPath = GetBackupPath(packagePath);
        var previousBackupPath = $"{backupPath}.previous-{Guid.NewGuid():N}";
        var previousBackupMoved = false;
        var activeMoved = false;
        try
        {
            if (Directory.Exists(backupPath))
            {
                Directory.Move(backupPath, previousBackupPath);
                previousBackupMoved = true;
            }

            if (Directory.Exists(packagePath))
            {
                Directory.Move(packagePath, backupPath);
                activeMoved = true;
            }

            Directory.Move(stagingPath, packagePath);
            DeleteDirectoryIfExists(previousBackupPath);
        }
        catch
        {
            if (!Directory.Exists(packagePath) && activeMoved && Directory.Exists(backupPath))
            {
                Directory.Move(backupPath, packagePath);
            }

            if (previousBackupMoved && !Directory.Exists(backupPath) && Directory.Exists(previousBackupPath))
            {
                Directory.Move(previousBackupPath, backupPath);
            }

            throw;
        }
        finally
        {
            DeleteDirectoryIfExists(previousBackupPath);
        }
    }

    private static string GetBackupPath(string packagePath) => $"{packagePath}.previous";

    private static void ValidatePackageDirectory(string packageDirectory)
    {
        if (string.IsNullOrWhiteSpace(packageDirectory))
        {
            throw new ArgumentException(nameof(packageDirectory), nameof(packageDirectory));
        }
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
