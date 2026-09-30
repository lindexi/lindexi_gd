using System.Globalization;
using System.Text;
using XiaoXiIme.Dictionary;

namespace XiaoXiIme.Cli;

internal static class SeWzcDictionarySourceConverter
{
    internal const string PhoneticOutputRelativePath = "phonetic/sewzc-default.phonetic.tsv";
    internal const string ProjectTermsOutputRelativePath = XiaoXiImeProjectTerms.OutputRelativePath;
    internal const string ShapeOutputRelativePath = "shape/sewzc-moqi.shape.tsv";
    internal const string SymbolOutputRelativePath = "symbols/sewzc-default.symbols.tsv";

    private static readonly string[] PhoneticSourceRelativePaths =
    [
        "phonetic/rime-frost-8105.dict.yaml",
        "phonetic/rime-frost-base.dict.yaml",
        "phonetic/rime-frost-corrections.dict.yaml",
        "phonetic/project-terms.dict.yaml",
    ];

    internal static void Convert(string sourceDirectory, string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory))
        {
            throw new ArgumentException("A source directory is required.", nameof(sourceDirectory));
        }

        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new ArgumentException("A target directory is required.", nameof(targetDirectory));
        }

        var sourceRoot = Path.GetFullPath(sourceDirectory);
        RejectRepositoryRelativeSnapshot(sourceRoot);
        var targetRoot = Path.GetFullPath(targetDirectory);
        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"Dictionary snapshot directory was not found: {sourceRoot}");
        }

        var stagingRoot = Path.Combine(Path.GetTempPath(), "XiaoXiIme.DictionarySourceConversion", Guid.NewGuid().ToString("N"));
        try
        {
            var phoneticOutput = Path.Combine(stagingRoot, PhoneticOutputRelativePath);
            var projectTermsOutput = Path.Combine(stagingRoot, ProjectTermsOutputRelativePath);
            var shapeOutput = Path.Combine(stagingRoot, ShapeOutputRelativePath);
            var symbolOutput = Path.Combine(stagingRoot, SymbolOutputRelativePath);

            WritePhoneticSource(sourceRoot, phoneticOutput);
            XiaoXiImeProjectTerms.WriteTo(projectTermsOutput);
            WriteShapeSource(RequiredSource(sourceRoot, "shape/moqi_chaifen.txt"), shapeOutput);
            WriteSymbolSource(RequiredSource(sourceRoot, "symbols/default.symbols.tsv"), symbolOutput);

            ValidateOutput(phoneticOutput, PhoneticDictionarySourceParser.Parse);
            ValidateOutput(projectTermsOutput, PhoneticDictionarySourceParser.Parse);
            ValidateOutput(shapeOutput, ShapeDictionarySourceParser.Parse);
            ValidateOutput(symbolOutput, SymbolDictionarySourceParser.Parse);

            InstallOutputs(
                targetRoot,
                (phoneticOutput, PhoneticOutputRelativePath),
                (projectTermsOutput, ProjectTermsOutputRelativePath),
                (shapeOutput, ShapeOutputRelativePath),
                (symbolOutput, SymbolOutputRelativePath));
        }
        finally
        {
            if (Directory.Exists(stagingRoot))
            {
                Directory.Delete(stagingRoot, true);
            }
        }
    }

    private static void WritePhoneticSource(string sourceRoot, string outputPath)
    {
        CreateOutputDirectory(outputPath);
        using var writer = CreateWriter(outputPath);
        WriteAttribution(writer, "phonetic");

        foreach (var relativePath in PhoneticSourceRelativePaths)
        {
            var sourcePath = RequiredSource(sourceRoot, relativePath);
            var lineNumber = 0;
            foreach (var rawLine in File.ReadLines(sourcePath))
            {
                lineNumber++;
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#') || line is "---" or "..." || !line.Contains('\t'))
                {
                    continue;
                }

                var columns = line.Split('\t', StringSplitOptions.TrimEntries);
                if (columns.Length < 3 || columns[0].Length == 0 || columns[1].Length == 0)
                {
                    throw new InvalidDataException($"Invalid phonetic entry at {sourcePath}:{lineNumber}.");
                }

                var frequency = columns.Skip(2)
                    .Select(column => int.TryParse(column, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : (int?) null)
                    .FirstOrDefault(value => value.HasValue);
                if (frequency is null)
                {
                    throw new InvalidDataException($"Missing phonetic frequency at {sourcePath}:{lineNumber}.");
                }

                writer.Write(columns[0]);
                writer.Write('\t');
                writer.Write(PhoneticDictionarySourceParser.CanonicalizeReading(columns[1]));
                writer.Write('\t');
                writer.WriteLine(frequency.Value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }

    private static void WriteShapeSource(string sourcePath, string outputPath)
    {
        CreateOutputDirectory(outputPath);
        using var writer = CreateWriter(outputPath);
        WriteAttribution(writer, "shape");
        foreach (var line in File.ReadLines(sourcePath))
        {
            writer.WriteLine(line);
        }
    }

    private static void WriteSymbolSource(string sourcePath, string outputPath)
    {
        CreateOutputDirectory(outputPath);
        using var writer = CreateWriter(outputPath);
        WriteAttribution(writer, "symbol");

        var candidatesByInput = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var seenByInput = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var rawLine in File.ReadLines(sourcePath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var columns = line.Split('\t', StringSplitOptions.TrimEntries);
            if (columns.Length < 2 || columns[0].Length == 0)
            {
                throw new InvalidDataException($"Invalid symbol entry in {sourcePath}.");
            }

            var input = columns[0];
            if (!candidatesByInput.TryGetValue(input, out var candidates))
            {
                candidates = [];
                candidatesByInput.Add(input, candidates);
                seenByInput.Add(input, new HashSet<string>(StringComparer.Ordinal));
            }

            foreach (var candidate in columns.Skip(1))
            {
                if (candidate.Length > 0 && candidates.Count < SymbolDictionarySourceParser.MaxCandidatesPerInput && seenByInput[input].Add(candidate))
                {
                    candidates.Add(candidate);
                }
            }
        }

        foreach (var entry in candidatesByInput.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            writer.Write(entry.Key);
            foreach (var candidate in entry.Value)
            {
                writer.Write('\t');
                writer.Write(candidate);
            }
            writer.WriteLine();
        }
    }

    private static void ValidateOutput<T>(string path, Func<TextReader, string, IReadOnlyList<T>> parser)
    {
        using var reader = File.OpenText(path);
        parser(reader, path);
    }

    private static string RequiredSource(string sourceRoot, string relativePath)
    {
        var path = Path.Combine(sourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required dictionary snapshot file was not found: {path}", path);
        }

        return path;
    }

    private static void RejectRepositoryRelativeSnapshot(string sourceRoot)
    {
        var current = new DirectoryInfo(sourceRoot);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ImeLab.slnx"))
                && Directory.Exists(Path.Combine(current.FullName, "src", "Ime.RimeShim.NativeAot")))
            {
                throw new InvalidOperationException(
                    "dictionary-convert-sewzc does not accept a SeWZC_IME repository path. Copy data/dictionaries to a decoupled snapshot first.");
            }

            current = current.Parent;
        }
    }

    private static void InstallOutputs(string targetRoot, params (string SourcePath, string RelativePath)[] outputs)
    {
        var transactionRoot = Path.Combine(Path.GetTempPath(), "XiaoXiIme.DictionarySourceInstallation", Guid.NewGuid().ToString("N"));
        var installedPaths = new List<string>();
        var backups = new List<(string BackupPath, string TargetPath)>();
        try
        {
            foreach (var output in outputs)
            {
                var targetPath = Path.Combine(targetRoot, output.RelativePath);
                CreateOutputDirectory(targetPath);
                if (File.Exists(targetPath))
                {
                    var backupPath = Path.Combine(transactionRoot, output.RelativePath);
                    CreateOutputDirectory(backupPath);
                    File.Move(targetPath, backupPath);
                    backups.Add((backupPath, targetPath));
                }

                File.Move(output.SourcePath, targetPath);
                installedPaths.Add(targetPath);
            }
        }
        catch
        {
            foreach (var installedPath in installedPaths)
            {
                File.Delete(installedPath);
            }

            foreach (var backup in backups)
            {
                CreateOutputDirectory(backup.TargetPath);
                File.Move(backup.BackupPath, backup.TargetPath);
            }

            throw;
        }
        finally
        {
            if (Directory.Exists(transactionRoot))
            {
                Directory.Delete(transactionRoot, true);
            }
        }
    }

    private static void CreateOutputDirectory(string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    }

    private static StreamWriter CreateWriter(string outputPath)
    {
        return new StreamWriter(outputPath, false, new UTF8Encoding(false));
    }

    private static void WriteAttribution(TextWriter writer, string sourceKind)
    {
        writer.WriteLine("# XiaoXiIme native dictionary source generated from the SeWZC dictionary snapshot.");
        writer.WriteLine($"# Source kind: {sourceKind}; attribution: SeWZC.");
    }
}
