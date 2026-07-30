using System.IO;
using AgentLib;
using AgentLib.Model;
using CoursewarePptxGeneratorWpfDemo.Models;
using Microsoft.Extensions.AI;
using PptxGenerator.Models;
using PptxGenerator.Prompt;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Uses an independent AgentLib conversation to generate a lightweight courseware theme.
/// </summary>
public sealed class CopilotCoursewareThemeAgent : ICoursewareThemeAgent
{
    private const int MaximumInteractionCount = 3;

    private readonly ICopilotChatManagerFactory _chatManagerFactory;
    private readonly ISlideMlPromptProvider? _promptProvider;
    private readonly CoursewareThemeValidator _themeValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotCoursewareThemeAgent" /> class.
    /// </summary>
    /// <param name="chatManagerFactory">Creates an independent chat manager for each analysis.</param>
    /// <param name="promptProvider">Optional shared SlideML specification provider.</param>
    /// <param name="themeValidator">Optional theme validator used for field and rendered SlideML validation.</param>
    public CopilotCoursewareThemeAgent(
        ICopilotChatManagerFactory chatManagerFactory,
        ISlideMlPromptProvider? promptProvider = null,
        CoursewareThemeValidator? themeValidator = null)
    {
        ArgumentNullException.ThrowIfNull(chatManagerFactory);
        _chatManagerFactory = chatManagerFactory;
        _promptProvider = promptProvider;
        _themeValidator = themeValidator ?? new CoursewareThemeValidator();
    }

    /// <inheritdoc />
    public async Task<CoursewareTheme> AnalyzeAsync(
        string prompt,
        SlideDocumentContext validationCanvas,
        IReadOnlySet<string> availableResourceIds,
        IProgress<CoursewareAnalysisEvent>? progress = null,
        IProgress<CopilotChatMessage>? messageProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("主题分析 Prompt 不能为空。", nameof(prompt));
        }
        ArgumentNullException.ThrowIfNull(validationCanvas);
        ArgumentNullException.ThrowIfNull(availableResourceIds);
        cancellationToken.ThrowIfCancellationRequested();

        var chatManager = await _chatManagerFactory.CreateAsync(
            AgentWorkload.ThemeAnalysis,
            cancellationToken).ConfigureAwait(false);
        chatManager.CreateNewSession();

        var promptProvider = _promptProvider ?? new SlideMlPromptProvider(validationCanvas);
        var systemPrompt = BuildSystemPrompt(promptProvider.BuildCompleteDocumentSpecificationPrompt());
        var submissionTool = new CoursewareThemeSubmissionTool(_themeValidator);
        var aiFunction = submissionTool.CreateTool();
        IReadOnlyList<AITool> tools = [aiFunction];
        var currentPrompt = prompt;
        IReadOnlyList<string> latestProblems = [];

        for (var interaction = 1; interaction <= MaximumInteractionCount; interaction++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(CreateProgressEvent(
                interaction == 1 ? CoursewareAnalysisStage.DesigningTheme : CoursewareAnalysisStage.RepairingTheme,
                CoursewareAnalysisEventState.Running,
                interaction == 1 ? "正在生成主题建议" : "正在完善主题建议",
                interaction == 1 ? "正在整理统一的配色、字体与版式建议。" : $"正在调整 {latestProblems.Count} 项未通过检查的内容。"));

            CoursewareModelContextBudgetValidator.ValidateIfConfigured(
                chatManager.AgentApiEndpointManager.PrimaryModel.ModelDefinition,
                systemPrompt,
                currentPrompt,
                aiFunction,
                cancellationToken);

            var submissionCountBeforeRound = submissionTool.SubmissionCount;
            var sendResult = chatManager.SendMessage(new SendMessageRequest(currentPrompt)
            {
                SystemPrompt = systemPrompt,
                WithHistory = true,
                CreateNewSession = false,
                Tools = tools,
                AppendDefaultTools = false,
                CancellationToken = cancellationToken,
            });
            messageProgress?.Report(sendResult.AssistantChatMessage);
            var runState = await sendResult.RunTask.ConfigureAwait(false);

            if (!runState.IsSuccess)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new InvalidOperationException($"主题分析第 {interaction} 轮调用失败：{sendResult.AssistantChatMessage.Content}");
            }

