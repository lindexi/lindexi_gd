using System.Text;

namespace PptxGenerator.Evaluation;

/// <summary>
/// SlideML 生成结果的 AI 评估报告，包含多维度评分与改进建议。
/// </summary>
public sealed class SlideEvaluationResult
{
    /// <summary>
    /// XML 语法正确性、标签合规性、属性完整性（1-10）。
    /// </summary>
    public int XmlWellFormedness { get; init; }

    /// <summary>
    /// 层级结构清晰度、Panel 嵌套合理性（1-10）。
    /// </summary>
    public int LayoutStructure { get; init; }

    /// <summary>
    /// 元素分布均衡性、留白是否充足（1-10）。
    /// </summary>
    public int VisualBalance { get; init; }

    /// <summary>
    /// 是否遵守 1280×720 画布、颜色格式等约束（1-10）。
    /// </summary>
    public int ConstraintAdherence { get; init; }

    /// <summary>
    /// 内容是否与用户需求对齐（1-10）。
    /// </summary>
    public int SemanticAlignment { get; init; }

    /// <summary>
    /// 配色、字体大小层级、整体美观度（1-10）。
    /// </summary>
    public int AestheticQuality { get; init; }

    /// <summary>
    /// 截图还原度：生成的 SlideML 渲染截图与原始 PPT 截图的匹配程度（1-10）。
    /// 仅在评估时提供了原始截图时有效。未提供截图时默认 5 分中性值。
    /// </summary>
    public int ScreenshotFidelity { get; init; } = 5;

    /// <summary>
    /// 是否在评估时提供了原始截图。为 <see langword="false"/> 时 <see cref="ScreenshotFidelity"/> 为中性值，不参与综合评分计算。
    /// </summary>
    public bool HasOriginalScreenshot { get; init; }

    /// <summary>
    /// 综合评分（各维度平均值）。未提供原始截图时排除 <see cref="ScreenshotFidelity"/> 维度。
    /// </summary>
    public double OverallScore => HasOriginalScreenshot
        ? new[] { XmlWellFormedness, LayoutStructure, VisualBalance, ConstraintAdherence, SemanticAlignment, AestheticQuality, ScreenshotFidelity }.Average()
        : new[] { XmlWellFormedness, LayoutStructure, VisualBalance, ConstraintAdherence, SemanticAlignment, AestheticQuality }.Average();

    /// <summary>
    /// 自然语言改进建议列表。
    /// </summary>
    public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 评估完成时间。
    /// </summary>
    public DateTimeOffset EvaluatedAt { get; init; } = DateTimeOffset.Now;

    /// <summary>
    /// 评估时使用的原始用户需求文本。
    /// </summary>
    public string? UserPrompt { get; init; }

    /// <summary>
    /// 评估时使用的 SlideML XML 文本。
    /// </summary>
    public string? SlideXml { get; init; }

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
    public static SlideEvaluationResult Failed(string errorMessage)
    {
        return new SlideEvaluationResult
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
            return $"📊 SlideML 评估失败：{ErrorMessage}";
        }

        var builder = new StringBuilder(256);
        builder.AppendLine($"📊 SlideML 评估报告 | 综合评分: {OverallScore:F1}/10");
        builder.AppendLine($"  XML 规范: {XmlWellFormedness}/10");
        builder.AppendLine($"  布局结构: {LayoutStructure}/10");
        builder.AppendLine($"  视觉平衡: {VisualBalance}/10");
        builder.AppendLine($"  约束遵守: {ConstraintAdherence}/10");
        builder.AppendLine($"  语义对齐: {SemanticAlignment}/10");
        builder.AppendLine($"  美观度:   {AestheticQuality}/10");
        builder.AppendLine($"  截图还原: {ScreenshotFidelity}/10");

        if (Suggestions.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("改进建议:");
            foreach (var suggestion in Suggestions)
            {
                builder.AppendLine($"  - {suggestion}");
            }
        }

        return builder.ToString().TrimEnd();
    }
}
