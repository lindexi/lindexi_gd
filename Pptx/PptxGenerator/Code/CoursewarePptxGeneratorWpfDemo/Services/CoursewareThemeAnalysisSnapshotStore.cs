using System.IO;
using System.Text.Json;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Saves and restores self-contained version 2 theme-analysis snapshots.
/// </summary>
public sealed class CoursewareThemeAnalysisSnapshotStore : ICoursewareThemeAnalysisSnapshotStore
{
    private const string SnapshotManifestFileName = "CoursewareThemeAnalysis.json";
    private const string CoursewareManifestFileName = "Courseware.json";
    private const string ThemeFileRelativePath = "Theme/Theme.json";
    private const string SnapshotDirectoryNamePrefix = "CoursewareThemeAnalysis_";
    private readonly string _outputRootPath;
    private readonly DateTimeOffset? _fixedTimestamp;
    private readonly CoursewareFolderLoader _coursewareFolderLoader;
    private readonly CoursewareThemeValidator _themeValidator;

    /// <summary>
    /// Initializes a snapshot store that writes to the current process directory.
    /// </summary>
    public CoursewareThemeAnalysisSnapshotStore()
        : this(Environment.CurrentDirectory)
    {
    }

    /// <summary>
    /// Initializes a snapshot store with an explicit output root.
    /// </summary>
    public CoursewareThemeAnalysisSnapshotStore(string outputRootPath)
        : this(outputRootPath, fixedTimestamp: null, new CoursewareFolderLoader(), new CoursewareThemeValidator())
    {
    }

    internal CoursewareThemeAnalysisSnapshotStore(
        string outputRootPath,
        DateTimeOffset fixedTimestamp)
        : this(outputRootPath, fixedTimestamp, new CoursewareFolderLoader(), new CoursewareThemeValidator())
    {
    }

    private CoursewareThemeAnalysisSnapshotStore(
        string outputRootPath,
        DateTimeOffset? fixedTimestamp,
        CoursewareFolderLoader coursewareFolderLoader,
        CoursewareThemeValidator themeValidator)
    {
        if (string.IsNullOrWhiteSpace(outputRootPath))
        {
            throw new ArgumentException("快照输出目录不能为空。", nameof(outputRootPath));
        }

        ArgumentNullException.ThrowIfNull(coursewareFolderLoader);
        ArgumentNullException.ThrowIfNull(themeValidator);
        _outputRootPath = Path.GetFullPath(outputRootPath);
        _fixedTimestamp = fixedTimestamp;
        _coursewareFolderLoader = coursewareFolderLoader;
        _themeValidator = themeValidator;
    }

    /// <inheritdoc />
    public string ManifestFileName => SnapshotManifestFileName;

    /// <inheritdoc />
    public async Task<DirectoryInfo> SaveAsync(
        CoursewareInputPackage inputPackage,
        CoursewareThemeAnalysisResult analysisResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);
        ArgumentNullException.ThrowIfNull(analysisResult);
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(_outputRootPath);
        var temporaryDirectoryPath = Path.Join(
            _outputRootPath,
            $".{SnapshotDirectoryNamePrefix}tmp_{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(temporaryDirectoryPath);
            await CopyCoursewareAsync(inputPackage, temporaryDirectoryPath, cancellationToken).ConfigureAwait(false);
            await WriteJsonAsync(
                ResolvePathUnderRoot(temporaryDirectoryPath, ThemeFileRelativePath),
                analysisResult.Theme,
                CoursewareExportJsonSerializerContext.Default.CoursewareTheme,
                cancellationToken).ConfigureAwait(false);

            var createdAt = _fixedTimestamp ?? DateTimeOffset.Now;
            var manifest = new CoursewareThemeAnalysisSnapshotManifest
            {
                CreatedAt = createdAt,
                CoursewareManifestFile = CoursewareManifestFileName,
                ThemeFile = ThemeFileRelativePath,
            };
            await WriteJsonAsync(
                Path.Join(temporaryDirectoryPath, SnapshotManifestFileName),
                manifest,
                CoursewareExportJsonSerializerContext.Default.CoursewareThemeAnalysisSnapshotManifest,
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            var publishedPath = PublishTemporaryDirectory(temporaryDirectoryPath, createdAt);
            temporaryDirectoryPath = string.Empty;
            return new DirectoryInfo(publishedPath);
        }
        finally
        {
            if (!string.IsNullOrEmpty(temporaryDirectoryPath) && Directory.Exists(temporaryDirectoryPath))
            {
                Directory.Delete(temporaryDirectoryPath, recursive: true);
            }
        }
    }

    /// <inheritdoc />
    public async Task<CoursewareThemeAnalysisSnapshot> LoadAsync(
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("快照目录不能为空。", nameof(folderPath));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var snapshotDirectory = new DirectoryInfo(folderPath);
        if (!snapshotDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"快照目录不存在：{folderPath}");
        }

