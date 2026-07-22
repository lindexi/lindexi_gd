using AgentLib.Model;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;
using CoursewarePptxGeneratorWpfDemo.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGeneratorWpfDemo.Tests;

[TestClass]
public sealed class CoursewareThemeAnalysisServiceTests
{
    [TestMethod(DisplayName = "正式分析服务应返回 Agent 主题并发布业务过程事件")]
    [Timeout(60_000)]
    public async Task AnalyzeAsyncShouldReturnAgentThemeAndPublishEvents()
    {
        var exportDirectory = new TestCoursewareExportBuilder()
            .AddSlide("slide-first", "#### 内容\n```\n测试标题\n测试内容\n```")
            .Build();
        var package = await new CoursewareFolderLoader().LoadAsync(exportDirectory.FullName);
        var events = new List<CoursewareAnalysisEvent>();
        var service = new CoursewareThemeAnalysisService(
            new CoursewareAnalysisInputBuilder(),
            new FakeThemeAgent(),
            new CoursewareTemplateStressTester(
                new CoursewareTemplateCompiler(),
                _ => new FakeSlideMlRenderPipeline()));

        var result = await service.AnalyzeAsync(
            package,
            new SynchronousProgress<CoursewareAnalysisEvent>(events.Add));

        Assert.AreEqual("清晰课堂", result.Theme.Title);
        Assert.AreEqual(1, result.AnalyzedSlideCount);
        Assert.AreEqual(1280, result.ReferenceCanvas.CanvasWidth);
        Assert.AreEqual(720, result.ReferenceCanvas.CanvasHeight);
        Assert.AreEqual(CoursewareCapabilityStatus.Passed, result.CapabilityStates.TextAnalysis);
        Assert.AreEqual(CoursewareCapabilityStatus.Passed, result.CapabilityStates.ThemeSuggestion);
        Assert.AreEqual(CoursewareCapabilityStatus.Passed, result.CapabilityStates.DesignSystem);
        Assert.AreEqual(CoursewareCapabilityStatus.Passed, result.CapabilityStates.TemplateValidation);
        Assert.AreEqual(CoursewareCapabilityStatus.NotRequested, result.CapabilityStates.VisualAnalysis);
        Assert.AreEqual(CoursewareCapabilityStatus.NotRequested, result.CapabilityStates.PageGeneration);
        Assert.IsTrue(events.Any(item => item.Stage == CoursewareAnalysisStage.PreparingInput));
        var completedEvent = events.Single(item => item.Stage == CoursewareAnalysisStage.Completed);
        Assert.AreEqual("课件文本分析完成", completedEvent.Title);
        StringAssert.Contains(completedEvent.Message, "可进入页面美化工作台");
        Assert.IsTrue(result.DesignSystemValidation.IsValid);
        Assert.IsTrue(result.TemplateValidation.IsValid);
    }

    private sealed class FakeThemeAgent : ICoursewareThemeAgent
    {
        public Task<CoursewareDesignSystemAgentResult> AnalyzeAsync(
            CoursewareAnalysisInput analysisInput,
            CoursewareInputPackage inputPackage,
            CoursewareStructuredFactReport structuredFacts,
            IReadOnlyList<CoursewareVisualSample> visualSamples,
            IProgress<CoursewareAnalysisEvent>? progress = null,
            IProgress<CopilotChatMessage>? messageProgress = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new CoursewareAnalysisEvent
            {
                Stage = CoursewareAnalysisStage.DesigningTheme,
                Kind = CoursewareAnalysisEventKind.Progress,
                Title = "测试 Agent",
                Message = "正在生成测试主题。",
                State = CoursewareAnalysisEventState.Running,
            });
            var designSystem = CreateDesignSystem(inputPackage.Slides[0].SlideId);
            var validation = new CoursewareDesignSystemValidator().Validate(
                designSystem,
                inputPackage.Slides.Select(slide => slide.SlideId),
                inputPackage.Resources.Select(resource => resource.ResourceId ?? string.Empty),
                visualAnalysisExecuted: false);
            return Task.FromResult(new CoursewareDesignSystemAgentResult
            {
                DesignSystem = designSystem,
                Validation = validation,
            });
        }

