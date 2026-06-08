using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PptxGenerator;

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
    /// 综合评分（各维度平均值）。
    /// </summary>
    public double OverallScore => new[] { XmlWellFormedness, LayoutStructure, VisualBalance, ConstraintAdherence, SemanticAlignment, AestheticQuality }.Average();

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
}
