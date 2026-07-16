using System.IO;
using AgentLib.Model;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Produces a validated whole-courseware theme through an independent language-model conversation.
/// </summary>
public sealed class CoursewareThemeAnalysisService : ICoursewareThemeAnalysisService
{
    private readonly ICoursewareAnalysisInputBuilder _inputBuilder;
    private readonly ICoursewareThemeAgent? _themeAgent;

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
    /// <param name="inputBuilder">The bounded analysis input builder.</param>
    /// <param name="themeAgent">The optional injected theme agent.</param>
    public CoursewareThemeAnalysisService(
        ICoursewareAnalysisInputBuilder inputBuilder,
        ICoursewareThemeAgent? themeAgent)
    {
        ArgumentNullException.ThrowIfNull(inputBuilder);

        _inputBuilder = inputBuilder;
        _themeAgent = themeAgent;
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
            $"正在整理 {inputPackage.SlideCount} 页课件的标题、正文摘要、资源和警告。",
            CoursewareAnalysisEventState.Running));
        var analysisInput = _inputBuilder.Build(inputPackage, cancellationToken);
        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.PreparingInput,
            "分析输入准备完成",
            $"已整理 {analysisInput.AnalyzedSlideCount} 页课件输入。",
            CoursewareAnalysisEventState.Completed));
        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.ReadingStructure,
            "正在读取课件结构",
            "正在识别页面顺序、标题、正文和资源分布。",
            CoursewareAnalysisEventState.Running));
        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.ReadingStructure,
            "课件结构读取完成",
            $"已将 {analysisInput.AnalyzedSlideCount} 页纳入分析输入，共 {analysisInput.CharacterCount} 个字符。",
            CoursewareAnalysisEventState.Completed));
        progress?.Report(CreateProgressEvent(
            CoursewareAnalysisStage.AnalyzingContentHierarchy,
            "正在分析内容层级",
            "正在识别课件主题、章节节奏、页面类型和课堂表达重点。",
            CoursewareAnalysisEventState.Running));

        try
        {
            var dimensions = GetDominantDimensions(inputPackage);
            var agent = _themeAgent ?? CreateDefaultAgent();
            var theme = await agent.AnalyzeAsync(
                analysisInput,
                dimensions.Width,
                dimensions.Height,
                progress,
                messageProgress,
                cancellationToken).ConfigureAwait(false);
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
                Title = "全课件主题分析完成",
                Message = $"已形成“{theme.Title}”主题，可进入页面美化工作台。",
                State = CoursewareAnalysisEventState.Completed,
            });

            return new CoursewareThemeAnalysisResult
            {
                Theme = theme,
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
        return new CopilotCoursewareThemeAgent(chatManagerFactory, new CoursewareThemeValidator());
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
