using System.IO;
using System.Text.Json;
using AgentLib.Model;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareThemeContractTests
{
    [TestMethod(DisplayName = "主题分析服务应汇总提示词并使用首张页面画布调用一次Agent")]
    [Timeout(5000)]
    public async Task AnalyzeAsyncShouldOrchestrateSingleAgentCallAsync()
    {
        var agent = new RecordingThemeAgent(CreateValidTheme());
        var events = new List<CoursewareAnalysisEvent>();
        var service = new CoursewareThemeAnalysisService(
            new CoursewareStyleUsageSummaryBuilder(),
            new CoursewareThemeAnalysisPromptBuilder(),
            agent,
            new CoursewareThemeValidator(new FakeCoursewareThemeSlideMlValidator()));
        var package = CreatePackage("### 文本.1\n字号: 32px | 字体: Arial", width: 1024.4, height: 576.6);

        var result = await service.AnalyzeAsync(package, new Progress<CoursewareAnalysisEvent>(events.Add));

        Assert.AreSame(agent.Theme, result.Theme);
        Assert.AreEqual(1, agent.CallCount);
        Assert.AreEqual(1024, agent.ValidationCanvas!.CanvasWidth);
        Assert.AreEqual(577, agent.ValidationCanvas.CanvasHeight);
        StringAssert.Contains(agent.Prompt!, package.Slides[0].MarkdownText);
        CollectionAssert.AreEqual(
            new[]
            {
                CoursewareAnalysisStage.PreparingInput,
                CoursewareAnalysisStage.PreparingInput,
                CoursewareAnalysisStage.DesigningTheme,
                CoursewareAnalysisStage.Completed,
            },
            events.Select(item => item.Stage).ToArray());
    }

    [TestMethod(DisplayName = "主题字段验证应接受完整2.1契约")]
    [Timeout(5000)]
    public void ValidateShouldAcceptCompleteVersionTwoPointOneTheme()
    {
        var result = new CoursewareThemeValidator().Validate(CreateValidTheme());

        Assert.IsTrue(result.IsValid, string.Join(Environment.NewLine, result.Errors));
    }

    [TestMethod(DisplayName = "主题字段验证应拒绝错误Schema颜色字段和空自然语言字段")]
    [Timeout(5000)]
    public void ValidateShouldRejectInvalidFieldContract()
    {
        var theme = CreateValidTheme() with
        {
            SchemaVersion = "2.0",
            ColorSuggestions =
            [
                new CoursewareColorSuggestion { Name = "", Usage = "背景", Hex = "#ffffff" },
                new CoursewareColorSuggestion { Name = "正文", Usage = "", Hex = "112233" },
            ],
            Fonts = new CoursewareFontSuggestions { Chinese = " ", Western = "" },
            FontSizeRules = " ",
            Style = "",
            SafeArea = new CoursewareSafeAreaRatios
            {
                LeftRatio = -0.1,
                TopRatio = 0.5,
                RightRatio = double.NaN,
                BottomRatio = 0.1,
            },
            SpacingAndVisualEffects = " ",
            LayoutPrinciples = "",
            CoverPageSlideMl = "",
            ContentPageSlideMl = " ",
        };

        var result = new CoursewareThemeValidator().Validate(theme);

        Assert.IsFalse(result.IsValid);
        CollectionAssert.IsSubsetOf(
            new[]
            {
                "SchemaVersion 必须为 2.1。",
                "ColorSuggestions 必须包含 3 到 8 项。",
                "ColorSuggestions.Name 不能为空。",
                "FontSizeRules 不能为空。",
                "LayoutPrinciples 不能为空。",
                "CoverPageSlideMl 不能为空。",
            },
            result.Errors.ToArray());
    }

    [TestMethod(DisplayName = "主题提交工具应保存最新有效结果并清除最近错误")]
    [Timeout(5000)]
    public void SubmitThemeShouldStoreLatestValidTheme()
    {
        var tool = new CoursewareThemeSubmissionTool(new CoursewareThemeValidator());
        var invalid = CreateValidTheme() with { LayoutPrinciples = "" };

        var invalidMessage = tool.SubmitTheme(invalid);
        var validTheme = CreateValidTheme();
        var validMessage = tool.SubmitTheme(validTheme);

        StringAssert.Contains(invalidMessage, "验证失败");
        Assert.AreSame(validTheme, tool.SubmittedTheme);
        Assert.AreEqual(2, tool.SubmissionCount);
        Assert.IsEmpty(tool.ValidationErrors);
        Assert.AreEqual("Theme 2.1 字段校验通过，已记录。", validMessage);
        Assert.IsFalse(validMessage.Contains("Draft", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(validMessage.Contains("Revision", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod(DisplayName = "主题提交工具应暴露规定工具名")]
    [Timeout(5000)]
    public void CreateToolShouldUseAnalysisSubmissionName()
    {
        var function = new CoursewareThemeSubmissionTool(new CoursewareThemeValidator()).CreateTool();

        Assert.AreEqual("submit_courseware_theme_analysis", function.Name);
    }

    [TestMethod(DisplayName = "主题提交工具Schema应包含必填字号规则")]
    [Timeout(5000)]
    public void CreateToolShouldExposeRequiredFontSizeRules()
    {
        var function = new CoursewareThemeSubmissionTool(new CoursewareThemeValidator()).CreateTool();
        var themeSchema = function.JsonSchema.GetProperty("properties").GetProperty("theme");
        var requiredProperties = themeSchema.GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.IsTrue(themeSchema.GetProperty("properties").TryGetProperty("fontSizeRules", out _));
        CollectionAssert.Contains(requiredProperties, "fontSizeRules");
    }

    [TestMethod(DisplayName = "Theme 2.1序列化应保留字号规则原文")]
    [Timeout(5000)]
    public void SerializationShouldPreserveFontSizeRules()
    {
        var theme = CreateValidTheme();

        var json = JsonSerializer.Serialize(theme, CoursewareExportJsonSerializerContext.Default.CoursewareTheme);
        var restored = JsonSerializer.Deserialize(json, CoursewareExportJsonSerializerContext.Default.CoursewareTheme);

        StringAssert.Contains(json, nameof(CoursewareTheme.FontSizeRules));
        Assert.IsNotNull(restored);
        Assert.AreEqual(theme.FontSizeRules, restored.FontSizeRules);
    }

    private static CoursewareTheme CreateValidTheme()
    {
        return new CoursewareTheme
        {
            ColorSuggestions =
            [
                new CoursewareColorSuggestion { Name = "纸白", Usage = "背景", Hex = "#FFFFFF" },
                new CoursewareColorSuggestion { Name = "墨色", Usage = "正文", Hex = "#0F172A" },
                new CoursewareColorSuggestion { Name = "强调蓝", Usage = "重点", Hex = "#2563EB" },
            ],
            Fonts = new CoursewareFontSuggestions { Chinese = "微软雅黑", Western = "Arial" },
            FontSizeRules = "封面标题 44-52px，内容页标题 30-36px，正文 20-24px，辅助文字不小于 16px。",
            Style = "清晰、克制、现代",
            SafeArea = new CoursewareSafeAreaRatios
            {
                LeftRatio = 0.05,
                TopRatio = 0.05,
                RightRatio = 0.05,
                BottomRatio = 0.05,
            },
            SpacingAndVisualEffects = "保持充足留白，不使用阴影。",
            LayoutPrinciples = "建立清晰网格，保持对齐并突出单一视觉重点。",
            CoverPageSlideMl = "<Page />",
            ContentPageSlideMl = "<Page />",
        };
    }

    private static CoursewareInputPackage CreatePackage(string markdown, double width, double height)
    {
        return new CoursewareInputPackage
        {
            RootDirectory = new DirectoryInfo(Path.GetTempPath()),
            Slides =
            [
                new CoursewareSlideInput
                {
                    SlideIndex = 0,
                    PageNumber = 1,
                    SlideId = "slide-0",
                    Width = width,
                    Height = height,
                    MarkdownText = markdown,
                    MarkdownFile = new FileInfo(Path.Join(Path.GetTempPath(), "slide-0.md")),
                },
            ],
        };
    }

    private sealed class RecordingThemeAgent(CoursewareTheme theme) : ICoursewareThemeAgent
    {
        public CoursewareTheme Theme { get; } = theme;
        public int CallCount { get; private set; }
        public string? Prompt { get; private set; }
        public SlideDocumentContext? ValidationCanvas { get; private set; }

        public Task<CoursewareTheme> AnalyzeAsync(
            string prompt,
            SlideDocumentContext validationCanvas,
            IReadOnlySet<string> availableResourceIds,
            IProgress<CoursewareAnalysisEvent>? progress = null,
            IProgress<CopilotChatMessage>? messageProgress = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            Prompt = prompt;
            ValidationCanvas = validationCanvas;
            return Task.FromResult(Theme);
        }
    }
}
