using System.Text.Json;
using AgentLib;
using AgentLib.Core.AgentApiManagers.Contexts;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator.Models;
using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareSlidePromptBuilderTests
{
    [TestMethod(DisplayName = "页面 Prompt 应包含当前页完整 Markdown、相邻摘要和缩放后的主题")]
    [Timeout(60_000)]
    public async Task BuildShouldIncludeCompleteCurrentSlideBoundedNeighborsAndScaledTheme()
    {
        var currentMarkdown = CreateSlideMarkdown("第二页标题", $"第二页正文 {new string('文', 2_000)} TAIL-CURRENT");
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("第一页标题", "第一页正文"))
            .AddSlide("slide-second", currentMarkdown)
            .AddSlide("slide-third", CreateSlideMarkdown("第三页标题", "第三页正文"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var analysisResult = FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package) with
        {
            ReferenceCanvas = new SlideDocumentContext(640, 360),
        };
        var builder = CreateBuilder();
        var source = builder.PrepareSource(package, analysisResult);

        var result = builder.Build(source, 1, "突出本页核心结论", screenshotAttached: true);
        var envelope = JsonSerializer.Deserialize(
            result.Prompt,
            CoursewareSlideGenerationJsonSerializerContext.Default.CoursewareSlideGenerationEnvelope)
            ?? throw new AssertFailedException("页面生成 Prompt JSON 信封不能为空。");

        Assert.AreEqual(CoursewareSlideGenerationEnvelope.CurrentSchemaVersion, envelope.SchemaVersion);
        Assert.AreEqual("slide-second", envelope.CurrentSlide.SlideId);
        Assert.AreEqual(2, envelope.CurrentSlide.PageNumber);
        Assert.AreEqual(1280, envelope.CurrentSlide.Width);
        Assert.AreEqual(720, envelope.CurrentSlide.Height);
        Assert.AreEqual(1280d, envelope.CurrentSlide.LogicalWidth);
        Assert.AreEqual(720d, envelope.CurrentSlide.LogicalHeight);
        Assert.IsTrue(envelope.CurrentSlide.ScreenshotAttached);
        Assert.IsTrue(envelope.VisualInput.WasAttached);
        Assert.AreEqual("ThemeSuggestion", envelope.DesignContext.Capability);
        Assert.AreEqual(640, envelope.DesignContext.ReferenceCanvasWidth);
        Assert.AreEqual(360, envelope.DesignContext.ReferenceCanvasHeight);
        Assert.AreEqual("Page", envelope.OutputRequirements.RootElement);
        StringAssert.Contains(envelope.CurrentSlide.Markdown, "TAIL-CURRENT");
        Assert.AreEqual(package.Slides[1].MarkdownText, envelope.CurrentSlide.Markdown);
        Assert.AreEqual("slide-first", envelope.Neighbors.Previous?.SlideId);
        Assert.AreEqual("第一页标题", envelope.Neighbors.Previous?.Title);
        Assert.AreEqual("slide-third", envelope.Neighbors.Next?.SlideId);
        Assert.AreEqual("第三页标题", envelope.Neighbors.Next?.Title);
        Assert.IsLessThan(120, envelope.Neighbors.Previous?.Summary.Length ?? 0);
        Assert.AreEqual(64, envelope.DesignContext.Typography.PrimaryHeading.FontSize);
        Assert.AreEqual(120, envelope.DesignContext.SafeArea.Left);
        Assert.AreEqual("突出本页核心结论", envelope.Task.UserInstruction);
        Assert.IsGreaterThan(0, result.EstimatedTokenCount);
        Assert.IsFalse(result.Prompt.Contains(exportDirectory.FullName, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod(DisplayName = "页面 Prompt 应复用课件路径脱敏并拒绝泄露本地绝对路径")]
    [Timeout(60_000)]
    public async Task BuildShouldReusePrivacySafeMarkdownWithoutLocalPaths()
    {
        const string localPath = @"C:\Users\teacher\Desktop\answer.txt";
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", CreateSlideMarkdown("标题", $"路径：{localPath}"))
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var builder = CreateBuilder();
        var result = builder.Build(
            package,
            FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package),
            0,
            "美化页面",
            screenshotAttached: false);

        Assert.IsFalse(result.Prompt.Contains(localPath, StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(result.Envelope.CurrentSlide.Markdown, "[已移除本地路径]");
        StringAssert.Contains(result.Envelope.VisualInput.EvidenceBoundary, "未附带原始页面截图");
        Assert.IsFalse(result.Envelope.VisualInput.WasAttached);
    }

    [TestMethod(DisplayName = "页面 Prompt 应记录非整数画布转换和不同比例 fallback 诊断")]
    [Timeout(60_000)]
    public async Task BuildShouldExposeCanvasConversionAndAspectRatioDiagnostics()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide(
                "slide-first",
                CreateSlideMarkdown("标题", "正文"),
                width: 1000.4,
                height: 1000.6)
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var result = CreateBuilder().Build(
            package,
            FakeCoursewareThemeAnalysisService.CreateSuccessfulResult(package) with
            {
                ReferenceCanvas = new SlideDocumentContext(1280, 720),
            },
            0,
            "美化页面",
            screenshotAttached: true);

        Assert.AreEqual(1000.4, result.Envelope.CurrentSlide.LogicalWidth);
        Assert.AreEqual(1000.6, result.Envelope.CurrentSlide.LogicalHeight);
        Assert.AreEqual(1000, result.Envelope.CurrentSlide.Width);
        Assert.AreEqual(1001, result.Envelope.CurrentSlide.Height);
        Assert.IsTrue(result.Envelope.CurrentSlide.Diagnostics.Any(item => item.StartsWith("CanvasDimensionRounded", StringComparison.Ordinal)));
        Assert.IsTrue(result.Envelope.DesignContext.Diagnostics.Any(item => item.StartsWith("CanvasProfileFallback", StringComparison.Ordinal)));
    }

    [TestMethod(DisplayName = "页面预算预检应覆盖系统 Prompt、用户 Prompt、工具和输出预留")]
    [Timeout(60_000)]
    public void ValidateIfConfiguredShouldAccountForCompleteStreamingRequest()
    {
        var promptProvider = new SlideMlPromptProvider(new SlideDocumentContext(1280, 720));
        var renderTool = new SlideMlRenderTool(new FakeSlideMlRenderPipeline(), new FakeMainThreadDispatcher());
        var modelDefinition = new ModelDefinition
        {
            ModelName = "large-context-test",
            ContextWindowSize = 100_000,
            MaxOutputTokens = 8_000,
        };

        var budget = CoursewareSlideContextBudgetValidator.ValidateIfConfigured(
            modelDefinition,
            promptProvider,
            renderTool,
            pageNumber: 2,
            pagePrompt: "结构化页面 Prompt");

        Assert.IsNotNull(budget);
        Assert.IsGreaterThan(0, budget.SystemPromptTokenCount);
        Assert.IsGreaterThan(0, budget.UserPromptTokenCount);
        Assert.IsGreaterThan(0, budget.ToolSchemaTokenCount);
        Assert.AreEqual(8_000, budget.OutputTokenReserve);
        Assert.IsGreaterThan(
            budget.SystemPromptTokenCount + budget.UserPromptTokenCount + budget.ToolSchemaTokenCount + budget.OutputTokenReserve,
            budget.RequiredTokenCount);
    }

    [TestMethod(DisplayName = "页面预算不足时应显式失败且不截断 Prompt")]
    [Timeout(60_000)]
    public void ValidateIfConfiguredShouldFailWithoutTruncatingPagePrompt()
    {
        var promptProvider = new SlideMlPromptProvider(new SlideDocumentContext(1280, 720));
        var renderTool = new SlideMlRenderTool(new FakeSlideMlRenderPipeline(), new FakeMainThreadDispatcher());
        var modelDefinition = new ModelDefinition
        {
            ModelName = "small-context-test",
            ContextWindowSize = 1_000,
            MaxOutputTokens = 500,
        };
        var pagePrompt = new string('文', 4_000) + "TAIL-NOT-TRUNCATED";

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            CoursewareSlideContextBudgetValidator.ValidateIfConfigured(
                modelDefinition,
                promptProvider,
                renderTool,
                pageNumber: 7,
                pagePrompt));

        StringAssert.Contains(exception.Message, "第 7 页");
        StringAssert.Contains(exception.Message, "不会静默截断");
        StringAssert.EndsWith(pagePrompt, "TAIL-NOT-TRUNCATED");
    }

    private static CoursewareSlidePromptBuilder CreateBuilder()
    {
        return new CoursewareSlidePromptBuilder(
            new CoursewareSlideSummaryService(),
            new CoursewareThemePageDesignAdapter());
    }

    private static string CreateSlideMarkdown(string title, string content)
    {
        return $"## 元素细节\n\n### 文本.1\n#### 内容\n```\n{title}\n{content}\n```";
    }
}
