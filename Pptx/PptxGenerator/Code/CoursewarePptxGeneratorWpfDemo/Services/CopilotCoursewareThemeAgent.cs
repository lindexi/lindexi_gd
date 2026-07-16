using AgentLib.Model;
using CoursewarePptxGeneratorWpfDemo.Models;
using Microsoft.Extensions.AI;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Uses an independent AgentLib conversation to generate a validated courseware theme.
/// </summary>
public sealed class CopilotCoursewareThemeAgent : ICoursewareThemeAgent
{
    private const int MaximumRequestCount = 2;
    private const string SystemPrompt = """
        你是课件全局视觉主题分析器。请根据用户提供的整份课件文本快照，形成适合课堂投影、远距离阅读和后续逐页生成的统一视觉主题。

        必须遵守：
        1. 只根据输入内容分析，不虚构具体页面缺陷或资源。
        2. 主题必须覆盖标题、摘要、3-8 个风格关键词、完整配色、四级字号、字体建议、安全区、3-8 条版式原则、四类页面建议、2-8 条内容呈现规则和生成提示摘要。
        3. 颜色使用 #RRGGBB 或 #AARRGGBB；字号单位与课件页面坐标一致；安全区也使用页面坐标单位。
        4. 一级标题字号不得小于二级标题，二级标题不得小于正文，正文不得小于辅助文字。
        5. 不访问文件系统，不调用任何未提供的工具，不输出隐藏推理。
        6. 在提交工具前，用简洁、面向用户的文本说明你正在形成的主题方向；不要输出 JSON、工具参数或隐藏推理。
        7. 最终必须调用 submit_courseware_theme 工具。普通聊天文本不能替代工具提交。
        """;

    private readonly ICopilotChatManagerFactory _chatManagerFactory;
    private readonly CoursewareThemeValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotCoursewareThemeAgent" /> class.
    /// </summary>
    /// <param name="chatManagerFactory">The independent chat manager factory.</param>
    /// <param name="validator">The deterministic theme validator.</param>
    public CopilotCoursewareThemeAgent(
        ICopilotChatManagerFactory chatManagerFactory,
        CoursewareThemeValidator validator)
    {
        ArgumentNullException.ThrowIfNull(chatManagerFactory);
        ArgumentNullException.ThrowIfNull(validator);

        _chatManagerFactory = chatManagerFactory;
        _validator = validator;
    }

    /// <inheritdoc />
    public async Task<CoursewareTheme> AnalyzeAsync(
        CoursewareAnalysisInput analysisInput,
        double slideWidth,
        double slideHeight,
        IProgress<CoursewareAnalysisEvent>? progress = null,
        IProgress<CopilotChatMessage>? messageProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysisInput);
        cancellationToken.ThrowIfCancellationRequested();

        var chatManager = await _chatManagerFactory.CreateAsync(
            AgentWorkload.ThemeAnalysis,
            cancellationToken).ConfigureAwait(true);
        var submissionTool = new CoursewareThemeSubmissionTool(_validator, slideWidth, slideHeight);
        var tool = submissionTool.CreateTool();

        for (var requestIndex = 0; requestIndex < MaximumRequestCount; requestIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isRepair = requestIndex > 0;
            progress?.Report(CreateEvent(
                isRepair ? CoursewareAnalysisStage.RepairingTheme : CoursewareAnalysisStage.DesigningTheme,
                isRepair ? "正在修正主题结构" : "正在形成主题方案",
                isRepair ? "模型正在根据本地校验结果修正主题。" : "模型正在归纳课件内容层级、配色、字号和版式。",
                CoursewareAnalysisEventState.Running));

            var prompt = isRepair
                ? BuildRepairPrompt(submissionTool.ValidationErrors)
                : analysisInput.Prompt;
            var request = new SendMessageRequest(new List<AIContent>(1) { new TextContent(prompt) })
            {
                WithHistory = false,
                CreateNewSession = true,
                AppendDefaultTools = false,
                Tools = [tool],
                SystemPrompt = SystemPrompt,
                CancellationToken = cancellationToken,
            };

            var result = chatManager.SendMessage(request);
            result.UserChatMessage.IsPresetInfo = true;
            result.AssistantChatMessage.IsPresetInfo = true;
            messageProgress?.Report(result.UserChatMessage);
            messageProgress?.Report(result.AssistantChatMessage);
            await result.RunTask.ConfigureAwait(true);
            progress?.Report(CreateEvent(
                isRepair ? CoursewareAnalysisStage.RepairingTheme : CoursewareAnalysisStage.DesigningTheme,
                isRepair ? "主题结构修正完成" : "主题方案形成完成",
                isRepair ? "模型已完成主题结构修正，正在重新校验。" : "模型已形成课件主题方案，正在校验结构化结果。",
                CoursewareAnalysisEventState.Completed));
            progress?.Report(CreateEvent(
                CoursewareAnalysisStage.ValidatingTheme,
                "主题结构校验",
                "正在校验配色、字号层级、安全区和页面类型建议。",
                CoursewareAnalysisEventState.Running));

            if (submissionTool.SubmittedTheme is not null)
            {
                progress?.Report(new CoursewareAnalysisEvent
                {
                    Stage = CoursewareAnalysisStage.ValidatingTheme,
                    Kind = CoursewareAnalysisEventKind.ToolSubmission,
                    Title = "主题结构校验",
                    Message = $"结构化主题已通过本地校验，共提交 {submissionTool.SubmissionCount} 次。",
                    State = CoursewareAnalysisEventState.Completed,
                });
                return submissionTool.SubmittedTheme;
            }

            progress?.Report(new CoursewareAnalysisEvent
            {
                Stage = CoursewareAnalysisStage.ValidatingTheme,
                Kind = CoursewareAnalysisEventKind.Warning,
                Title = "主题结构校验",
                Message = submissionTool.ValidationErrors.Count == 0
                    ? "模型尚未提交结构化主题。"
                    : string.Join("；", submissionTool.ValidationErrors),
                State = CoursewareAnalysisEventState.Warning,
            });
        }

        var details = submissionTool.ValidationErrors.Count == 0
            ? "模型未调用 submit_courseware_theme 工具。"
            : string.Join("；", submissionTool.ValidationErrors);
        throw new InvalidOperationException($"未能获得有效的课件全局主题。{details}");
    }

    private static string BuildRepairPrompt(IReadOnlyList<string> errors)
    {
        var detail = errors.Count == 0
            ? "上一轮没有调用 submit_courseware_theme。"
            : "上一轮存在以下校验错误：\n- " + string.Join("\n- ", errors);
        return $"{detail}\n请重新生成完整主题，并调用 submit_courseware_theme 提交。";
    }

    private static CoursewareAnalysisEvent CreateEvent(
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