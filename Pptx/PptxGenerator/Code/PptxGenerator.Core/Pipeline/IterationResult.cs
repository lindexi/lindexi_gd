namespace PptxGenerator.Pipeline;

/// <summary>
/// 提示词迭代优化的最终结果。
/// </summary>
public sealed record IterationResult
{
    /// <summary>
    /// 所有轮次的迭代记录。
    /// </summary>
    public IReadOnlyList<IterationRound> IterationHistory { get; init; } = Array.Empty<IterationRound>();

    /// <summary>
    /// 是否因评分达标或收敛而终止。
    /// </summary>
    public bool IsConverged { get; init; }

    /// <summary>
    /// 最后一轮的评估综合评分。
    /// </summary>
    public double FinalScore { get; init; }

    /// <summary>
    /// 实际执行的轮数。
    /// </summary>
    public int TotalRounds { get; init; }

    /// <summary>
    /// 终止原因描述。
    /// </summary>
    public string TerminateReason { get; init; } = string.Empty;
}
