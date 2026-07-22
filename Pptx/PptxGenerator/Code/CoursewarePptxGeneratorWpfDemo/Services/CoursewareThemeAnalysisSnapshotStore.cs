using System.Globalization;
using System.IO;
using System.Text.Json;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoreCoursewareExportJsonSerializerContext = CoursewarePptxGenerator.Core.Serialization.CoursewareExportJsonSerializerContext;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Persists and restores self-contained courseware theme-analysis snapshots.
/// </summary>
public sealed class CoursewareThemeAnalysisSnapshotStore : ICoursewareThemeAnalysisSnapshotStore
{
    private const string SnapshotDirectoryPrefix = "CoursewareThemeAnalysis_";
    private const string SlidesDirectoryName = "Slides";
    private const string ResourcesDirectoryName = "Resources";
    private const string CoursewareManifestFileName = "Courseware.json";
    private const string ResourcesManifestFileName = "Resources.json";
    private readonly DirectoryInfo _outputRootDirectory;
    private readonly Func<DateTimeOffset> _nowProvider;
    private readonly CoursewareFolderLoader _coursewareFolderLoader;
    private readonly CoursewareAnalysisSourceSnapshotBuilder _sourceSnapshotBuilder;
    private readonly CoursewareThemeValidator _themeValidator;

    /// <summary>
    /// Initializes a snapshot store that writes to the current process directory.
    /// </summary>
    public CoursewareThemeAnalysisSnapshotStore()
        : this(Directory.GetCurrentDirectory())
    {
    }

    /// <summary>
    /// Initializes a snapshot store with an explicit output root and time source.
    /// </summary>
    /// <param name="outputRootPath">The directory in which published snapshots are created.</param>
    /// <param name="nowProvider">The optional clock used for deterministic snapshot names and metadata.</param>
    public CoursewareThemeAnalysisSnapshotStore(
        string outputRootPath,
        Func<DateTimeOffset>? nowProvider = null)
        : this(
            outputRootPath,
            nowProvider,
            new CoursewareFolderLoader(),
            new CoursewareAnalysisSourceSnapshotBuilder(),
            new CoursewareThemeValidator())
    {
    }

    internal CoursewareThemeAnalysisSnapshotStore(
        string outputRootPath,
        Func<DateTimeOffset>? nowProvider,
        CoursewareFolderLoader coursewareFolderLoader,
        CoursewareAnalysisSourceSnapshotBuilder sourceSnapshotBuilder,
        CoursewareThemeValidator themeValidator)
    {
        if (string.IsNullOrWhiteSpace(outputRootPath))
        {
            throw new ArgumentException("快照输出根目录不能为空。", nameof(outputRootPath));
        }
        ArgumentNullException.ThrowIfNull(coursewareFolderLoader);
        ArgumentNullException.ThrowIfNull(sourceSnapshotBuilder);
        ArgumentNullException.ThrowIfNull(themeValidator);

        _outputRootDirectory = new DirectoryInfo(Path.GetFullPath(outputRootPath));
        _nowProvider = nowProvider ?? (() => DateTimeOffset.Now);
        _coursewareFolderLoader = coursewareFolderLoader;
        _sourceSnapshotBuilder = sourceSnapshotBuilder;
        _themeValidator = themeValidator;
    }

    /// <inheritdoc />
    public string ManifestFileName => CoursewareThemeAnalysisSnapshotManifest.FileName;

    /// <inheritdoc />
    public async Task<DirectoryInfo> SaveAsync(
        CoursewareInputPackage inputPackage,
        CoursewareThemeAnalysisResult analysisResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);
        ArgumentNullException.ThrowIfNull(analysisResult);
        cancellationToken.ThrowIfCancellationRequested();

        _outputRootDirectory.Create();
        var createdAt = _nowProvider();
        var publishedDirectory = GetAvailablePublishedDirectory(createdAt);
        var temporaryDirectory = new DirectoryInfo(Path.Join(
            _outputRootDirectory.FullName,
            $".{publishedDirectory.Name}.tmp_{Guid.NewGuid():N}"));
        temporaryDirectory.Create();

