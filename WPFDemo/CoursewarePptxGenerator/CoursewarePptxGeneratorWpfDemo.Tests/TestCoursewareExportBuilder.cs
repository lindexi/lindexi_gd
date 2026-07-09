using System.IO;
using System.Text.Json;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

internal sealed class TestCoursewareExportBuilder
{
    private readonly List<TestSlideEntry> _slides = new();
    private readonly List<CoursewareResourceEntry> _resources = new();
    private bool _writeResourcesFile = true;

    public DirectoryInfo RootDirectory { get; }

    public TestCoursewareExportBuilder()
    {
        RootDirectory = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"CoursewareExportTests_{Guid.NewGuid():N}"));
    }

    public TestCoursewareExportBuilder AddSlide(string slideId, string markdownText, bool hasScreenshot = true)
    {
        var slideIndex = _slides.Count;
        var pageNumber = slideIndex + 1;
        var markdownFile = $"slides/Slide_{pageNumber:D3}.md";
        var screenshotFile = hasScreenshot ? $"screenshots/Slide_{pageNumber:D3}.png" : $"screenshots/Missing_{pageNumber:D3}.png";

        WriteText(markdownFile, markdownText);
        if (hasScreenshot)
        {
            WriteBytes(screenshotFile, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII="));
        }

        _slides.Add(new TestSlideEntry(slideIndex, slideId, markdownFile, screenshotFile));
        return this;
    }

    public TestCoursewareExportBuilder AddResource(string imageId, string sourceFileName, string exportFile, bool exists = true)
    {
        _resources.Add(new CoursewareResourceEntry
        {
            ImageId = imageId,
            SourceFileName = sourceFileName,
            ExportFile = exportFile,
        });

        if (exists)
        {
            WriteBytes(Path.Join("resources", exportFile), [1, 2, 3, 4]);
        }

        return this;
    }

    public TestCoursewareExportBuilder WithoutResourcesFile()
    {
        _writeResourcesFile = false;
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
            ResourcesFile = _writeResourcesFile ? "resources/resources.json" : null,
            Slides = _slides.Select(slide => new CoursewareExportSlideEntry
            {
                SlideIndex = slide.SlideIndex,
                SlideId = slide.SlideId,
                Width = 1280,
                Height = 720,
                MarkdownFile = slide.MarkdownFile,
                ScreenshotFile = slide.ScreenshotFile,
            }).ToArray(),
        };

        WriteJson("courseware.json", manifest, CoursewareExportJsonSerializerContext.Default.CoursewareExportManifest);
        if (_writeResourcesFile)
        {
            WriteJson("resources/resources.json", new CoursewareResourceManifest { Resources = _resources }, CoursewareExportJsonSerializerContext.Default.CoursewareResourceManifest);
        }

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

    private void WriteJson<T>(string relativePath, T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo)
    {
        var filePath = Path.Join(RootDirectory.FullName, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, JsonSerializer.Serialize(value, jsonTypeInfo));
    }

    private sealed record TestSlideEntry(int SlideIndex, string SlideId, string MarkdownFile, string ScreenshotFile);
}
