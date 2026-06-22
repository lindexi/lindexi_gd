namespace PptxGenerator.Evaluation;

/// <summary>
/// 提示词优化结果，包含 AI 优化后的提示词与变更说明。
/// </summary>
public sealed class PromptOptimizationResult
{
    /// <summary>
    /// 优化后的系统提示词。
    /// </summary>
    public string? OptimizedSystemPrompt { get; init; }

    /// <summary>
    /// 优化后的用户提示词模板。模板中使用 <c>{USER_INPUT}</c> 作为用户输入占位符。
    /// </summary>
    public string? OptimizedUserPromptTemplate { get; init; }

    /// <summary>
    /// 变更说明，描述本次优化的主要改动。
    /// </summary>
    public string? ChangeDescription { get; init; }

    /// <summary>
    /// 优化是否成功完成。
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// 优化失败时的错误信息。
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 创建一个表示优化失败的实例。
    /// </summary>
    public static PromptOptimizationResult Failed(string errorMessage)
    {
        return new PromptOptimizationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
        };
    }
}