        try
        {
            var sourceSnapshot = _sourceSnapshotBuilder.Build(inputPackage, cancellationToken);
            await WriteNormalizedCoursewareAsync(
                    temporaryDirectory,
                    inputPackage,
                    createdAt,
                    cancellationToken)
                .ConfigureAwait(false);

            var manifest = new CoursewareThemeAnalysisSnapshotManifest
            {
                SchemaVersion = CoursewareThemeAnalysisSnapshotManifest.CurrentSchemaVersion,
                CreatedAt = createdAt,
                CoursewareName = inputPackage.CoursewareName,
                SlideCount = inputPackage.SlideCount,
                SourceFingerprint = sourceSnapshot.SourceFingerprint,
                AnalysisResult = analysisResult,
            };
            await WriteJsonAsync(
                    Path.Join(temporaryDirectory.FullName, ManifestFileName),
                    manifest,
                    CoursewareThemeAnalysisSnapshotJsonSerializerContext.Default.CoursewareThemeAnalysisSnapshotManifest,
                    cancellationToken)
                .ConfigureAwait(false);

            await LoadAsync(temporaryDirectory.FullName, cancellationToken).ConfigureAwait(false);
            Directory.Move(temporaryDirectory.FullName, publishedDirectory.FullName);
            return publishedDirectory;
        }
        catch
        {
            if (temporaryDirectory.Exists)
            {
                temporaryDirectory.Delete(recursive: true);
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CoursewareThemeAnalysisSnapshot> LoadAsync(
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("主题分析快照目录不能为空。", nameof(folderPath));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var snapshotDirectory = new DirectoryInfo(folderPath);
        if (!snapshotDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"主题分析快照目录不存在：{folderPath}");
        }

        var manifestPath = Path.Join(snapshotDirectory.FullName, ManifestFileName);
        var manifestFile = new FileInfo(manifestPath);
        if (!manifestFile.Exists)
        {
            throw new InvalidDataException($"缺少 {ManifestFileName}，无法识别主题分析快照目录。");
        }

        CoursewareThemeAnalysisSnapshotManifest? manifest;
        try
        {
            await using var stream = manifestFile.OpenRead();
            manifest = await JsonSerializer.DeserializeAsync(
                stream,
                CoursewareThemeAnalysisSnapshotJsonSerializerContext.Default.CoursewareThemeAnalysisSnapshotManifest,
                cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{ManifestFileName} 内容无法解析。", ex);
        }

        if (manifest is null)
        {
            throw new InvalidDataException($"{ManifestFileName} 内容为空或无法解析。");
        }

        var inputPackage = await _coursewareFolderLoader.LoadAsync(snapshotDirectory.FullName, cancellationToken)
            .ConfigureAwait(false);
        return ValidateSnapshot(snapshotDirectory, inputPackage, manifest, cancellationToken);
    }

    private async Task WriteNormalizedCoursewareAsync(
        DirectoryInfo snapshotDirectory,
        CoursewareInputPackage inputPackage,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var slidesDirectory = snapshotDirectory.CreateSubdirectory(SlidesDirectoryName);
        var resourcesDirectory = snapshotDirectory.CreateSubdirectory(ResourcesDirectoryName);
        var slideEntries = new CoursewareExportSlideEntry[inputPackage.Slides.Count];

        for (var position = 0; position < inputPackage.Slides.Count; position++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var slide = inputPackage.Slides[position];
            var markdownFileName = $"Slide{position:D3}.md";
            var screenshotFileName = $"Slide{position:D3}.jpg";
            var markdownRelativePath = $"{SlidesDirectoryName}/{markdownFileName}";
            var screenshotRelativePath = $"{SlidesDirectoryName}/{screenshotFileName}";

            await File.WriteAllTextAsync(
                    Path.Join(slidesDirectory.FullName, markdownFileName),
                    slide.MarkdownText,
                    cancellationToken)
                .ConfigureAwait(false);
            if (slide.ScreenshotFile is not null && slide.ScreenshotFile.Exists)
            {
                await CopyFileAsync(
                        slide.ScreenshotFile.FullName,
                        Path.Join(slidesDirectory.FullName, screenshotFileName),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            slideEntries[position] = new CoursewareExportSlideEntry
            {
                SlideIndex = slide.SlideIndex,
                SlideId = slide.SlideId,
                Width = slide.Width,
                Height = slide.Height,
                MarkdownFile = markdownRelativePath,
                ScreenshotFile = screenshotRelativePath,
            };
        }

        var resourceEntries = new CoursewareResourceEntry[inputPackage.Resources.Count];
        for (var position = 0; position < inputPackage.Resources.Count; position++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var resource = inputPackage.Resources[position];
            resourceEntries[position] = new CoursewareResourceEntry
            {
                ResourceId = resource.ResourceId,
                ResourceType = resource.ResourceType,
                ExportFile = resource.ExportFile,
            };

            if (resource.Exists
                && !string.IsNullOrWhiteSpace(resource.ResolvedFilePath)
                && File.Exists(resource.ResolvedFilePath))
            {
                await CopyFileAsync(
                        resource.ResolvedFilePath,
                        Path.Join(resourcesDirectory.FullName, resource.ExportFile!),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        var coursewareManifest = new CoursewareExportManifest
        {
            ExportVersion = 1,
            CreatedAt = createdAt,
            CoursewareName = inputPackage.CoursewareName,
            SlideCount = slideEntries.Length,
            Slides = slideEntries,
            ResourcesFile = $"{ResourcesDirectoryName}/{ResourcesManifestFileName}",
        };
        await WriteJsonAsync(
                Path.Join(snapshotDirectory.FullName, CoursewareManifestFileName),
                coursewareManifest,
                CoreCoursewareExportJsonSerializerContext.Default.CoursewareExportManifest,
                cancellationToken)
            .ConfigureAwait(false);
        await WriteJsonAsync(
                Path.Join(resourcesDirectory.FullName, ResourcesManifestFileName),
                resourceEntries,
                CoreCoursewareExportJsonSerializerContext.Default.CoursewareResourceEntryArray,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private CoursewareThemeAnalysisSnapshot ValidateSnapshot(
        DirectoryInfo snapshotDirectory,
        CoursewareInputPackage inputPackage,
        CoursewareThemeAnalysisSnapshotManifest manifest,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(
                manifest.SchemaVersion,
                CoursewareThemeAnalysisSnapshotManifest.CurrentSchemaVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"不支持的主题分析快照版本：{manifest.SchemaVersion}，"
                + $"当前仅支持 {CoursewareThemeAnalysisSnapshotManifest.CurrentSchemaVersion}。");
        }

        if (manifest.CreatedAt == default)
        {
            throw new InvalidDataException("主题分析快照缺少有效的创建时间。");
        }

        if (!string.Equals(manifest.CoursewareName, inputPackage.CoursewareName, StringComparison.Ordinal))
        {
            throw new InvalidDataException("主题分析快照中的课件名称与复制输入不一致。");
        }

        if (manifest.SlideCount <= 0 || manifest.SlideCount != inputPackage.SlideCount)
        {
            throw new InvalidDataException(
                $"主题分析快照声明页数 {manifest.SlideCount} 与复制输入页数 {inputPackage.SlideCount} 不一致。");
        }

        if (string.IsNullOrWhiteSpace(manifest.SourceFingerprint))
        {
            throw new InvalidDataException("主题分析快照缺少源事实指纹。");
        }

        var sourceSnapshot = _sourceSnapshotBuilder.Build(inputPackage, cancellationToken);
        if (!string.Equals(manifest.SourceFingerprint, sourceSnapshot.SourceFingerprint, StringComparison.Ordinal))
        {
            throw new InvalidDataException("主题分析快照的源事实指纹与复制输入不一致，文件可能已被修改。");
        }

        var analysisResult = manifest.AnalysisResult
            ?? throw new InvalidDataException("主题分析快照缺少完整的分析结果。");
        if (analysisResult.Theme is null
            || analysisResult.ReferenceCanvas is null
            || analysisResult.CapabilityStates is null)
        {
            throw new InvalidDataException("主题分析快照的分析结果对象图不完整。");
        }

        if (analysisResult.TotalSlideCount != inputPackage.SlideCount
            || analysisResult.AnalyzedSlideCount != inputPackage.SlideCount)
        {
            throw new InvalidDataException(
                $"主题分析快照的分析覆盖数 {analysisResult.AnalyzedSlideCount}/{analysisResult.TotalSlideCount} "
                + $"与复制输入页数 {inputPackage.SlideCount} 不一致。");
        }

        if (analysisResult.AnalyzedAt == default || analysisResult.Duration < TimeSpan.Zero)
        {
            throw new InvalidDataException("主题分析快照包含无效的分析时间信息。");
        }

        if (!string.Equals(analysisResult.Theme.SchemaVersion, CoursewareTheme.CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"不支持的课件主题版本：{analysisResult.Theme.SchemaVersion}，"
                + $"当前仅支持 {CoursewareTheme.CurrentSchemaVersion}。");
        }

        var dominantDimensions = inputPackage.Slides
            .GroupBy(slide => (slide.Width, slide.Height))
            .OrderByDescending(group => group.Count())
            .First().Key;
        var expectedReferenceCanvas = CoursewareCanvasAdapter.CreateDocumentContext(
            dominantDimensions.Width,
            dominantDimensions.Height);
        if (analysisResult.ReferenceCanvas.CanvasWidth != expectedReferenceCanvas.CanvasWidth
            || analysisResult.ReferenceCanvas.CanvasHeight != expectedReferenceCanvas.CanvasHeight)
        {
            throw new InvalidDataException(
                $"主题分析快照的参考画布 {analysisResult.ReferenceCanvas.CanvasWidth}×"
                + $"{analysisResult.ReferenceCanvas.CanvasHeight} 与复制输入主画布 "
                + $"{expectedReferenceCanvas.CanvasWidth}×{expectedReferenceCanvas.CanvasHeight} 不一致。");
        }

        CoursewareThemeValidationResult validationResult;
        try
        {
            validationResult = _themeValidator.Validate(
                analysisResult.Theme,
                dominantDimensions.Width,
                dominantDimensions.Height);
        }
        catch (Exception ex) when (ex is NullReferenceException or ArgumentException)
        {
            throw new InvalidDataException("主题分析快照的主题对象图不完整。", ex);
        }

        if (!validationResult.IsValid)
        {
            throw new InvalidDataException(
                "主题分析快照中的主题未通过校验：" + string.Join("；", validationResult.Errors));
        }

        return new CoursewareThemeAnalysisSnapshot
        {
            SnapshotDirectory = snapshotDirectory,
            InputPackage = inputPackage,
            Manifest = manifest,
        };
    }

    private DirectoryInfo GetAvailablePublishedDirectory(DateTimeOffset createdAt)
    {
        var baseName = SnapshotDirectoryPrefix + createdAt.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        for (var suffix = 0; ; suffix++)
        {
            var directoryName = suffix == 0 ? baseName : $"{baseName}_{suffix}";
            var candidate = new DirectoryInfo(Path.Join(_outputRootDirectory.FullName, directoryName));
            if (!candidate.Exists)
            {
                return candidate;
            }
        }
    }

    private static async Task WriteJsonAsync<T>(
        string filePath,
        T value,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);
        await JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
    }

    private static async Task CopyFileAsync(
        string sourceFilePath,
        string destinationFilePath,
        CancellationToken cancellationToken)
    {
        await using var sourceStream = new FileStream(
            sourceFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
        await using var destinationStream = new FileStream(
            destinationFilePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        await sourceStream.CopyToAsync(destinationStream, 81920, cancellationToken).ConfigureAwait(false);
    }
}
