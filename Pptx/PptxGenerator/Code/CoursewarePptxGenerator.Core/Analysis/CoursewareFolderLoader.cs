using System.IO;
using System.Text.Json;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGenerator.Core.Serialization;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Loads and validates a courseware Markdown export directory without depending on UI frameworks.
/// </summary>
public sealed class CoursewareFolderLoader
{
    private const int SupportedExportVersion = 1;
    private const string ManifestFileName = "Courseware.json";
    private static readonly HashSet<string> SupportedResourceTypes = new(StringComparer.Ordinal)
    {
        "image",
        "audio",
        "video",
    };

    /// <summary>
    /// Loads a courseware export directory.
    /// </summary>
    public async Task<CoursewareInputPackage> LoadAsync(
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("课件目录不能为空。", nameof(folderPath));
        }

        var rootDirectory = new DirectoryInfo(folderPath);
        if (!rootDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"课件目录不存在：{folderPath}");
        }

        var warnings = new List<CoursewareLoadWarning>();
        var manifestFile = new FileInfo(Path.Join(rootDirectory.FullName, ManifestFileName));
        if (!manifestFile.Exists)
        {
            throw new InvalidDataException("缺少 Courseware.json，无法识别课件导出目录。");
        }

        var manifest = await ReadJsonAsync(
                manifestFile,
                CoursewareExportJsonSerializerContext.Default.CoursewareExportManifest,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidDataException("Courseware.json 内容为空或无法解析。");
        ValidateManifest(manifest, warnings);

        var slides = await LoadSlidesAsync(rootDirectory, manifest.Slides, warnings, cancellationToken)
            .ConfigureAwait(false);
        if (slides.Count == 0)
        {
            throw new InvalidDataException("课件导出目录中没有可加载的页面 Markdown。");
        }

        var resources = await LoadResourcesAsync(
                rootDirectory,
                manifest.ResourcesFile!,
                warnings,
                cancellationToken)
            .ConfigureAwait(false);
        var coursewareName = string.IsNullOrWhiteSpace(manifest.CoursewareName)
            ? rootDirectory.Name
            : manifest.CoursewareName.Trim();

        return new CoursewareInputPackage
        {
            RootDirectory = rootDirectory,
            CoursewareName = coursewareName,
            Slides = slides,
            Resources = resources,
            Warnings = warnings,
        };
    }

    private static async Task<T?> ReadJsonAsync<T>(
        FileInfo file,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenRead();
        return await JsonSerializer.DeserializeAsync(stream, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateManifest(CoursewareExportManifest manifest, List<CoursewareLoadWarning> warnings)
    {
        if (manifest.ExportVersion != SupportedExportVersion)
        {
            throw new InvalidDataException(
                $"不支持的课件导出格式版本：{manifest.ExportVersion}，当前仅支持 {SupportedExportVersion}。");
        }

        if (manifest.Slides.Count == 0)
        {
            throw new InvalidDataException("Courseware.json 中没有页面列表。");
        }

        if (manifest.SlideCount != manifest.Slides.Count)
        {
            throw new InvalidDataException(
                $"课件声明页数 {manifest.SlideCount} 与页面列表数量 {manifest.Slides.Count} 不一致。");
        }

        if (manifest.Slides.Any(entry => entry.SlideIndex < 0))
        {
            throw new InvalidDataException("Courseware.json 中的 SlideIndex 不能为负数。");
        }

        if (manifest.Slides.Select(entry => entry.SlideIndex).Distinct().Count() != manifest.Slides.Count)
        {
            throw new InvalidDataException("Courseware.json 中包含重复的 SlideIndex。");
        }

        if (string.IsNullOrWhiteSpace(manifest.ResourcesFile))
        {
            throw new InvalidDataException("Courseware.json 缺少 ResourcesFile。");
        }

        ValidateRelativePath(manifest.ResourcesFile);
    }

    private static async Task<IReadOnlyList<CoursewareSlideInput>> LoadSlidesAsync(
        DirectoryInfo rootDirectory,
        IReadOnlyList<CoursewareExportSlideEntry> entries,
        List<CoursewareLoadWarning> warnings,
        CancellationToken cancellationToken)
    {
        var slides = new List<CoursewareSlideInput>(entries.Count);
        for (var slidePosition = 0; slidePosition < entries.Count; slidePosition++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = entries[slidePosition];
            var pageNumber = slidePosition + 1;
            var slideWarnings = new List<CoursewareLoadWarning>();

            if (string.IsNullOrWhiteSpace(entry.SlideId))
            {
                throw new InvalidDataException($"第 {pageNumber} 个页面条目缺少 SlideId。");
            }

            if (string.IsNullOrWhiteSpace(entry.MarkdownFile))
            {
                throw new InvalidDataException($"第 {pageNumber} 个页面条目缺少 MarkdownFile。");
            }

            ValidateSlideFilePath(entry.MarkdownFile, slidePosition, ".md", "MarkdownFile");
            var markdownPath = ResolvePathUnderRoot(rootDirectory, entry.MarkdownFile);
            var markdownFile = new FileInfo(markdownPath);
            if (!markdownFile.Exists)
            {
                throw new FileNotFoundException(
                    $"第 {pageNumber} 个页面条目的 Markdown 文件不存在：{entry.MarkdownFile}",
                    markdownPath);
            }

            var markdownText = await File.ReadAllTextAsync(markdownFile.FullName, cancellationToken)
                .ConfigureAwait(false);
            if (entry.Width <= 0 || entry.Height <= 0)
            {
                throw new InvalidDataException($"第 {pageNumber} 个页面条目的尺寸必须大于 0。");
            }

            FileInfo? screenshotFile = null;
            if (string.IsNullOrWhiteSpace(entry.ScreenshotFile))
            {
                throw new InvalidDataException($"第 {pageNumber} 个页面条目缺少 ScreenshotFile。");
            }

            ValidateSlideFilePath(entry.ScreenshotFile, slidePosition, ".jpg", "ScreenshotFile");
            var screenshotPath = ResolvePathUnderRoot(rootDirectory, entry.ScreenshotFile);
            var candidateScreenshotFile = new FileInfo(screenshotPath);
            if (candidateScreenshotFile.Exists)
            {
                screenshotFile = candidateScreenshotFile;
            }
            else
            {
                AddSlideWarning(
                    slideWarnings,
                    warnings,
                    "MissingScreenshotFile",
                    $"第 {pageNumber} 个页面条目的截图文件不存在。",
                    entry.ScreenshotFile,
                    entry.SlideIndex);
            }

            slides.Add(new CoursewareSlideInput
            {
                SlideIndex = entry.SlideIndex,
                PageNumber = pageNumber,
                SlideId = entry.SlideId.Trim(),
                Width = entry.Width,
                Height = entry.Height,
                MarkdownFile = markdownFile,
                ScreenshotFile = screenshotFile,
                MarkdownText = markdownText,
                Warnings = slideWarnings,
            });
        }

        return slides;
    }

    private static async Task<IReadOnlyList<CoursewareResourceEntry>> LoadResourcesAsync(
        DirectoryInfo rootDirectory,
        string resourcesFilePath,
        List<CoursewareLoadWarning> warnings,
        CancellationToken cancellationToken)
    {
        var resourcesIndexPath = ResolvePathUnderRoot(rootDirectory, resourcesFilePath);
        var resourcesIndexFile = new FileInfo(resourcesIndexPath);
        if (!resourcesIndexFile.Exists)
        {
            throw new FileNotFoundException("资源索引文件不存在。", resourcesIndexPath);
        }

        var resources = await ReadResourcesAsync(resourcesIndexFile, cancellationToken).ConfigureAwait(false);
        var resourcesDirectory = resourcesIndexFile.Directory ?? rootDirectory;
        var resolvedResources = new List<CoursewareResourceEntry>(resources.Count);
        foreach (var resource in resources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(resource.ResourceId))
            {
                throw new InvalidDataException("资源条目缺少 ResourceId。");
            }

            if (!SupportedResourceTypes.Contains(resource.ResourceType ?? string.Empty))
            {
                throw new InvalidDataException(
                    $"资源 {resource.ResourceId} 的 ResourceType 无效：{resource.ResourceType}");
            }

            if (string.IsNullOrWhiteSpace(resource.ExportFile))
            {
                throw new InvalidDataException($"资源 {resource.ResourceId} 缺少 ExportFile。");
            }

            ValidateResourceFileName(resource.ExportFile);
            var resolvedPath = ResolvePathUnderDirectory(resourcesDirectory, resource.ExportFile);
            var exists = File.Exists(resolvedPath);
            if (!exists)
            {
                warnings.Add(new CoursewareLoadWarning(
                    "MissingResourceFile",
                    "资源文件不存在。",
                    resource.ExportFile));
            }

            resolvedResources.Add(resource with
            {
                ResolvedFilePath = resolvedPath,
                Exists = exists,
            });
        }

        if (resolvedResources.Select(resource => resource.ResourceId)
            .Distinct(StringComparer.Ordinal)
            .Count() != resolvedResources.Count)
        {
            throw new InvalidDataException("Resources.json 包含重复的 ResourceId。");
        }

        return resolvedResources;
    }

    private static async Task<IReadOnlyList<CoursewareResourceEntry>> ReadResourcesAsync(
        FileInfo resourcesIndexFile,
        CancellationToken cancellationToken)
    {
        await using var stream = resourcesIndexFile.OpenRead();
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            var arrayJson = document.RootElement.GetRawText();
            return JsonSerializer.Deserialize(
                       arrayJson,
                       CoursewareExportJsonSerializerContext.Default.CoursewareResourceEntryArray)
                   ?? [];
        }

        throw new InvalidDataException("Resources.json 必须是资源条目数组。");
    }

    private static string ResolvePathUnderRoot(DirectoryInfo rootDirectory, string relativePath)
    {
        return ResolvePathUnderDirectory(rootDirectory, relativePath);
    }

    private static string ResolvePathUnderDirectory(DirectoryInfo baseDirectory, string relativePath)
    {
        ValidateRelativePath(relativePath);
        var basePath = Path.GetFullPath(baseDirectory.FullName);
        var normalizedRelativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
        var resolvedPath = Path.GetFullPath(Path.Join(basePath, normalizedRelativePath));
        var basePathWithSeparator = Path.EndsInDirectorySeparator(basePath)
            ? basePath
            : basePath + Path.DirectorySeparatorChar;
        if (!resolvedPath.StartsWith(basePathWithSeparator, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(resolvedPath, basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"导出目录中的路径越界：{relativePath}");
        }

        return resolvedPath;
    }

    private static void ValidateRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new InvalidDataException("相对路径不能为空。");
        }

        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException($"导出目录中的路径必须是相对路径：{relativePath}");
        }

        var segments = relativePath.Split(['/', '\\'], StringSplitOptions.None);
        if (segments.Any(segment => string.IsNullOrEmpty(segment) || segment is "." or ".."))
        {
            throw new InvalidDataException($"导出目录中的路径不能包含空、. 或 .. 路径片段：{relativePath}");
        }
    }

    private static void ValidateSlideFilePath(
        string relativePath,
        int slideIndex,
        string expectedExtension,
        string fieldName)
    {
        ValidateRelativePath(relativePath);
        var expectedFileName = $"Slide{slideIndex:D3}{expectedExtension}";
        if (!string.Equals(Path.GetFileName(relativePath), expectedFileName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"页面 {slideIndex} 的 {fieldName} 必须指向 {expectedFileName}：{relativePath}");
        }
    }

    private static void ValidateResourceFileName(string exportFile)
    {
        ValidateRelativePath(exportFile);
        if (exportFile.Contains('/') || exportFile.Contains('\\'))
        {
            throw new InvalidDataException($"资源 ExportFile 必须是单个文件名：{exportFile}");
        }
    }

    private static void AddSlideWarning(
        List<CoursewareLoadWarning> slideWarnings,
        List<CoursewareLoadWarning> warnings,
        string code,
        string message,
        string? relativePath,
        int slideIndex)
    {
        var warning = new CoursewareLoadWarning(code, message, relativePath, slideIndex);
        slideWarnings.Add(warning);
        warnings.Add(warning);
    }
}
