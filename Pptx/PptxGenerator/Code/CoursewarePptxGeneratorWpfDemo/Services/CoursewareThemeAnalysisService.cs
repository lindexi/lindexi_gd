using AgentLib.Model;
using System.IO;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Produces a lightweight whole-courseware theme through an independent language-model conversation.
/// </summary>
public sealed class CoursewareThemeAnalysisService : ICoursewareThemeAnalysisService
{
    private readonly CoursewareStyleUsageSummaryBuilder _styleUsageSummaryBuilder;
    private readonly ICoursewareThemeAnalysisPromptBuilder _promptBuilder;
    private readonly ICoursewareThemeAgent _themeAgent;
    private readonly CoursewareThemeValidator _themeValidator;

    /// <summary>
    /// Initializes a service that resolves language-model configuration when analysis starts.
    /// </summary>
    public CoursewareThemeAnalysisService()
        : this(
            new CoursewareStyleUsageSummaryBuilder(),
            new CoursewareThemeAnalysisPromptBuilder(),
            new CopilotCoursewareThemeAgent(new CopilotChatManagerFactory()),
            new CoursewareThemeValidator())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareThemeAnalysisService" /> class.
    /// </summary>
    public CoursewareThemeAnalysisService(
        CoursewareStyleUsageSummaryBuilder styleUsageSummaryBuilder,
        ICoursewareThemeAnalysisPromptBuilder promptBuilder,
        ICoursewareThemeAgent themeAgent,
        CoursewareThemeValidator? themeValidator = null)
    {
        ArgumentNullException.ThrowIfNull(styleUsageSummaryBuilder);
        ArgumentNullException.ThrowIfNull(promptBuilder);
        ArgumentNullException.ThrowIfNull(themeAgent);

        _styleUsageSummaryBuilder = styleUsageSummaryBuilder;
        _promptBuilder = promptBuilder;
        _themeAgent = themeAgent;
        _themeValidator = themeValidator ?? new CoursewareThemeValidator();
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
        if (inputPackage.Slides.Count == 0)
        {
            throw new ArgumentException("课件必须至少包含一页。", nameof(inputPackage));
        }

        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.PreparingInput,
            CoursewareAnalysisEventState.Running,
            "准备主题分析输入",
            "正在汇总整份课件的页面内容与样式参考。"));

        var styleUsageSummary = _styleUsageSummaryBuilder.Build(inputPackage, cancellationToken);
        var prompt = _promptBuilder.Build(inputPackage, styleUsageSummary);
        var validationCanvas = CoursewareCanvasAdapter.CreateDocumentContext(inputPackage.Slides[0]);
        var availableResourceIds = inputPackage.Resources
            .Where(resource => !string.IsNullOrWhiteSpace(resource.ResourceId))
            .Select(resource => resource.ResourceId!)
            .ToHashSet(StringComparer.Ordinal);

        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.PreparingInput,
            CoursewareAnalysisEventState.Completed,
            "主题分析输入已准备",
            "已完成页面内容与样式参考汇总。"));
        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.DesigningTheme,
            CoursewareAnalysisEventState.Running,
            "分析课件主题",
            "正在生成统一的课件视觉主题。"));

        var theme = await _themeAgent.AnalyzeAsync(
            prompt,
            validationCanvas,
            availableResourceIds,
            progress,
            messageProgress,
            cancellationToken);
        var validationResult = await _themeValidator.ValidateAsync(
            theme,
            validationCanvas,
            availableResourceIds,
            cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            throw new InvalidDataException("课件主题验证失败：" + Environment.NewLine + string.Join(Environment.NewLine, validationResult.Errors));
        }

        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.Completed,
            CoursewareAnalysisEventState.Completed,
            "主题分析完成",
            "已生成课件全局主题。"));

        return new CoursewareThemeAnalysisResult { Theme = theme };
    }

    private static CoursewareAnalysisEvent CreateProgressEvent(
        CoursewareAnalysisStage stage,
        CoursewareAnalysisEventState state,
        string title,
        string message)
    {
        return new CoursewareAnalysisEvent
        {
            Stage = stage,
            Kind = CoursewareAnalysisEventKind.Progress,
            State = state,
            Title = title,
            Message = message,
        };
    }
}