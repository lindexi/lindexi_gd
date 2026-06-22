namespace PptxGenerator.Pipeline;

/// <summary>
/// 提示词迭代优化选项。
/// </summary>
public sealed record IterationOptions
{
    /// <summary>
    /// 最大迭代轮数。
    /// </summary>
    public int MaxRounds { get; init; } = 5;

    /// <summary>
    /// 综合评分阈值，达到此分数时提前终止迭代。
    /// </summary>
    public double ScoreThreshold { get; init; } = 8.0;

    /// <summary>
    /// 收敛判定轮数：连续此轮数评分不提升则终止。
    /// </summary>
    public int ConvergenceRounds { get; init; } = 2;
}
