using AgentLib.Model;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Services;

namespace CoursewarePptxGeneratorWpfDemo.Tests.Fakes;

internal sealed class FakeCoursewareThemeAnalysisService : ICoursewareThemeAnalysisService
{
    private readonly Func<CoursewareInputPackage, IProgress<CoursewareAnalysisEvent>?, IProgress<CopilotChatMessage>?, CancellationToken, Task<CoursewareThemeAnalysisResult>> _analyzeAsync;

    public FakeCoursewareThemeAnalysisService(
        Func<CoursewareInputPackage, IProgress<CoursewareAnalysisEvent>?, IProgress<CopilotChatMessage>?, CancellationToken, Task<CoursewareThemeAnalysisResult>>? analyzeAsync = null)
    {
        _analyzeAsync = analyzeAsync ?? CreateSuccessfulResultAsync;
    }

    public int AnalysisCount { get; private set; }

    public Task<CoursewareThemeAnalysisResult> AnalyzeAsync(
        CoursewareInputPackage inputPackage,
        IProgress<CoursewareAnalysisEvent>? progress = null,
        IProgress<CopilotChatMessage>? messageProgress = null,
        CancellationToken cancellationToken = default)
    {
        AnalysisCount++;
        return _analyzeAsync(inputPackage, progress, messageProgress, cancellationToken);
    }

