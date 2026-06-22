namespace PptxGenerator.Evaluation;

/// <summary>
/// 提示词优化器接口，根据评估反馈生成改进后的提示词。
/// </summary>
public interface IPromptOptimizer
{
    /// <summary>
    /// 根据 SlideML 评估结果优化提示词。
    /// </summary>
    /// <param name="evaluation">SlideML 评估结果，包含各维度评分与改进建议。</param>
    /// <param name="systemPrompt">当前系统提示词文本。</param>
    /// <param name="userPromptTemplate">当前用户提示词模板文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>优化结果，包含改进后的提示词与变更说明。</returns>
    Task<PromptOptimizationResult> OptimizeAsync(
        SlideEvaluationResult evaluation,
        string systemPrompt,
        string userPromptTemplate,
        CancellationToken cancellationToken = default);
}