            latestProblems = await GetValidationProblemsAsync(
                submissionTool,
                submissionCountBeforeRound,
                validationCanvas,
                availableResourceIds,
                cancellationToken).ConfigureAwait(false);
            if (latestProblems.Count == 0 && submissionTool.SubmittedTheme is { } submittedTheme)
            {
                progress?.Report(CreateProgressEvent(
                    CoursewareAnalysisStage.ValidatingTheme,
                    CoursewareAnalysisEventState.Completed,
                    "主题建议已通过检查",
                    "配色、字体、版式与页面设计参考均已准备完成。"));
                return submittedTheme;
            }

            progress?.Report(CreateProgressEvent(
                CoursewareAnalysisStage.RepairingTheme,
                interaction == MaximumInteractionCount
                    ? CoursewareAnalysisEventState.Failed
                    : CoursewareAnalysisEventState.Warning,
                "主题建议需要继续调整",
                $"发现 {latestProblems.Count} 项需要修正的内容，正在继续处理。"));

            if (interaction < MaximumInteractionCount)
            {
                currentPrompt = BuildRepairPrompt(latestProblems);
            }
        }

        throw new InvalidDataException(
            $"主题分析经过 {MaximumInteractionCount} 轮仍未通过：{string.Join("；", latestProblems)}");
    }

    private static string BuildSystemPrompt(string completeDocumentSpecification)
    {
        return $"""
你是课件全局主题分析 Agent。必须遵守以下规则：
- 只调用 submit_courseware_theme_analysis；不要调用、提及或模拟其他工具。
- 必须通过该工具提交 Theme 2.1，SchemaVersion 必须精确为 2.1。
- FontSizeRules 必须总结整课件的字号层级和使用方式，并保留为可直接用于页面美化的规则原文。
- CoverPageSlideMl 与 ContentPageSlideMl 必须分别是完整、可渲染的 SlideML Page XML 文档。
- 禁止输出流式 SlideML 协议、片段补丁、Remove、StyleFrom、StyleId、TargetId 或 get_slide_state。
- 不要在工具调用之外输出主题结果；收到校验问题后修正全部问题并重新调用同一工具。

{completeDocumentSpecification}
""";
    }

    private async Task<IReadOnlyList<string>> GetValidationProblemsAsync(
        CoursewareThemeSubmissionTool submissionTool,
        int submissionCountBeforeRound,
        SlideDocumentContext validationCanvas,
        IReadOnlySet<string> availableResourceIds,
        CancellationToken cancellationToken)
    {
        if (submissionTool.SubmissionCount == submissionCountBeforeRound)
        {
            return ["未调用 submit_courseware_theme_analysis。"];
        }

        if (submissionTool.ValidationErrors.Count > 0)
        {
            return submissionTool.ValidationErrors;
        }

        if (submissionTool.SubmittedTheme is not { } submittedTheme)
        {
            return ["工具未提交可用的 Theme 2.1。"];
        }

        var validationResult = await _themeValidator.ValidateAsync(
            submittedTheme,
            validationCanvas,
            availableResourceIds,
            cancellationToken).ConfigureAwait(false);
        return validationResult.Errors;
    }

    private static string BuildRepairPrompt(IReadOnlyList<string> problems)
    {
        return "请修复以下问题并重新调用 submit_courseware_theme_analysis：\n- "
            + string.Join("\n- ", problems);
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
            Kind = state == CoursewareAnalysisEventState.Failed
                ? CoursewareAnalysisEventKind.Error
                : CoursewareAnalysisEventKind.Progress,
            State = state,
            Title = title,
            Message = message,
        };
    }
}