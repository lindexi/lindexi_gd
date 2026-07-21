using AgentLib.Core.AgentApiManagers.Contexts;
using CoursewarePptxGenerator.Core.Analysis;
using Microsoft.Extensions.AI;

namespace CoursewarePptxGeneratorWpfDemo.Services;

internal static class CoursewareModelContextBudgetValidator
{
    private const double SafetyMarginRatio = 0.05;

    internal static CoursewareModelContextBudget? ValidateIfConfigured(
        ModelDefinition modelDefinition,
        string systemPrompt,
        string userPrompt,
        AIFunction tool,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modelDefinition);
        ArgumentNullException.ThrowIfNull(systemPrompt);
        ArgumentNullException.ThrowIfNull(userPrompt);
        ArgumentNullException.ThrowIfNull(tool);
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

        var systemPromptTokenCount = CoursewareTokenEstimator.Estimate(systemPrompt, cancellationToken);
        var userPromptTokenCount = CoursewareTokenEstimator.Estimate(userPrompt, cancellationToken);
        var toolSchemaTokenCount = EstimateToolSchema(tool, cancellationToken);
        var safetyMarginTokenCount = checked((int) Math.Ceiling(contextWindowSize * SafetyMarginRatio));
        var requiredTokenCount = (long) systemPromptTokenCount
            + userPromptTokenCount
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
                $"完整课件分析输入超出模型 {modelDefinition.ModelName} 的上下文预算。"
                + $"上下文窗口 {contextWindowSize} Token，System Prompt 约 {systemPromptTokenCount} Token，"
                + $"User Prompt 约 {userPromptTokenCount} Token，工具 Schema 约 {toolSchemaTokenCount} Token，"
                + $"输出预留 {outputTokenReserve} Token，安全余量 {safetyMarginTokenCount} Token；"
                + $"当前 User Prompt 最多可用约 {availableUserPromptTokenCount} Token。"
                + "系统不会静默截断或丢弃页面，请改用更大上下文模型或拆分课件后重试。");
        }

        return new CoursewareModelContextBudget
        {
            ModelName = modelDefinition.ModelName,
            ContextWindowSize = contextWindowSize,
            SystemPromptTokenCount = systemPromptTokenCount,
            UserPromptTokenCount = userPromptTokenCount,
            ToolSchemaTokenCount = toolSchemaTokenCount,
            OutputTokenReserve = outputTokenReserve,
            SafetyMarginTokenCount = safetyMarginTokenCount,
            RequiredTokenCount = requiredTokenCount,
        };
    }

    private static int EstimateToolSchema(AIFunction tool, CancellationToken cancellationToken)
    {
        var descriptor = $"{tool.Name}\n{tool.Description}\n{tool.JsonSchema.GetRawText()}";
        return CoursewareTokenEstimator.Estimate(descriptor, cancellationToken);
    }

}

internal sealed record CoursewareModelContextBudget
{
    internal required string ModelName { get; init; }

    internal required int ContextWindowSize { get; init; }

    internal required int SystemPromptTokenCount { get; init; }

    internal required int UserPromptTokenCount { get; init; }

    internal required int ToolSchemaTokenCount { get; init; }

    internal required int OutputTokenReserve { get; init; }

    internal required int SafetyMarginTokenCount { get; init; }

    internal required long RequiredTokenCount { get; init; }
}
