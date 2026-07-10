using System.IO;
using System.Text.Json;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// 从本地文件系统加载课件 Markdown 导出目录。
/// </summary>
public sealed class CoursewareFolderLoader
{
    private const int SupportedExportVersion = 1;
    private const double DefaultSlideWidth = 1280;
    private const double DefaultSlideHeight = 720;
    private const string ManifestFileName = "courseware.json";

    /// <summary>
    /// 加载并验证课件 Markdown 导出目录。
    /// </summary>
    /// <param name="folderPath">用户选择的目录路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>加载完成的课件输入包。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="folderPath" /> 为空时抛出。</exception>
    /// <exception cref="DirectoryNotFoundException">当目录不存在时抛出。</exception>
    /// <exception cref="InvalidDataException">当导出目录无效时抛出。</exception>
    public async Task<CoursewareInputPackage> LoadAsync(string folderPath, CancellationToken cancellationToken = default)
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
            throw new InvalidDataException("缺少 courseware.json，无法识别课件导出目录。");
        }

        var manifest = await ReadJsonAsync(
                manifestFile,
                CoursewareExportJsonSerializerContext.Default.CoursewareExportManifest,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidDataException("courseware.json 内容为空或无法解析。");

        ValidateManifest(manifest, warnings);

        var slides = await LoadSlidesAsync(rootDirectory, manifest.Slides, warnings, cancellationToken).ConfigureAwait(false);
        if (slides.Count == 0)
        {
            throw new InvalidDataException("课件导出目录中没有可加载的页面 Markdown。");
        }

        var resourcesResult = await LoadResourcesAsync(rootDirectory, manifest.ResourcesFile, warnings, cancellationToken).ConfigureAwait(false);
        var coursewareName = string.IsNullOrWhiteSpace(manifest.CoursewareName)
            ? rootDirectory.Name
            : manifest.CoursewareName.Trim();

        return new CoursewareInputPackage
        {
            RootDirectory = rootDirectory,
            CoursewareName = coursewareName,
            ManifestSlideCount = manifest.SlideCount,
            Slides = slides,
            ResourcesIndexFile = resourcesResult.ResourcesIndexFile,
            Resources = resourcesResult.Resources,
            Warnings = warnings,
        };
    }

    private static async Task<T?> ReadJsonAsync<T>(FileInfo file, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenRead();
        return await JsonSerializer.DeserializeAsync(stream, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateManifest(CoursewareExportManifest manifest, List<CoursewareLoadWarning> warnings)
    {
        if (manifest.ExportVersion != SupportedExportVersion)
        {
            throw new InvalidDataException($"不支持的课件导出格式版本：{manifest.ExportVersion}，当前仅支持 {SupportedExportVersion}。");
        }

        if (manifest.Slides.Count == 0)
        {
            throw new InvalidDataException("courseware.json 中没有页面列表。");
        }

        if (manifest.SlideCount != manifest.Slides.Count)
        {
            warnings.Add(new CoursewareLoadWarning(
                CoursewareLoadWarningLevel.Warning,
                "SlideCountMismatch",
                $"课件声明页数 {manifest.SlideCount} 与页面列表数量 {manifest.Slides.Count} 不一致。"));
        }
    }

    private static async Task<IReadOnlyList<CoursewareSlideInput>> LoadSlidesAsync(
        DirectoryInfo rootDirectory,
        IReadOnlyList<CoursewareExportSlideEntry> entries,
        List<CoursewareLoadWarning> warnings,
        CancellationToken cancellationToken)
    {
        var slides = new List<CoursewareSlideInput>(entries.Count);
        for (var i = 0; i < entries.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = entries[i];
            var slideWarnings = new List<CoursewareLoadWarning>();

            if (entry.SlideIndex != i)
            {
                AddSlideWarning(slideWarnings, warnings, "SlideIndexMismatch", $"页面索引 {entry.SlideIndex} 与列表位置 {i} 不一致。", null, entry.SlideIndex);
            }

            if (string.IsNullOrWhiteSpace(entry.SlideId))
            {
                AddSlideWarning(slideWarnings, warnings, "MissingSlideId", $"第 {i + 1} 页缺少 slideId。", null, i);
            }

            if (string.IsNullOrWhiteSpace(entry.MarkdownFile))
            {
                throw new InvalidDataException($"第 {i + 1} 页缺少 markdownFile。");
            }

            var markdownPath = ResolvePathUnderRoot(rootDirectory, entry.MarkdownFile);
            var markdownFile = new FileInfo(markdownPath);
            if (!markdownFile.Exists)
            {
                throw new FileNotFoundException($"第 {i + 1} 页 Markdown 文件不存在：{entry.MarkdownFile}", markdownPath);
            }

            var markdownText = await File.ReadAllTextAsync(markdownFile.FullName, cancellationToken).ConfigureAwait(false);
            var width = entry.Width;
            var height = entry.Height;
            if (width <= 0 || height <= 0)
            {
                width = DefaultSlideWidth;
                height = DefaultSlideHeight;
                AddSlideWarning(slideWarnings, warnings, "InvalidSlideSize", $"第 {i + 1} 页尺寸无效，已回退到 1280 x 720。", entry.MarkdownFile, i);
            }

            FileInfo? screenshotFile = null;
            if (string.IsNullOrWhiteSpace(entry.ScreenshotFile))
            {
                AddSlideWarning(slideWarnings, warnings, "MissingScreenshotPath", $"第 {i + 1} 页缺少截图路径。", null, i);
            }
            else
            {
                var screenshotPath = ResolvePathUnderRoot(rootDirectory, entry.ScreenshotFile);
                var candidateScreenshotFile = new FileInfo(screenshotPath);
                if (candidateScreenshotFile.Exists)
                {
                    screenshotFile = candidateScreenshotFile;
                }
                else
                {
                    AddSlideWarning(slideWarnings, warnings, "MissingScreenshotFile", $"第 {i + 1} 页截图文件不存在。", entry.ScreenshotFile, i);
                }
            }

            slides.Add(new CoursewareSlideInput
            {
                SlideIndex = entry.SlideIndex,
                PageNumber = i + 1,
                SlideId = string.IsNullOrWhiteSpace(entry.SlideId) ? $"slide-{i + 1}" : entry.SlideId.Trim(),
                Width = width,
                Height = height,
                MarkdownFile = markdownFile,
                ScreenshotFile = screenshotFile,
                MarkdownText = markdownText,
                Warnings = slideWarnings,
            });
        }

        return slides;
    }

    private static async Task<(FileInfo? ResourcesIndexFile, IReadOnlyList<CoursewareResourceEntry> Resources)> LoadResourcesAsync(
        DirectoryInfo rootDirectory,
        string? resourcesFilePath,
        List<CoursewareLoadWarning> warnings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resourcesFilePath))
        {
            warnings.Add(new CoursewareLoadWarning(CoursewareLoadWarningLevel.Warning, "MissingResourcesFile", "课件清单未声明资源索引文件。"));
            return (null, []);
        }

        var resourcesIndexPath = ResolvePathUnderRoot(rootDirectory, resourcesFilePath);
        var resourcesIndexFile = new FileInfo(resourcesIndexPath);
        if (!resourcesIndexFile.Exists)
        {
            warnings.Add(new CoursewareLoadWarning(CoursewareLoadWarningLevel.Warning, "MissingResourcesIndex", "资源索引文件不存在。", resourcesFilePath));
            return (null, []);
        }

        var resources = await ReadResourcesAsync(resourcesIndexFile, cancellationToken).ConfigureAwait(false);
        var resourcesDirectory = resourcesIndexFile.Directory ?? rootDirectory;
        var resolvedResources = new List<CoursewareResourceEntry>(resources.Count);
        foreach (var resource in resources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(resource.ExportFile))
            {
                warnings.Add(new CoursewareLoadWarning(CoursewareLoadWarningLevel.Warning, "MissingResourceExportFile", "资源条目缺少 exportFile。"));
                resolvedResources.Add(resource with { Exists = false });
                continue;
            }

            var resolvedPath = ResolvePathUnderDirectory(resourcesDirectory, resource.ExportFile);
            var exists = File.Exists(resolvedPath);
            if (!exists)
            {
                warnings.Add(new CoursewareLoadWarning(CoursewareLoadWarningLevel.Warning, "MissingResourceFile", "资源文件不存在。", resource.ExportFile));
            }

            resolvedResources.Add(resource with
            {
                ResolvedFilePath = resolvedPath,
                Exists = exists,
            });
        }

        return (resourcesIndexFile, resolvedResources);
    }

    private static async Task<IReadOnlyList<CoursewareResourceEntry>> ReadResourcesAsync(FileInfo resourcesIndexFile, CancellationToken cancellationToken)
    {
        await using var stream = resourcesIndexFile.OpenRead();
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            var arrayJson = document.RootElement.GetRawText();
            return JsonSerializer.Deserialize(arrayJson, CoursewareExportJsonSerializerContext.Default.CoursewareResourceEntryArray) ?? [];
        }

        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            var manifestJson = document.RootElement.GetRawText();
            var manifest = JsonSerializer.Deserialize(manifestJson, CoursewareExportJsonSerializerContext.Default.CoursewareResourceManifest);
            if (manifest is not null)
            {
                return manifest.Resources;
            }
        }

        throw new InvalidDataException("resources.json 格式无效。");
    }

    private static string ResolvePathUnderRoot(DirectoryInfo rootDirectory, string relativePath)
    {
        return ResolvePathUnderDirectory(rootDirectory, relativePath);
    }

    private static string ResolvePathUnderDirectory(DirectoryInfo baseDirectory, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new InvalidDataException("相对路径不能为空。");
        }

        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException($"导出目录中的路径必须是相对路径：{relativePath}");
        }

        var basePath = Path.GetFullPath(baseDirectory.FullName);
        var resolvedPath = Path.GetFullPath(Path.Join(basePath, relativePath));
        var basePathWithSeparator = Path.EndsInDirectorySeparator(basePath) ? basePath : basePath + Path.DirectorySeparatorChar;
        if (!resolvedPath.StartsWith(basePathWithSeparator, StringComparison.OrdinalIgnoreCase) && !string.Equals(resolvedPath, basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"导出目录中的路径越界：{relativePath}");
        }

        return resolvedPath;
    }

    private static void AddSlideWarning(
        List<CoursewareLoadWarning> slideWarnings,
        List<CoursewareLoadWarning> allWarnings,
        string code,
        string message,
        string? relativePath,
        int slideIndex)
    {
        var warning = new CoursewareLoadWarning(CoursewareLoadWarningLevel.Warning, code, message, relativePath, slideIndex);
        slideWarnings.Add(warning);
        allWarnings.Add(warning);
    }
}
