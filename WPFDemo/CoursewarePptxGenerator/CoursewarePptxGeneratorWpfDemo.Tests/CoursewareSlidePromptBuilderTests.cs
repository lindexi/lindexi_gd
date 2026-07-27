using System.Text.Json;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareSlidePromptBuilderTests
{
    [TestMethod(DisplayName = "首轮结构化 Prompt 应包含完整页面上下文和全部 Theme 2.0 原文")]
    [Timeout(60_000)]
    public async Task BuildShouldContainCompletePageContextAndTheme20WithoutTruncation()
    {
        var longMarker = new string('长', 12_000);
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("上一页", "上一页摘要"))
            .AddSlide("slide-current", CreateSlideMarkdown("当前页", $"当前页完整内容 {longMarker} resource-first"))
            .AddSlide("slide-next", CreateSlideMarkdown("下一页", "下一页摘要"))
            .AddResource("resource-first", "image", "resource-first.png")
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var baseTheme = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package).Theme;
        var theme = baseTheme with
        {
            CoverPageSlideMl = $"<Page><TextElement Text=\"cover-{longMarker}\" /></Page>",
            ContentPageSlideMl = $"<Page><TextElement Text=\"content-{longMarker}\" /></Page>",
        };
        var builder = new CoursewareSlidePromptBuilder();

        var result = builder.Build(
            package,
            new CoursewareThemeAnalysisResult { Theme = theme },
            1,
            "用户补充要求：强化结论",
            screenshotAttached: true);

        Assert.AreSame(theme, result.Envelope.Theme);
        Assert.AreEqual(package.Slides[1].MarkdownText, result.Envelope.CurrentSlide.Markdown);
        Assert.AreEqual("上一页", result.Envelope.Neighbors.Previous!.Title);
        Assert.AreEqual("下一页", result.Envelope.Neighbors.Next!.Title);
        Assert.AreEqual("用户补充要求：强化结论", result.Envelope.Task.UserInstruction);
        Assert.IsTrue(result.Envelope.VisualInput.WasAttached);
        Assert.AreEqual(theme.ColorSuggestions.Count, result.Envelope.Theme.ColorSuggestions.Count);
        Assert.AreEqual(theme.Fonts.Chinese, result.Envelope.Theme.Fonts.Chinese);
        Assert.AreEqual(theme.Fonts.Western, result.Envelope.Theme.Fonts.Western);
        Assert.AreEqual(theme.Style, result.Envelope.Theme.Style);
        Assert.AreEqual(theme.SafeArea, result.Envelope.Theme.SafeArea);
        Assert.AreEqual(theme.SpacingAndVisualEffects, result.Envelope.Theme.SpacingAndVisualEffects);
        Assert.AreEqual(theme.LayoutPrinciples, result.Envelope.Theme.LayoutPrinciples);
        Assert.AreEqual(theme.CoverPageSlideMl, result.Envelope.Theme.CoverPageSlideMl);
        Assert.AreEqual(theme.ContentPageSlideMl, result.Envelope.Theme.ContentPageSlideMl);
        StringAssert.Contains(result.Prompt, longMarker);
        StringAssert.Contains(result.Prompt, "cover-");
        StringAssert.Contains(result.Prompt, "content-");
        Assert.IsFalse(result.Prompt.Contains(exportDirectory.FullName, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.Prompt.Contains(package.Slides[1].MarkdownFile.FullName, StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.Prompt.Contains("DesignSystem", StringComparison.Ordinal));
    }

    [TestMethod(DisplayName = "准备的 Prompt Source 应持有原始 Theme 实例且不改写坐标")]
    [Timeout(60_000)]
    public async Task PrepareSourceShouldKeepOriginalThemeInstance()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("标题", "正文"), width: 1600, height: 900)
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var result = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package);
        var builder = new CoursewareSlidePromptBuilder();

        var source = builder.PrepareSource(package, result);
        var prompt = builder.Build(source, 0, "保持原始主题", screenshotAttached: false);

        Assert.AreSame(result.Theme, source.Theme);
        Assert.AreSame(result.Theme, prompt.Envelope.Theme);
        Assert.AreEqual(0, prompt.Envelope.CurrentSlide.Width);
        Assert.AreEqual(0, prompt.Envelope.CurrentSlide.Height);
        Assert.AreEqual(0d, prompt.Envelope.CurrentSlide.LogicalWidth);
        Assert.AreEqual(0d, prompt.Envelope.CurrentSlide.LogicalHeight);
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }
}
