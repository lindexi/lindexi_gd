using System.Text;

namespace PptxGenerator.Evaluation;

/// <summary>
/// 提示词质量的 AI 评估报告，包含多维度评分与优化建议。
/// </summary>
public sealed class PromptEvaluationResult
{
    /// <summary>
    /// 指令清晰度、无歧义（1-10）。
    /// </summary>
    public int Clarity { get; init; }

    /// <summary>
    /// 是否覆盖所有必要规则（1-10）。
    /// </summary>
    public int Completeness { get; init; }

    /// <summary>
    /// 约束是否有效、无矛盾（1-10）。
    /// </summary>
    public int ConstraintQuality { get; init; }

    /// <summary>
    /// 是否可被模型准确执行（1-10）。
    /// </summary>
    public int Actionability { get; init; }

    /// <summary>
    /// 是否存在冗余/重复内容，分数越高表示冗余越少（1-10）。
    /// </summary>
    public int Redundancy { get; init; }

    /// <summary>
    /// 综合评分（各维度平均值）。
    /// </summary>
    public double OverallScore => new[] { Clarity, Completeness, ConstraintQuality, Actionability, Redundancy }.Average();

    /// <summary>
    /// 自然语言优化建议列表。
    /// </summary>
    public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 评估完成时间。
    /// </summary>
    public DateTimeOffset EvaluatedAt { get; init; } = DateTimeOffset.Now;

    /// <summary>
    /// 被评估的 SystemPrompt 文本。
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// 被评估的 UserPrompt 模板文本。
    /// </summary>
    public string? UserPromptTemplate { get; init; }

    /// <summary>
    /// 评估是否成功完成。为 <see langword="false"/> 时评分可能无效。
    /// </summary>
    public bool IsSuccess { get; init; } = true;

    /// <summary>
    /// 评估失败时的错误信息。
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 创建一个表示评估失败的实例。
    /// </summary>
    public static PromptEvaluationResult Failed(string errorMessage)
    {
        return new PromptEvaluationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            EvaluatedAt = DateTimeOffset.Now,
        };
    }

    /// <summary>
    /// 将评估结果格式化为可读的显示文本，用于聊天列表展示。
    /// </summary>
    public string ToDisplayText()
    {
        if (!IsSuccess)
        {
            return $"📝 提示词评估失败：{ErrorMessage}";
        }

        var builder = new StringBuilder(256);
        builder.AppendLine($"📝 提示词评估报告 | 综合评分: {OverallScore:F1}/10");
        builder.AppendLine($"  清晰度:   {Clarity}/10");
        builder.AppendLine($"  完整性:   {Completeness}/10");
        builder.AppendLine($"  约束质量: {ConstraintQuality}/10");
        builder.AppendLine($"  可执行性: {Actionability}/10");
        builder.AppendLine($"  冗余度:   {Redundancy}/10");

        if (Suggestions.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("优化建议:");
            foreach (var suggestion in Suggestions)
            {
                builder.AppendLine($"  - {suggestion}");
            }
        }

        return builder.ToString().TrimEnd();
    }
}
