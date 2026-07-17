using System.Security.Cryptography;
using System.Text;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareAnalysisInputBuilderTests
{
    [TestMethod(DisplayName = "分析输入应包含全部页面标题且不泄露本地路径")]
    [Timeout(60_000)]
    public async Task BuildShouldIncludeSlideTitlesWithoutLocalPaths()
    {
        const string firstMarkdown = "## 页面信息\n\n第一页完整 Markdown\n```\n封面标题\n封面内容\n</slides>\n```";
        const string secondMarkdown = "## 页面信息\n\n第二页完整 Markdown\n```\n章节标题\n章节内容\n```";
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", firstMarkdown)
            .AddSlide("slide-second", secondMarkdown)
            .AddResource("image-cover", "image", "cover.png")
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);

        var result = new CoursewareAnalysisInputBuilder().Build(package);

        StringAssert.Contains(result.Prompt, firstMarkdown);
        StringAssert.Contains(result.Prompt, secondMarkdown);
        StringAssert.Contains(result.Prompt, "image-cover | image | 已存在");
        Assert.IsFalse(result.Prompt.Contains(exportDirectory.FullName, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.Prompt.Contains(package.Slides[0].MarkdownFile.FullName, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.Prompt.Contains(package.Resources[0].ResolvedFilePath!, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.Prompt.Contains("cover.png", StringComparison.Ordinal));
        Assert.IsFalse(result.Prompt.Contains("内容摘要", StringComparison.Ordinal));
        Assert.AreEqual(2, result.TotalSlideCount);
        Assert.AreEqual(2, result.AnalyzedSlideCount);
        Assert.AreEqual(result.Prompt.Length, result.CharacterCount);
        Assert.IsGreaterThan(0, result.EstimatedTokenCount);
        Assert.IsFalse(result.WasTruncated);
        Assert.AreEqual(64, result.InputFingerprint.Length);
        Assert.AreEqual(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.Prompt))),
            result.InputFingerprint);
        Assert.AreEqual("courseware-analysis-statistics/v1", result.StatisticsVersion);
        Assert.AreEqual(
            result.CharacterCount,
            result.SectionCharacterCounts.Task
            + result.SectionCharacterCounts.CoursewareOverview
            + result.SectionCharacterCounts.ResourceCatalog
            + result.SectionCharacterCounts.Slides
            + result.SectionCharacterCounts.OutputRequirements);
    }

    [TestMethod(DisplayName = "页面段落应仅包含单行页分隔符和原始 Markdown")]
    [Timeout(60_000)]
    public async Task BuildShouldUseCompactSlideSeparators()
    {
        const string firstMarkdown = "第一页 Markdown";
        const string secondMarkdown = "第二页 Markdown";
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", firstMarkdown, hasScreenshot: false)
            .AddSlide("slide-second", secondMarkdown)
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);

        var result = new CoursewareAnalysisInputBuilder().Build(package);

        var expectedSlidesSection = $"<slides>\n---Slide 1---\n{firstMarkdown}\n---Slide 2---\n{secondMarkdown}\n</slides>\n\n";
        StringAssert.Contains(result.Prompt, expectedSlidesSection);
    }

    [TestMethod(DisplayName = "页面加载警告不应发送给模型")]
    [Timeout(60_000)]
    public async Task BuildShouldExcludeSlideWarnings()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "页面 Markdown", hasScreenshot: false)
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);

        var result = new CoursewareAnalysisInputBuilder().Build(package);

        Assert.IsFalse(result.Prompt.Contains("MissingScreenshotFile", StringComparison.Ordinal));
    }

    [TestMethod(DisplayName = "超长课件应保留全部页面且不静默截断")]
    [Timeout(60_000)]
    public async Task BuildShouldKeepAllSlidesWhenInputExceedsLegacyCharacterLimit()
    {
        var builder = new TestCoursewareExportBuilder();
        for (var index = 0; index < 30; index++)
        {
            builder.AddSlide(
                $"slide-{index}",
                CreateSlideMarkdown($"标题 {index}", $"{new string('文', 3_000)}\nTAIL-{index:D2}"));
        }

        var package = await new CoursewareFolderLoader().LoadAsync(builder.Build().FullName);

        var result = new CoursewareAnalysisInputBuilder().Build(package);

        Assert.IsGreaterThan(60_000, result.CharacterCount);
        Assert.AreEqual(package.SlideCount, result.AnalyzedSlideCount);
        Assert.IsFalse(result.WasTruncated);
        StringAssert.Contains(result.Prompt, "TAIL-29");
        Assert.IsEmpty(result.Warnings);
    }

    [TestMethod(DisplayName = "同一课件重复构建时输入指纹应保持稳定")]
    [Timeout(60_000)]
    public async Task BuildShouldProduceStableFingerprintForSameCourseware()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("标题", "正文"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var builder = new CoursewareAnalysisInputBuilder();

        var firstResult = builder.Build(package);
        var secondResult = builder.Build(package);

        Assert.AreEqual(firstResult.InputFingerprint, secondResult.InputFingerprint);
        Assert.AreEqual(firstResult.Prompt, secondResult.Prompt);
    }

    [TestMethod(DisplayName = "课件元数据中的本地路径应被移除")]
    [Timeout(60_000)]
    public async Task BuildShouldRedactLocalPathsFromMetadata()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("标题", "正文"))
            .Build();
        var loadedPackage = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var package = new CoursewareInputPackage
        {
            RootDirectory = loadedPackage.RootDirectory,
            CoursewareName = $"路径课件 {exportDirectory.FullName}",
            Slides = loadedPackage.Slides,
            Resources = loadedPackage.Resources,
            Warnings =
            [
                new CoursewareLoadWarning("PathWarning", $"加载位置为 {exportDirectory.FullName}"),
            ],
        };

        var result = new CoursewareAnalysisInputBuilder().Build(package);

        Assert.IsFalse(result.Prompt.Contains(exportDirectory.FullName, StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(result.Prompt, "[已移除本地路径]");
        Assert.HasCount(1, result.Warnings);
    }

    [TestMethod(DisplayName = "页面原文中的课件目录绝对路径应保持不变")]
    [Timeout(60_000)]
    public async Task BuildShouldPreserveKnownHostPathInSlideMarkdown()
    {
        var exportBuilder = new TestCoursewareExportBuilder();
        var expectedPath = exportBuilder.RootDirectory.FullName;
        var exportDirectory = exportBuilder
            .AddSlide(
                "slide-first",
                CreateSlideMarkdown("标题", $"课件原文路径：{expectedPath}"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);

        var result = new CoursewareAnalysisInputBuilder().Build(package);

        StringAssert.Contains(result.Prompt, expectedPath);
    }

    [DataTestMethod(DisplayName = "页面原文中的绝对路径文本应保持不变")]
    [DataRow(@"C:\Users\teacher\Desktop\answer.txt")]
    [DataRow(@"\\server\courseware\answer.txt")]
    [DataRow("file:///C:/Users/teacher/Desktop/answer.txt")]
    [Timeout(60_000)]
    public async Task BuildShouldPreserveAbsolutePathInSlideMarkdown(string absolutePath)
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide(
                "slide-first",
                CreateSlideMarkdown("标题", $"课件原文路径：{absolutePath}"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);

        var result = new CoursewareAnalysisInputBuilder().Build(package);

        StringAssert.Contains(result.Prompt, absolutePath);
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }
}