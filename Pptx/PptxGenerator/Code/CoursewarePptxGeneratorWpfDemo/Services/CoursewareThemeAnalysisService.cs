using System.IO;
using AgentLib.Model;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;
using CoursewarePptxGeneratorWpfDemo.Resources;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Produces a validated whole-courseware theme through an independent language-model conversation.
/// </summary>
public sealed class CoursewareThemeAnalysisService : ICoursewareThemeAnalysisService
{
    private readonly ICoursewareAnalysisInputBuilder _inputBuilder;
    private readonly ICoursewareThemeAgent? _themeAgent;
    private readonly CoursewareStructuredFactBuilder _factBuilder;
    private readonly CoursewareVisualSampleSelector _visualSampleSelector;
    private readonly CoursewareTemplateStressTester _templateStressTester;

    /// <summary>
    /// Initializes a service that resolves language-model configuration when analysis starts.
    /// </summary>
    public CoursewareThemeAnalysisService()
        : this(new CoursewareAnalysisInputBuilder(), themeAgent: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareThemeAnalysisService" /> class.
    /// </summary>
    /// <param name="inputBuilder">The complete analysis input builder.</param>
    /// <param name="themeAgent">The optional injected theme agent.</param>
    public CoursewareThemeAnalysisService(
        ICoursewareAnalysisInputBuilder inputBuilder,
        ICoursewareThemeAgent? themeAgent,
        CoursewareTemplateStressTester? templateStressTester = null)
    {
        ArgumentNullException.ThrowIfNull(inputBuilder);

        _inputBuilder = inputBuilder;
        _themeAgent = themeAgent;
        _factBuilder = new CoursewareStructuredFactBuilder();
        _visualSampleSelector = new CoursewareVisualSampleSelector();
        _templateStressTester = templateStressTester ?? new CoursewareTemplateStressTester();
    }

    /// <inheritdoc />
    public async Task<CoursewareThemeAnalysisResult> AnalyzeAsync(
        CoursewareInputPackage inputPackage,
        IProgress<CoursewareAnalysisEvent>? progress = null,
        IProgress<CopilotChatMessage>? messageProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);
        cancellationToken.ThrowIfCancellationRequested();

        var startedAt = DateTimeOffset.UtcNow;
        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.PreparingInput,
            "正在准备分析输入",
            $"正在整理 {inputPackage.SlideCount} 页课件的完整 Markdown、逻辑资源目录和加载警告。",
            CoursewareAnalysisEventState.Running));
        var analysisInput = _inputBuilder.Build(inputPackage, cancellationToken);
        var inputFingerprintPrefix = analysisInput.InputFingerprint[..Math.Min(12, analysisInput.InputFingerprint.Length)];
        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.PreparingInput,
            "分析输入准备完成",
            $"已完整整理 {analysisInput.AnalyzedSlideCount}/{analysisInput.TotalSlideCount} 页，"
            + $"约 {analysisInput.EstimatedTokenCount} Token，未裁剪；输入指纹 {inputFingerprintPrefix}。",
            CoursewareAnalysisEventState.Completed));
        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.ReadingStructure,
            "正在读取课件结构",
            "正在基于全部页面原始 Markdown 识别页面顺序、内容层级、版式参数和资源引用。",
            CoursewareAnalysisEventState.Running));
        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.ReadingStructure,
            "课件结构读取完成",
            $"全部 {analysisInput.AnalyzedSlideCount} 页均已纳入分析输入，共 {analysisInput.CharacterCount} 个字符，"
            + $"页面分区 {analysisInput.SectionCharacterCounts.Slides} 个字符。",
            CoursewareAnalysisEventState.Completed));
        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.AnalyzingContentHierarchy,
            "正在分析内容层级",
            "正在识别课件主题、章节节奏、页面类型和课堂表达重点。",
            CoursewareAnalysisEventState.Running));

        try
        {
            var structuredFacts = _factBuilder.Build(analysisInput, cancellationToken);
            var visualSamples = _visualSampleSelector.Select(inputPackage, structuredFacts);
            var dimensions = GetDominantDimensions(inputPackage);
            var referenceCanvas = CoursewareCanvasAdapter.CreateDocumentContext(dimensions.Width, dimensions.Height);
            var agent = _themeAgent ?? CreateDefaultAgent();
            var designResult = await agent.AnalyzeAsync(
                analysisInput,
                inputPackage,
                structuredFacts,
                visualSamples,
                progress,
                messageProgress,
                cancellationToken).ConfigureAwait(false);
            var templateValidation = await _templateStressTester.ValidateAsync(
                designResult.DesignSystem,
                structuredFacts,
                inputPackage.Resources
                    .Where(resource => resource.Exists && !string.IsNullOrWhiteSpace(resource.ResourceId))
                    .Select(resource => resource.ResourceId!)
                    .ToHashSet(StringComparer.Ordinal),
                cancellationToken).ConfigureAwait(false);
            var theme = CoursewareDesignSystemThemeAdapter.CreateTheme(designResult.DesignSystem);
            progress?.Report(CreateProgressEvent(
                CoursewareAnalysisStage.AnalyzingContentHierarchy,
                "内容层级分析完成",
                "已完成课件主题、章节节奏、页面类型和课堂表达重点分析。",
                CoursewareAnalysisEventState.Completed));

            var warnings = inputPackage.Warnings
                .Select(warning => warning.Message)
                .Concat(analysisInput.Warnings)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var completedAt = DateTimeOffset.UtcNow;
            progress?.Report(new CoursewareAnalysisEvent
            {
                Stage = CoursewareAnalysisStage.Completed,
                Kind = CoursewareAnalysisEventKind.Progress,
                Title = CoursewareUiStrings.AnalysisCompletedTitle,
                Message = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    CoursewareUiStrings.AnalysisCompletedMessageFormat,
                    theme.Title),
                State = CoursewareAnalysisEventState.Completed,
            });

            return new CoursewareThemeAnalysisResult
            {
                Theme = theme,
                DesignSystem = designResult.DesignSystem,
                StructuredFacts = structuredFacts,
                DesignSystemValidation = designResult.Validation,
                TemplateValidation = templateValidation,
                VisualAnalysis = designResult.VisualAnalysis,
                ReferenceCanvas = referenceCanvas,
                CapabilityStates = new CoursewareAnalysisCapabilityStates
                {
                    TextAnalysis = CoursewareCapabilityStatus.Passed,
                    ThemeSuggestion = CoursewareCapabilityStatus.Passed,
                    DesignSystem = designResult.Validation.IsValid ? CoursewareCapabilityStatus.Passed : CoursewareCapabilityStatus.Failed,
                    TemplateValidation = templateValidation.IsValid ? CoursewareCapabilityStatus.Passed : CoursewareCapabilityStatus.Failed,
                    VisualAnalysis = designResult.VisualAnalysis.Observations.Count > 0
                        ? CoursewareCapabilityStatus.Passed
                        : designResult.VisualAnalysis.WasRequested && !designResult.VisualAnalysis.ModelSupportedImages
                            ? CoursewareCapabilityStatus.NotSupported
                            : CoursewareCapabilityStatus.NotRequested,
                    PageGeneration = CoursewareCapabilityStatus.NotRequested,
                },
                AnalyzedAt = completedAt,
                TotalSlideCount = inputPackage.SlideCount,
                AnalyzedSlideCount = analysisInput.AnalyzedSlideCount,
                Duration = completedAt - startedAt,
                Warnings = warnings,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is System.IO.FileNotFoundException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException("无法加载课件主题分析所需的语言模型配置。", ex);
        }
    }

    private static ICoursewareThemeAgent CreateDefaultAgent()
    {
        var chatManagerFactory = new CopilotChatManagerFactory();
        return new CopilotCoursewareThemeAgent(chatManagerFactory, new CoursewareDesignSystemValidator());
    }

    private static (double Width, double Height) GetDominantDimensions(CoursewareInputPackage inputPackage)
    {
        var dimensions = inputPackage.Slides
            .GroupBy(slide => (slide.Width, slide.Height))
            .OrderByDescending(group => group.Count())
            .First().Key;
        return dimensions;
    }

    private static CoursewareAnalysisEvent CreateProgressEvent(
        CoursewareAnalysisStage stage,
        string title,
        string message,
        CoursewareAnalysisEventState state)
    {
        return new CoursewareAnalysisEvent
        {
            Stage = stage,
            Kind = CoursewareAnalysisEventKind.Progress,
            Title = title,
            Message = message,
            State = state,
        };
    }
}