        private static CoursewareDesignSystem CreateDesignSystem(string slideId)
        {
            return new CoursewareDesignSystem
            {
                DesignSystemId = "test-design-system",
                DesignIntent = new CoursewareDesignIntent
                {
                    Name = "清晰课堂",
                    Summary = "测试可执行设计系统。",
                    Rationale = "用于验证分析服务。",
                },
                CanvasProfiles = [new CoursewareCanvasDesignProfile { CanvasId = "primary", Width = 1280, Height = 720, IsPrimary = true }],
                Grid = new CoursewareGridSystem { SafeLeft = 64, SafeTop = 48, SafeRight = 64, SafeBottom = 48, ColumnCount = 12, Gutter = 24, Baseline = 8 },
                Spacing = new CoursewareSpacingScale { Tokens = [new CoursewareSpacingToken { TokenId = "space-md", Value = 24, Purpose = "间距" }] },
                Typography = new CoursewareTypographySystem
                {
                    Tokens =
                    [
                        new CoursewareTypographyToken { TokenId = "title", EastAsianFontStack = ["Microsoft YaHei"], LatinFontStack = ["Arial"], FontSize = 48, FontWeight = "Bold", LineHeight = 1.2, Purpose = "标题", AllowedPageTypeIds = ["content"] },
                        new CoursewareTypographyToken { TokenId = "body", EastAsianFontStack = ["Microsoft YaHei"], LatinFontStack = ["Arial"], FontSize = 28, FontWeight = "Regular", LineHeight = 1.4, Purpose = "正文", AllowedPageTypeIds = ["content"] },
                    ],
                },
                Colors = new CoursewareColorSystem
                {
                    Tokens =
                    [
                        new CoursewareColorToken { TokenId = "background-page", HexValue = "#FFFFFF", Purpose = "背景", SourceKind = CoursewareEvidenceSourceKind.DesignInference },
                        new CoursewareColorToken { TokenId = "text-primary", HexValue = "#0F172A", Purpose = "文字", AllowedBackgroundTokenIds = ["background-page"], SourceKind = CoursewareEvidenceSourceKind.DesignInference },
                    ],
                },
                Effects = new CoursewareEffectSystem(),
                Components = [new CoursewareComponentSpecification { ComponentId = "title-block", Name = "标题", Purpose = "标题组件", TokenIds = ["title", "text-primary"] }],
                AssetPolicy = new CoursewareAssetUsagePolicy(),
                PageTypes =
                [
                    new CoursewarePageTypeContract
                    {
                        PageTypeId = "content",
                        Name = "内容页",
                        Purpose = "承载教学内容",
                        EvidenceReferences = [new CoursewareEvidenceReference { SourceKind = CoursewareEvidenceSourceKind.SlideMarkdownFact, SlideId = slideId, PageNumber = 1, Description = "测试页" }],
                        DensityRange = new CoursewareContentDensityRange { Minimum = 0, Maximum = 1 },
                        Slots = [new CoursewareTemplateSlot { SlotId = "title", SlotKind = "Text", IsRequired = true, Purpose = "标题" }],
                        ComponentIds = ["title-block"],
                        OverflowStrategy = "Split",
                    },
                ],
                PageTypeAssignments = [new CoursewarePageTypeAssignment { SlideId = slideId, PageTypeId = "content", Confidence = 1 }],
                PageTemplates =
                [
                    new CoursewarePageTemplate
                    {
                        TemplateId = "content-template",
                        PageTypeId = "content",
                        Name = "内容模板",
                        Purpose = "测试模板",
                        DensityRange = new CoursewareContentDensityRange { Minimum = 0, Maximum = 1 },
                        Slots = [new CoursewareTemplateSlot { SlotId = "title", SlotKind = "Text", IsRequired = true, Purpose = "标题" }],
                        SlideMlTemplate = "<Page Id=\"page\" Background=\"{{token:background-page}}\"><TextElement Id=\"title\" X=\"64\" Y=\"48\" Width=\"1152\" Height=\"120\" Text=\"{{slot:title}}\" FontName=\"{{token:title.fontName}}\" FontSize=\"{{token:title}}\" Foreground=\"{{token:text-primary}}\" /></Page>",
                    },
                ],
                Accessibility = new CoursewareAccessibilityRules { MinimumBodyFontSize = 24, MinimumNormalTextContrastRatio = 4.5, MinimumLargeTextContrastRatio = 3 },
                Consistency = new CoursewareConsistencyRules { Invariants = ["字体", "颜色"] },
            };
        }
    }

    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public SynchronousProgress(Action<T> handler)
        {
            _handler = handler;
        }

        public void Report(T value)
        {
            _handler(value);
        }
    }
}