using XiaoXiIme.Dictionary;

namespace XiaoXiIme.Cli;

internal static class LocalDictionaryCommands
{
    internal static int Update(DictionaryUpdateOptions options, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (string.IsNullOrWhiteSpace(options.SourceDirectory) || string.IsNullOrWhiteSpace(options.PackageDirectory))
        {
            error.WriteLine("dictionary-update requires <source-directory> <package-directory>.");
            return 2;
        }

        try
        {
            var sourceDirectory = Path.GetFullPath(options.SourceDirectory);
            if (!Directory.Exists(sourceDirectory))
            {
                error.WriteLine($"Dictionary source directory was not found: {sourceDirectory}");
                return 3;
            }

            var phoneticSources = FindSources(sourceDirectory, "*.phonetic.tsv");
            if (phoneticSources.Count == 0)
            {
                error.WriteLine("No *.phonetic.tsv source files were found.");
                return 3;
            }

            var result = LocalDictionaryPackageManager.UpdateWithReuse(
                new LocalDictionaryPackageUpdate
                {
                    PhoneticSourcePaths = phoneticSources,
                    ShapeSourcePaths = FindSources(sourceDirectory, "*.shape.tsv"),
                    SymbolSourcePaths = FindSources(sourceDirectory, "*.symbols.tsv"),
                    Parameters = new DictionaryPackageParameters { InputScheme = options.Scheme },
                },
                options.PackageDirectory);

            output.WriteLine(result.ReusedExistingPackage
                ? $"Dictionary package reused: {Path.GetFullPath(options.PackageDirectory)}"
                : $"Dictionary package updated: {Path.GetFullPath(options.PackageDirectory)}");
            output.WriteLine($"Input scheme: {result.Manifest.Parameters.InputScheme}");
            output.WriteLine($"Candidates: {result.Manifest.Counts.Candidates}");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or DictionarySourceException or DictionaryPackageException)
        {
            error.WriteLine(exception.Message);
            return 4;
        }
    }

    internal static int Rollback(DictionaryRollbackOptions options, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (string.IsNullOrWhiteSpace(options.PackageDirectory))
        {
            error.WriteLine("dictionary-rollback requires <package-directory>.");
            return 2;
        }

        try
        {
            LocalDictionaryPackageManager.Rollback(options.PackageDirectory);
            output.WriteLine($"Dictionary package rolled back: {Path.GetFullPath(options.PackageDirectory)}");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or DictionaryPackageException)
        {
            error.WriteLine(exception.Message);
            return 4;
        }
    }

    internal static int ConvertSeWzc(DictionaryConvertSeWzcOptions options, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (string.IsNullOrWhiteSpace(options.SourceDirectory) || string.IsNullOrWhiteSpace(options.TargetDirectory))
        {
            error.WriteLine("dictionary-convert-sewzc requires <source-directory> <target-directory>.");
            return 2;
        }

        try
        {
            SeWzcDictionarySourceConverter.Convert(options.SourceDirectory, options.TargetDirectory);
            output.WriteLine($"SeWZC dictionary snapshot converted: {Path.GetFullPath(options.TargetDirectory)}");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or DictionarySourceException)
        {
            error.WriteLine(exception.Message);
            return 4;
        }
    }

    internal static int BuildPackages(DictionaryBuildPackagesOptions options, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (string.IsNullOrWhiteSpace(options.SourceDirectory) || string.IsNullOrWhiteSpace(options.HostOutputDirectory))
        {
            error.WriteLine("dictionary-build-packages requires <source-directory> <host-output-directory>.");
            return 2;
        }

        try
        {
            DefaultDictionaryPackageBuilder.Build(options.SourceDirectory, options.HostOutputDirectory);
            var verificationError = IntegrationTestRunner.VerifyHostDictionaryPackages(options.HostOutputDirectory);
            if (verificationError is not null)
            {
                error.WriteLine(verificationError);
                return 4;
            }

            output.WriteLine($"Dictionary packages built: {Path.GetFullPath(options.HostOutputDirectory)}");
            output.WriteLine($"Full Pinyin package: {Path.Combine(Path.GetFullPath(options.HostOutputDirectory), DefaultDictionaryPackageBuilder.FullPinyinPackageDirectoryName)}");
            output.WriteLine($"Xiaohe package: {Path.Combine(Path.GetFullPath(options.HostOutputDirectory), DefaultDictionaryPackageBuilder.XiaoheDoublePinyinPackageRelativePath)}");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or DictionarySourceException or DictionaryPackageException)
        {
            error.WriteLine(exception.Message);
            return 4;
        }
    }

    internal static int Inspect(DictionaryInspectOptions options, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (string.IsNullOrWhiteSpace(options.PackageDirectory))
        {
            error.WriteLine("dictionary-inspect requires <package-directory>.");
            return 2;
        }

        try
        {
            var inspection = DictionaryPackageLoader.Inspect(options.PackageDirectory);
            if (options.Json)
            {
                output.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                    CreateInspectReport(inspection),
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }

            output.WriteLine($"Dictionary package: {inspection.PackagePath}");
            output.WriteLine($"Package kind: {inspection.Manifest.PackageKind}");
            output.WriteLine($"Format version: {inspection.Manifest.FormatVersion}");
            output.WriteLine($"Compiler version: {inspection.Manifest.CompilerVersion}");
            output.WriteLine($"Input scheme: {inspection.Manifest.Parameters.InputScheme}");
            output.WriteLine($"Candidates: {inspection.Manifest.Counts.Candidates}");
            output.WriteLine($"Exact keys: {inspection.Manifest.Counts.ExactKeys}");
            output.WriteLine($"Prefix keys: {inspection.Manifest.Counts.PrefixKeys}");
            output.WriteLine($"Shape entries: {inspection.Manifest.Counts.ShapeEntries}");
            output.WriteLine($"Symbol inputs: {inspection.Manifest.Counts.SymbolInputs}");
            output.WriteLine($"Attribution: {DictionaryAttribution.SourceName}");
            output.WriteLine(DictionaryAttribution.Notice);
            output.WriteLine("Validation: passed");
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or DictionaryPackageException)
        {
            error.WriteLine(exception.Message);
            return 4;
        }
    }

    private static object CreateInspectReport(DictionaryPackageInspection inspection)
    {
        return new
        {
            packagePath = inspection.PackagePath,
            packageKind = inspection.Manifest.PackageKind,
            formatVersion = inspection.Manifest.FormatVersion,
            compilerVersion = inspection.Manifest.CompilerVersion,
            createdBy = inspection.Manifest.CreatedBy,
            inputScheme = inspection.Manifest.Parameters.InputScheme,
            enablePrefixIndex = inspection.Manifest.Parameters.EnablePrefixIndex,
            maxPrefixCandidatesPerKey = inspection.Manifest.Parameters.MaxPrefixCandidatesPerKey,
            counts = inspection.Manifest.Counts,
            sources = inspection.Manifest.Sources,
            shards = inspection.Manifest.Shards,
            attribution = DictionaryAttribution.SourceName,
            attributionNotice = DictionaryAttribution.Notice,
            validation = "passed",
        };
    }

    private static IReadOnlyList<string> FindSources(string sourceDirectory, string pattern)
    {
        return Directory.EnumerateFiles(sourceDirectory, pattern, SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
