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

    internal static CoursewareThemeAnalysisResult CreateSuccessfulResult(CoursewareInputPackage inputPackage)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);
        return new CoursewareThemeAnalysisResult
        {
            Theme = new CoursewareTheme
            {
                ColorSuggestions =
                [
                    new CoursewareColorSuggestion { Name = "纸白", Usage = "背景", Hex = "#FFFFFF" },
                    new CoursewareColorSuggestion { Name = "墨色", Usage = "正文", Hex = "#0F172A" },
                    new CoursewareColorSuggestion { Name = "强调蓝", Usage = "强调", Hex = "#2563EB" },
                ],
                Fonts = new CoursewareFontSuggestions { Chinese = "微软雅黑", Western = "Arial" },
                Style = "清晰、克制、现代",
                SafeArea = new CoursewareSafeAreaRatios
                {
                    LeftRatio = 0.05,
                    TopRatio = 0.05,
                    RightRatio = 0.05,
                    BottomRatio = 0.05,
                },
                SpacingAndVisualEffects = "保持充足留白，不使用阴影。",
                LayoutPrinciples = "保持对齐，突出重点，并控制留白。",
                CoverPageSlideMl = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Page />",
                ContentPageSlideMl = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Page />",
            },
        };
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
}