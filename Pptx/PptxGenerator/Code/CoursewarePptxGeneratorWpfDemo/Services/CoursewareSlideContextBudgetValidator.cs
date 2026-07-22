using AgentLib.Core.AgentApiManagers.Contexts;
using CoursewarePptxGenerator.Core.Analysis;
using Microsoft.Extensions.AI;
using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Validates that one page-generation request fits the configured model context without truncating source data.
/// </summary>
public static class CoursewareSlideContextBudgetValidator
{
    private const double SafetyMarginRatio = 0.05;

    /// <summary>
    /// Validates one page-generation request when the model publishes complete context limits.
    /// </summary>
    /// <param name="modelDefinition">The configured model definition.</param>
    /// <param name="promptProvider">The page SlideML prompt provider.</param>
    /// <param name="renderTool">The page render tool that supplies the streaming query tools.</param>
    /// <param name="pageNumber">The one-based page number used in diagnostics.</param>
    /// <param name="pagePrompt">The structured page prompt.</param>
    /// <param name="cancellationToken">The token used to cancel estimation.</param>
    /// <returns>The calculated budget, or <see langword="null" /> when the model omits context limits.</returns>
    public static CoursewareSlideContextBudget? ValidateIfConfigured(
        ModelDefinition modelDefinition,
        ISlideMlPromptProvider promptProvider,
        SlideMlRenderTool renderTool,
        int pageNumber,
        string pagePrompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modelDefinition);
        ArgumentNullException.ThrowIfNull(promptProvider);
        ArgumentNullException.ThrowIfNull(renderTool);
        ArgumentNullException.ThrowIfNull(pagePrompt);
        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }
        cancellationToken.ThrowIfCancellationRequested();

        if (modelDefinition.ContextWindowSize is not > 0
            || modelDefinition.MaxOutputTokens is not > 0)
        {
            return null;
        }

        var contextWindowSize = modelDefinition.ContextWindowSize.Value;
        var outputTokenReserve = modelDefinition.MaxOutputTokens.Value;
        if (outputTokenReserve >= contextWindowSize)
        {
            throw new InvalidOperationException(
                $"模型 {modelDefinition.ModelName} 的最大输出 Token 配置 {outputTokenReserve} 必须小于上下文窗口 {contextWindowSize}。");
        }

        var systemPromptTokenCount = CoursewareTokenEstimator.Estimate(
            promptProvider.BuildStreamingSystemPrompt(),
            cancellationToken);
        var wrappedUserPromptTokenCount = CoursewareTokenEstimator.Estimate(
            promptProvider.BuildStreamingUserPrompt(pagePrompt),
            cancellationToken);
        var toolSchemaTokenCount = EstimateToolSchema(
            [renderTool.CreateSlideStateTool(), renderTool.CreatePreviewTool()],
            cancellationToken);
        var safetyMarginTokenCount = checked((int) Math.Ceiling(contextWindowSize * SafetyMarginRatio));
        var requiredTokenCount = (long) systemPromptTokenCount
            + wrappedUserPromptTokenCount
            + toolSchemaTokenCount
            + outputTokenReserve
            + safetyMarginTokenCount;
        if (requiredTokenCount > contextWindowSize)
        {
            var availableUserPromptTokenCount = Math.Max(
                0L,
                (long) contextWindowSize
                - systemPromptTokenCount
                - toolSchemaTokenCount
                - outputTokenReserve
                - safetyMarginTokenCount);
            throw new InvalidOperationException(
                $"第 {pageNumber} 页页面生成输入超出模型 {modelDefinition.ModelName} 的上下文预算。"
                + $"上下文窗口 {contextWindowSize} Token，System Prompt 约 {systemPromptTokenCount} Token，"
                + $"User Prompt 约 {wrappedUserPromptTokenCount} Token，工具 Schema 约 {toolSchemaTokenCount} Token，"
                + $"输出预留 {outputTokenReserve} Token，"
                + $"安全余量 {safetyMarginTokenCount} Token；当前 User Prompt 最多可用约 {availableUserPromptTokenCount} Token。"
                + "系统不会静默截断当前页 Markdown 或主题，请缩短用户补充要求或改用更大上下文模型后重试。");
        }

        return new CoursewareSlideContextBudget
        {
            ModelName = modelDefinition.ModelName,
            ContextWindowSize = contextWindowSize,
            SystemPromptTokenCount = systemPromptTokenCount,
            UserPromptTokenCount = wrappedUserPromptTokenCount,
            ToolSchemaTokenCount = toolSchemaTokenCount,
            OutputTokenReserve = outputTokenReserve,
            SafetyMarginTokenCount = safetyMarginTokenCount,
            RequiredTokenCount = requiredTokenCount,
        };
    }

    private static int EstimateToolSchema(
        IReadOnlyList<AIFunction> tools,
        CancellationToken cancellationToken)
    {
        var estimatedTokenCount = 0L;
        foreach (var tool in tools)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var descriptor = $"{tool.Name}\n{tool.Description}\n{tool.JsonSchema.GetRawText()}";
            estimatedTokenCount += CoursewareTokenEstimator.Estimate(descriptor, cancellationToken);
        }

        return estimatedTokenCount >= int.MaxValue ? int.MaxValue : (int) estimatedTokenCount;
    }
}

/// <summary>
/// Represents the deterministic context-budget calculation for one page-generation request.
/// </summary>
public sealed record CoursewareSlideContextBudget
{
    /// <summary>
    /// Gets the configured model name.
    /// </summary>
    public required string ModelName { get; init; }

    /// <summary>
    /// Gets the configured context-window size.
    /// </summary>
    public required int ContextWindowSize { get; init; }

    /// <summary>
    /// Gets the estimated streaming system-prompt token count.
    /// </summary>
    public required int SystemPromptTokenCount { get; init; }

    /// <summary>
    /// Gets the estimated wrapped user-prompt token count.
    /// </summary>
    public required int UserPromptTokenCount { get; init; }

    /// <summary>
    /// Gets the estimated streaming tool-schema token count.
    /// </summary>
    public required int ToolSchemaTokenCount { get; init; }

    /// <summary>
    /// Gets the reserved output token count.
    /// </summary>
    public required int OutputTokenReserve { get; init; }

    /// <summary>
    /// Gets the safety-margin token count.
    /// </summary>
    public required int SafetyMarginTokenCount { get; init; }

    /// <summary>
    /// Gets the total required token count.
    /// </summary>
    public required long RequiredTokenCount { get; init; }
}
