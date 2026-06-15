using System.Threading;
using System.Threading.Tasks;

namespace PptxGenerator;

/// <summary>
/// 提示词评估器接口，对 SlideML 生成所用的系统提示词和用户提示词模板进行质量评估。
/// </summary>
public interface IPromptEvaluator
{
    /// <summary>
    /// 评估提示词质量并给出优化建议。
    /// </summary>
    /// <param name="systemPrompt">系统提示词文本。</param>
    /// <param name="userPromptTemplate">用户提示词模板文本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>评估结果。</returns>
    Task<PromptEvaluationResult> EvaluateAsync(
        string systemPrompt,
        string userPromptTemplate,
        CancellationToken cancellationToken = default);
}