using PptxGenerator.Evaluation;

namespace PptxGenerator.Pipeline;

/// <summary>
/// 单轮迭代记录，包含该轮的提示词与评估结果。
/// </summary>
public sealed record IterationRound
{
    /// <summary>
    /// 轮次序号（从 1 开始）。
    /// </summary>
    public int Round { get; init; }

    /// <summary>
    /// 该轮使用的系统提示词。
    /// </summary>
    public string SystemPrompt { get; init; } = string.Empty;

    /// <summary>
    /// 该轮使用的用户提示词模板。
    /// </summary>
    public string UserPromptTemplate { get; init; } = string.Empty;

    /// <summary>
    /// SlideML 评估结果。
    /// </summary>
    public SlideEvaluationResult? SlideEvaluation { get; init; }

    /// <summary>
    /// 提示词优化结果（第一轮为空）。
    /// </summary>
    public PromptOptimizationResult? Optimization { get; init; }

    /// <summary>
    /// 记录时间戳。
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
}