        var manifestPath = Path.Join(snapshotDirectory.FullName, SnapshotManifestFileName);
        var manifest = await ReadAndValidateManifestAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        var coursewareManifestPath = ResolveExistingFileUnderRoot(
            snapshotDirectory.FullName,
            manifest.CoursewareManifestFile,
            "课件清单");
        if (!string.Equals(coursewareManifestPath, Path.Join(snapshotDirectory.FullName, CoursewareManifestFileName), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"快照 v2 的课件清单必须位于 {CoursewareManifestFileName}。");
        }

        var themePath = ResolveExistingFileUnderRoot(snapshotDirectory.FullName, manifest.ThemeFile, "Theme 2.0");
        var inputPackage = await _coursewareFolderLoader.LoadAsync(snapshotDirectory.FullName, cancellationToken)
            .ConfigureAwait(false);
        var theme = await ReadJsonAsync(
                themePath,
                CoursewareExportJsonSerializerContext.Default.CoursewareTheme,
                "Theme 2.0",
                cancellationToken)
            .ConfigureAwait(false);

        var firstSlide = inputPackage.Slides[0];
        var documentContext = CoursewareCanvasAdapter.CreateDocumentContext(firstSlide);
        var availableResourceIds = inputPackage.Resources
            .Where(resource => resource.Exists && !string.IsNullOrWhiteSpace(resource.ResourceId))
            .Select(resource => resource.ResourceId!)
            .ToHashSet(StringComparer.Ordinal);
        var validationResult = await _themeValidator.ValidateAsync(
                theme,
                documentContext,
                availableResourceIds,
                cancellationToken)
            .ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            throw new InvalidDataException($"快照 v2 的 Theme 2.0 无效：{string.Join("；", validationResult.Errors)}");
        }

        return new CoursewareThemeAnalysisSnapshot
        {
            SnapshotDirectory = snapshotDirectory,
            InputPackage = inputPackage,
            AnalysisResult = new CoursewareThemeAnalysisResult { Theme = theme },
        };
    }

    private static async Task CopyCoursewareAsync(
        CoursewareInputPackage inputPackage,
        string destinationRootPath,
        CancellationToken cancellationToken)
    {
        var sourceManifestPath = Path.Join(inputPackage.RootDirectory.FullName, CoursewareManifestFileName);
        var sourceManifest = await ReadJsonAsync(
                sourceManifestPath,
                CoursewareExportJsonSerializerContext.Default.CoursewareExportManifest,
                "Courseware.json",
                cancellationToken)
            .ConfigureAwait(false);
        await CopyFileAsync(sourceManifestPath, Path.Join(destinationRootPath, CoursewareManifestFileName), cancellationToken)
            .ConfigureAwait(false);

        foreach (var slide in inputPackage.Slides)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var markdownRelativePath = GetSafeRelativePath(inputPackage.RootDirectory.FullName, slide.MarkdownFile.FullName);
            await CopyFileAsync(
                    slide.MarkdownFile.FullName,
                    ResolvePathUnderRoot(destinationRootPath, markdownRelativePath),
                    cancellationToken)
                .ConfigureAwait(false);
            if (slide.ScreenshotFile is { Exists: true } screenshotFile)
            {
                var screenshotRelativePath = GetSafeRelativePath(inputPackage.RootDirectory.FullName, screenshotFile.FullName);
                await CopyFileAsync(
                        screenshotFile.FullName,
                        ResolvePathUnderRoot(destinationRootPath, screenshotRelativePath),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (string.IsNullOrWhiteSpace(sourceManifest.ResourcesFile))
        {
            throw new InvalidDataException("Courseware.json 缺少 ResourcesFile。");
        }

        var resourcesIndexPath = ResolvePathUnderRoot(inputPackage.RootDirectory.FullName, sourceManifest.ResourcesFile);
        await CopyFileAsync(
                resourcesIndexPath,
                ResolvePathUnderRoot(destinationRootPath, sourceManifest.ResourcesFile),
                cancellationToken)
            .ConfigureAwait(false);
        foreach (var resource in inputPackage.Resources.Where(resource => resource.Exists))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(resource.ResolvedFilePath))
            {
                throw new InvalidDataException($"资源 {resource.ResourceId} 缺少可复制的本地路径。");
            }

            var resourceRelativePath = GetSafeRelativePath(inputPackage.RootDirectory.FullName, resource.ResolvedFilePath);
            await CopyFileAsync(
                    resource.ResolvedFilePath,
                    ResolvePathUnderRoot(destinationRootPath, resourceRelativePath),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private string PublishTemporaryDirectory(string temporaryDirectoryPath, DateTimeOffset createdAt)
    {
        var baseName = SnapshotDirectoryNamePrefix + createdAt.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture);
        for (var suffix = 0; ; suffix++)
        {
            var directoryName = suffix == 0 ? baseName : $"{baseName}_{suffix}";
            var destinationPath = Path.Join(_outputRootPath, directoryName);
            try
            {
                Directory.Move(temporaryDirectoryPath, destinationPath);
                return destinationPath;
            }
            catch (IOException) when (Directory.Exists(destinationPath))
            {
            }
        }
    }

    private static async Task<CoursewareThemeAnalysisSnapshotManifest> ReadAndValidateManifestAsync(
        string manifestPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"缺少快照清单 {SnapshotManifestFileName}。", manifestPath);
        }

        JsonDocument document;
        try
        {
            await using var stream = File.OpenRead(manifestPath);
            document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"快照清单 {SnapshotManifestFileName} 无法解析。", exception);
        }

        using (document)
        {
            var version = ReadManifestVersion(document.RootElement);
            if (version != CoursewareThemeAnalysisSnapshotManifest.CurrentSchemaVersion)
            {
                throw new InvalidDataException(
                    $"不支持的主题分析快照版本：{version}，当前仅支持 v{CoursewareThemeAnalysisSnapshotManifest.CurrentSchemaVersion}。");
            }

            try
            {
                return document.RootElement.Deserialize(
                           CoursewareExportJsonSerializerContext.Default.CoursewareThemeAnalysisSnapshotManifest)
                       ?? throw new InvalidDataException($"快照清单 {SnapshotManifestFileName} 内容为空。");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"快照清单 {SnapshotManifestFileName} 不符合 v2 格式。", exception);
            }
        }
    }

    private static int ReadManifestVersion(JsonElement rootElement)
    {
        if (TryReadVersion(rootElement, "Version", out var version)
            || TryReadVersion(rootElement, "SchemaVersion", out version))
        {
            return version;
        }

        return 1;
    }

    private static bool TryReadVersion(JsonElement rootElement, string propertyName, out int version)
    {
        version = default;
        if (!rootElement.TryGetProperty(propertyName, out var versionElement))
        {
            return false;
        }

        if (versionElement.ValueKind == JsonValueKind.Number && versionElement.TryGetInt32(out version))
        {
            return true;
        }

        if (versionElement.ValueKind == JsonValueKind.String)
        {
            var text = versionElement.GetString();
            if (text is not null && int.TryParse(text.TrimStart('v', 'V'), out version))
            {
                return true;
            }
        }

        throw new InvalidDataException($"主题分析快照版本字段 {propertyName} 无效：{versionElement.GetRawText()}");
    }

    private static async Task<T> ReadJsonAsync<T>(
        string filePath,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo,
        string displayName,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync(stream, jsonTypeInfo, cancellationToken).ConfigureAwait(false)
                   ?? throw new InvalidDataException($"{displayName} 文件内容为空。");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"{displayName} 文件无法解析。", exception);
        }
    }

    private static async Task WriteJsonAsync<T>(
        string filePath,
        T value,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
    }

    private static async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"快照源文件不存在：{Path.GetFileName(sourcePath)}", sourcePath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        await using var destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await source.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveExistingFileUnderRoot(string rootPath, string relativePath, string displayName)
    {
        var resolvedPath = ResolvePathUnderRoot(rootPath, relativePath);
        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException($"快照 v2 缺少{displayName}文件：{relativePath}", resolvedPath);
        }

        return resolvedPath;
    }

    private static string ResolvePathUnderRoot(string rootPath, string relativePath)
    {
        ValidateRelativePath(relativePath);
        var fullRootPath = Path.GetFullPath(rootPath);
        var resolvedPath = Path.GetFullPath(Path.Join(fullRootPath, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
        var rootPathWithSeparator = Path.EndsInDirectorySeparator(fullRootPath)
            ? fullRootPath
            : fullRootPath + Path.DirectorySeparatorChar;
        if (!resolvedPath.StartsWith(rootPathWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"快照路径越界：{relativePath}");
        }

        return resolvedPath;
    }

    private static string GetSafeRelativePath(string rootPath, string filePath)
    {
        var relativePath = Path.GetRelativePath(rootPath, filePath);
        _ = ResolvePathUnderRoot(rootPath, relativePath);
        return relativePath;
    }

    private static void ValidateRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException($"快照路径必须是非空相对路径：{relativePath}");
        }

        var segments = relativePath.Split(['/', '\\'], StringSplitOptions.None);
        if (segments.Any(segment => string.IsNullOrEmpty(segment) || segment is "." or ".."))
        {
            throw new InvalidDataException($"快照路径不能包含空、. 或 .. 路径片段：{relativePath}");
        }
    }
}