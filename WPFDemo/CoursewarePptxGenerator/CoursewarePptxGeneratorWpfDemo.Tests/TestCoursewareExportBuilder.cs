using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGenerator.Core.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

internal sealed class TestCoursewareExportBuilder
{
    private readonly List<TestSlideEntry> _slides = new();
    private readonly List<CoursewareResourceEntry> _resources = new();
    public DirectoryInfo RootDirectory { get; }

    public TestCoursewareExportBuilder()
    {
        RootDirectory = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"CoursewareExportTests_{Guid.NewGuid():N}"));
    }

    public TestCoursewareExportBuilder AddSlide(
        string slideId,
        string markdownText,
        bool hasScreenshot = true,
        double width = 1280,
        double height = 720)
    {
        var slideIndex = _slides.Count;
        var markdownFile = $"Slides/Slide{slideIndex:D3}.md";
        var screenshotFile = $"Slides/Slide{slideIndex:D3}.jpg";

        WriteText(markdownFile, NormalizeMarkdownIdentity(markdownText, slideId, slideIndex + 1, width, height));
        if (hasScreenshot)
        {
            WriteBytes(screenshotFile, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII="));
        }

        _slides.Add(new TestSlideEntry(slideIndex, slideId, markdownFile, screenshotFile, width, height));
        return this;
    }

    public TestCoursewareExportBuilder AddResource(string resourceId, string resourceType, string exportFile, bool exists = true)
    {
        _resources.Add(new CoursewareResourceEntry
        {
            ResourceId = resourceId,
            ResourceType = resourceType,
            ExportFile = exportFile,
        });

        if (exists)
        {
            WriteBytes(Path.Join("Resources", exportFile), [1, 2, 3, 4]);
        }

        return this;
    }

    public DirectoryInfo Build()
    {
        var manifest = new CoursewareExportManifest
        {
            ExportVersion = 1,
            CreatedAt = DateTimeOffset.Parse("2026-07-07T10:20:13+08:00"),
            CoursewareName = "测试课件",
            SlideCount = _slides.Count,
            ResourcesFile = "Resources/Resources.json",
            Slides = _slides.Select(slide => new CoursewareExportSlideEntry
            {
                SlideIndex = slide.SlideIndex,
                SlideId = slide.SlideId,
                Width = slide.Width,
                Height = slide.Height,
                MarkdownFile = slide.MarkdownFile,
                ScreenshotFile = slide.ScreenshotFile,
            }).ToArray(),
        };

        WriteJson("Courseware.json", manifest, CoursewareExportJsonSerializerContext.Default.CoursewareExportManifest);
        WriteJson("Resources/Resources.json", _resources.ToArray(), CoursewareExportJsonSerializerContext.Default.CoursewareResourceEntryArray);

        return RootDirectory;
    }

    private void WriteText(string relativePath, string text)
    {
        var filePath = Path.Join(RootDirectory.FullName, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, text);
    }

    private void WriteBytes(string relativePath, byte[] data)
    {
        var filePath = Path.Join(RootDirectory.FullName, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllBytes(filePath, data);
    }

    private static string NormalizeMarkdownIdentity(
        string markdownText,
        string slideId,
        int pageNumber,
        double width,
        double height)
    {
        var normalized = markdownText;
        var idPattern = new Regex(@"(?im)^\s*-\s*Id\s*:\s*[^\r\n]*$", RegexOptions.CultureInvariant);
        var pageNumberPattern = new Regex(
            @"(?im)^\s*-\s*序号\s*\(\s*1-base\s*\)\s*:\s*[^\r\n]*$",
            RegexOptions.CultureInvariant);
        var dimensionsPattern = new Regex(
            @"(?im)^\s*-\s*尺寸\s*:\s*[^\r\n]*$",
            RegexOptions.CultureInvariant);
        var hasId = idPattern.IsMatch(normalized);
        var hasPageNumber = pageNumberPattern.IsMatch(normalized);
        var hasDimensions = dimensionsPattern.IsMatch(normalized);
        if (hasId)
        {
            normalized = idPattern.Replace(normalized, $"- Id: {slideId}", 1);
        }

        if (hasPageNumber)
        {
            normalized = pageNumberPattern.Replace(normalized, $"- 序号(1-base): {pageNumber}", 1);
        }

        var dimensions = $"{width:0.####}×{height:0.####}";
        if (hasDimensions)
        {
            normalized = dimensionsPattern.Replace(normalized, $"- 尺寸: {dimensions}", 1);
        }

        if (hasId && hasPageNumber && hasDimensions)
        {
            return normalized;
        }

        var metadata = $"## 页面信息\n\n- Id: {slideId}\n- 尺寸: {dimensions}\n- 序号(1-base): {pageNumber}\n\n---\n\n";
        return metadata + normalized;
    }

    private void WriteJson<T>(string relativePath, T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo)
    {
        var filePath = Path.Join(RootDirectory.FullName, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, JsonSerializer.Serialize(value, jsonTypeInfo));
    }

    private sealed record TestSlideEntry(
        int SlideIndex,
        string SlideId,
        string MarkdownFile,
        string ScreenshotFile,
        double Width,
        double Height);
}