    private static Task<CoursewareThemeAnalysisResult> CreateSuccessfulResultAsync(
        CoursewareInputPackage inputPackage,
        IProgress<CoursewareAnalysisEvent>? progress,
        IProgress<CopilotChatMessage>? messageProgress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateSuccessfulResult(inputPackage));
    }

    internal static CoursewareThemeAnalysisResult CreateSuccessfulResult(CoursewareInputPackage inputPackage)
    {
        var designSystem = CreateDesignSystem(inputPackage);
        return new CoursewareThemeAnalysisResult
        {
            Theme = CreateTheme(),
            DesignSystem = designSystem,
            StructuredFacts = new CoursewareStructuredFactReport
            {
                Slides = inputPackage.Slides.Select(slide => new CoursewareSlideStructuredFacts
                {
                    SlideId = slide.SlideId,
                    PageNumber = slide.PageNumber,
                    Width = slide.Width,
                    Height = slide.Height,
                }).ToArray(),
            },
            DesignSystemValidation = new CoursewareDesignSystemValidationReport
            {
                IsValid = true,
                CoveredSlideCount = inputPackage.SlideCount,
                TotalSlideCount = inputPackage.SlideCount,
            },
            TemplateValidation = new CoursewareTemplateValidationReport
            {
                IsValid = true,
                TemplateCount = 1,
                PassedTemplateCount = 1,
            },
            ReferenceCanvas = CoursewareCanvasAdapter.CreateDocumentContext(
                inputPackage.Slides[0].Width,
                inputPackage.Slides[0].Height),
            CapabilityStates = new CoursewareAnalysisCapabilityStates
            {
                TextAnalysis = CoursewareCapabilityStatus.Passed,
                ThemeSuggestion = CoursewareCapabilityStatus.Passed,
                DesignSystem = CoursewareCapabilityStatus.Passed,
                TemplateValidation = CoursewareCapabilityStatus.Passed,
                VisualAnalysis = CoursewareCapabilityStatus.NotRequested,
                PageGeneration = CoursewareCapabilityStatus.NotRequested,
            },
            AnalyzedAt = DateTimeOffset.UtcNow,
            TotalSlideCount = inputPackage.SlideCount,
            AnalyzedSlideCount = inputPackage.SlideCount,
            Duration = TimeSpan.FromSeconds(1),
        };
    }

    private static CoursewareDesignSystem CreateDesignSystem(CoursewareInputPackage inputPackage)
    {
        return new CoursewareDesignSystem
        {
            DesignSystemId = "fake-design-system",
            DesignIntent = new CoursewareDesignIntent { Name = "测试主题", Summary = "测试主题说明", Rationale = "测试" },
            CanvasProfiles = [new CoursewareCanvasDesignProfile { CanvasId = "primary", Width = inputPackage.Slides[0].Width, Height = inputPackage.Slides[0].Height, IsPrimary = true }],
            Grid = new CoursewareGridSystem { SafeLeft = 60, SafeTop = 40, SafeRight = 60, SafeBottom = 40, ColumnCount = 12, Gutter = 24, Baseline = 8 },
            Spacing = new CoursewareSpacingScale { Tokens = [new CoursewareSpacingToken { TokenId = "space-md", Value = 24, Purpose = "间距" }] },
            Typography = new CoursewareTypographySystem
            {
                Tokens =
                [
                    new CoursewareTypographyToken { TokenId = "title", EastAsianFontStack = ["微软雅黑"], LatinFontStack = ["Arial"], FontSize = 32, FontWeight = "Bold", LineHeight = 1.2, Purpose = "标题", AllowedPageTypeIds = ["content"] },
                    new CoursewareTypographyToken { TokenId = "body", EastAsianFontStack = ["微软雅黑"], LatinFontStack = ["Arial"], FontSize = 18, FontWeight = "Regular", LineHeight = 1.4, Purpose = "正文", AllowedPageTypeIds = ["content"] },
                ],
            },
            Colors = new CoursewareColorSystem
            {
                Tokens =
                [
                    new CoursewareColorToken { TokenId = "background-page", HexValue = "#FFFFFF", Purpose = "背景", SourceKind = CoursewareEvidenceSourceKind.DesignInference },
                    new CoursewareColorToken { TokenId = "text-primary", HexValue = "#0F172A", Purpose = "文字", SourceKind = CoursewareEvidenceSourceKind.DesignInference },
                ],
            },
            Effects = new CoursewareEffectSystem(),
            AssetPolicy = new CoursewareAssetUsagePolicy(),
            PageTypes =
            [
                new CoursewarePageTypeContract
                {
                    PageTypeId = "content",
                    Name = "内容页",
                    Purpose = "测试",
                    EvidenceReferences = [new CoursewareEvidenceReference { SourceKind = CoursewareEvidenceSourceKind.SlideMarkdownFact, SlideId = inputPackage.Slides[0].SlideId, PageNumber = 1, Description = "测试" }],
                    DensityRange = new CoursewareContentDensityRange { Minimum = 0, Maximum = 1 },
                    Slots = [new CoursewareTemplateSlot { SlotId = "title", SlotKind = "Text", IsRequired = true, Purpose = "标题" }],
                    OverflowStrategy = "Split",
                },
            ],
            PageTypeAssignments = inputPackage.Slides.Select(slide => new CoursewarePageTypeAssignment { SlideId = slide.SlideId, PageTypeId = "content" }).ToArray(),
            Accessibility = new CoursewareAccessibilityRules { MinimumBodyFontSize = 14, MinimumNormalTextContrastRatio = 4.5, MinimumLargeTextContrastRatio = 3 },
            Consistency = new CoursewareConsistencyRules(),
        };
    }

    private static CoursewareTheme CreateTheme()
    {
        return new CoursewareTheme
        {
            Title = "测试主题",
            Summary = "测试主题说明",
            StyleKeywords = ["清晰", "克制", "现代"],
            Colors = new CoursewareColorScheme
            {
                Primary = CreateColor("主色", "蓝色", "#2563EB"),
                Accent = CreateColor("强调色", "橙色", "#F59E0B"),
                Background = CreateColor("背景色", "白色", "#FFFFFF"),
                PrimaryText = CreateColor("主文字", "深色", "#0F172A"),
                SecondaryText = CreateColor("次文字", "灰色", "#475569"),
                Rationale = "测试配色说明",
            },
            Typography = new CoursewareTypography
            {
                PrimaryHeading = CreateTypography("一级标题", 32),
                SecondaryHeading = CreateTypography("二级标题", 24),
                Body = CreateTypography("正文", 18),
                Supporting = CreateTypography("辅助文字", 14),
            },
            Fonts = new CoursewareFontRecommendation
            {
                EastAsianHeading = "微软雅黑",
                EastAsianBody = "微软雅黑",
                LatinHeading = "Arial",
                LatinBody = "Arial",
            },
            SafeArea = new CoursewareSafeArea { Left = 60, Top = 40, Right = 60, Bottom = 40 },
            LayoutPrinciples = ["保持对齐", "突出重点", "控制留白"],
            PageTypes = new CoursewarePageTypeRecommendations
            {
                Cover = CreatePageType("封面页"),
                Section = CreatePageType("章节页"),
                Content = CreatePageType("内容页"),
                Ending = CreatePageType("结束页"),
            },
            ContentPresentationRules = ["保持语义准确", "结论重点突出"],
            GenerationPromptSummary = "使用统一蓝橙配色和四级字号。",
        };
    }

    private static CoursewareThemeColor CreateColor(string usage, string name, string hexValue)
    {
        return new CoursewareThemeColor { Usage = usage, Name = name, HexValue = hexValue };
    }

    private static CoursewareTypographyLevel CreateTypography(string name, double fontSize)
    {
        return new CoursewareTypographyLevel
        {
            Name = name,
            FontSize = fontSize,
            FontWeight = "Regular",
            Purpose = "测试",
        };
    }

    private static CoursewarePageTypeRecommendation CreatePageType(string name)
    {
        return new CoursewarePageTypeRecommendation { Name = name, Description = "测试建议" };
    }
}
