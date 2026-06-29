using PptxGenerator.Evaluation;

namespace PptxGenerator.Pipeline;

/// <summary>
/// 管道配置对象，用于减少 <see cref="SlideGenerationPipeline"/> 和 <see cref="SlideChatManager"/> 的构造函数参数。
/// </summary>
public sealed class PipelineConfiguration
{
    /// <summary>
    /// SlideML 评估器。为 <see langword="null"/> 时禁用 SlideML 评估。
    /// </summary>
    public ISlideEvaluator? SlideEvaluator { get; init; }

    /// <summary>
    /// 提示词评估器。为 <see langword="null"/> 时禁用提示词评估。
    /// </summary>
    public IPromptEvaluator? PromptEvaluator { get; init; }

    /// <summary>
    /// 提示词优化器。为 <see langword="null"/> 时禁用提示词迭代优化。
    /// </summary>
    public IPromptOptimizer? PromptOptimizer { get; init; }
}
