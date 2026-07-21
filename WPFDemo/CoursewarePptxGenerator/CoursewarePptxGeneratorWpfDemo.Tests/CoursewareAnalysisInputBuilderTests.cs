using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGenerator.Core.Serialization;
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
        var envelope = DeserializeEnvelope(result.Prompt);

        Assert.AreEqual("slide-first", envelope.Slides[0].SlideId);
        Assert.AreEqual("slide-second", envelope.Slides[1].SlideId);
        StringAssert.Contains(envelope.Slides[0].Markdown, "封面标题");
        StringAssert.Contains(envelope.Slides[0].Markdown, "</slides>");
        StringAssert.Contains(envelope.Slides[1].Markdown, "章节标题");
        Assert.AreEqual("image-cover", envelope.Resources[0].ResourceId);
        Assert.AreEqual("image", envelope.Resources[0].ResourceType);
        Assert.IsTrue(envelope.Resources[0].Exists);
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
        Assert.AreEqual(64, result.SourceFingerprint.Length);
        Assert.AreEqual(64, result.AnalysisViewFingerprint.Length);
        Assert.AreEqual(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.Prompt))),
            result.AnalysisViewFingerprint);
        Assert.AreEqual(result.AnalysisViewFingerprint, result.InputFingerprint);
        Assert.AreEqual("courseware-analysis-statistics/v2", result.StatisticsVersion);
        Assert.AreEqual(
            result.CharacterCount,
            result.SectionCharacterCounts.Task
            + result.SectionCharacterCounts.CoursewareOverview
            + result.SectionCharacterCounts.ResourceCatalog
            + result.SectionCharacterCounts.Slides
            + result.SectionCharacterCounts.OutputRequirements);
    }

    [TestMethod(DisplayName = "页面数组应按顺序携带稳定身份和完整 Markdown")]
    [Timeout(60_000)]
    public async Task BuildShouldUseStructuredSlideIdentityAndCompleteMarkdown()
    {
        const string firstMarkdown = "第一页 Markdown";
        const string secondMarkdown = "第二页 Markdown";
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", firstMarkdown, hasScreenshot: false)
            .AddSlide("slide-second", secondMarkdown)
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);

        var result = new CoursewareAnalysisInputBuilder().Build(package);
        var envelope = DeserializeEnvelope(result.Prompt);

        Assert.HasCount(2, envelope.Slides);
        Assert.AreEqual("slide-first", envelope.Slides[0].SlideId);
        Assert.AreEqual(1, envelope.Slides[0].PageNumber);
        Assert.AreEqual(0, envelope.Slides[0].SlideIndex);
        Assert.AreEqual(package.Slides[0].MarkdownText, envelope.Slides[0].Markdown);
        Assert.AreEqual("slide-second", envelope.Slides[1].SlideId);
        Assert.AreEqual(2, envelope.Slides[1].PageNumber);
        Assert.AreEqual(1, envelope.Slides[1].SlideIndex);
        Assert.AreEqual(package.Slides[1].MarkdownText, envelope.Slides[1].Markdown);
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
        var envelope = DeserializeEnvelope(result.Prompt);

        Assert.IsFalse(result.Prompt.Contains("MissingScreenshotFile", StringComparison.Ordinal));
        Assert.IsEmpty(envelope.CoursewareOverview.Warnings);
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

    [TestMethod(DisplayName = "页面原文中的课件目录绝对路径应只在分析视图脱敏")]
    [Timeout(60_000)]
    public async Task BuildShouldRedactKnownHostPathInAnalysisViewWithoutChangingSourceFacts()
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

        Assert.IsFalse(result.Prompt.Contains(expectedPath, StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(result.Prompt, "[已移除本地路径]");
        Assert.HasCount(1, result.PrivacyDiagnostics);
        Assert.AreEqual("slide-markdown", result.PrivacyDiagnostics[0].Section);
        Assert.AreEqual("slide-first", result.PrivacyDiagnostics[0].SlideId);
        StringAssert.Contains(package.Slides[0].MarkdownText, expectedPath);
        Assert.AreNotEqual(result.SourceFingerprint, result.AnalysisViewFingerprint);
    }

    [TestMethod(DisplayName = "页面原文中的绝对路径文本应在发送前脱敏")]
    [DataRow(@"C:\Users\teacher\Desktop\answer.txt")]
    [DataRow(@"\\server\courseware\answer.txt")]
    [DataRow("file:///C:/Users/teacher/Desktop/answer.txt")]
    [DataRow("/home/teacher/courseware/answer.txt")]
    [Timeout(60_000)]
    public async Task BuildShouldRedactAbsolutePathInSlideMarkdown(string absolutePath)
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide(
                "slide-first",
                CreateSlideMarkdown("标题", $"课件原文路径：{absolutePath}"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);

        var result = new CoursewareAnalysisInputBuilder().Build(package);

        Assert.IsFalse(result.Prompt.Contains(absolutePath, StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(result.Prompt, "[已移除本地路径]");
        Assert.HasCount(1, result.PrivacyDiagnostics);
        Assert.AreEqual(CoursewarePathPrivacyAction.Redacted, result.PrivacyDiagnostics[0].Action);
        Assert.AreEqual(64, result.PrivacyDiagnostics[0].OriginalValueFingerprint.Length);
    }

    [TestMethod(DisplayName = "阻断策略应在绝对路径发送模型前失败")]
    [Timeout(60_000)]
    public async Task BuildShouldBlockAbsolutePathWhenPrivacyModeIsBlock()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("标题", @"路径：C:\Users\teacher\Desktop\answer.txt"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var builder = new CoursewareAnalysisInputBuilder(
            new CoursewareAnalysisSourceSnapshotBuilder(),
            CoursewarePathPrivacyMode.Block);

        var exception = Assert.ThrowsExactly<CoursewarePathPrivacyException>(() => builder.Build(package));

        StringAssert.Contains(exception.Message, "隐私策略禁止发送");
        Assert.AreEqual(CoursewarePathPrivacyAction.Blocked, exception.Diagnostic.Action);
    }

    [TestMethod(DisplayName = "清单 SlideId 与 Markdown Id 不一致时应拒绝分析")]
    [Timeout(60_000)]
    public async Task BuildShouldRejectMismatchedManifestAndMarkdownSlideIds()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("标题", "正文"))
            .Build();
        var markdownPath = Path.Join(exportDirectory.FullName, "Slides", "Slide000.md");
        var markdown = await File.ReadAllTextAsync(markdownPath);
        await File.WriteAllTextAsync(markdownPath, markdown.Replace("- Id: slide-first", "- Id: other-slide", StringComparison.Ordinal));
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => new CoursewareAnalysisInputBuilder().Build(package));

        StringAssert.Contains(exception.Message, "与清单 SlideId");
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }

    private static CoursewareAnalysisEnvelope DeserializeEnvelope(string prompt)
    {
        return JsonSerializer.Deserialize(
                   prompt,
                   CoursewareAnalysisJsonSerializerContext.Default.CoursewareAnalysisEnvelope)
               ?? throw new AssertFailedException("分析输入 JSON 信封不能为空。");
    }
}