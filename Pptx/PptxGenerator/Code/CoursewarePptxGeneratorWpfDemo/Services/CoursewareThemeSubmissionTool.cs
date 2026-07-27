using System.ComponentModel;
using CoursewarePptxGeneratorWpfDemo.Models;
using Microsoft.Extensions.AI;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Receives and validates a structured courseware theme submitted by a language model.
/// </summary>
public sealed class CoursewareThemeSubmissionTool
{
    private readonly CoursewareThemeValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareThemeSubmissionTool" /> class.
    /// </summary>
    /// <param name="validator">The deterministic theme validator.</param>
    public CoursewareThemeSubmissionTool(CoursewareThemeValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        _validator = validator;
    }

    /// <summary>Gets the latest valid submitted theme.</summary>
    public CoursewareTheme? SubmittedTheme { get; private set; }
    /// <summary>Gets the latest validation errors.</summary>
    public IReadOnlyList<string> ValidationErrors { get; private set; } = [];
    /// <summary>Gets the number of submission attempts.</summary>
    public int SubmissionCount { get; private set; }

    /// <summary>
    /// Creates the AI function exposed to the model.
    /// </summary>
    /// <returns>The structured theme submission function.</returns>
    public AIFunction CreateTool()
    {
        return AIFunctionFactory.Create(
            SubmitTheme,
            name: "submit_courseware_theme_analysis",
            description: "提交完整的课件全局主题分析结果。系统当前执行字段级校验；失败时请修正并重新提交。");
    }

    [Description("提交完整且可用于后续页面生成的课件全局主题。")]
    public string SubmitTheme([Description("完整的结构化课件主题。")] CoursewareTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        SubmissionCount++;
        var validationResult = _validator.Validate(theme);
        ValidationErrors = validationResult.Errors;
        if (!validationResult.IsValid)
        {
            return "验证失败：\n- " + string.Join("\n- ", validationResult.Errors)
                + "\n请修正后重新提交。";
        }

        SubmittedTheme = theme;
        return "主题字段校验通过，已记录。";
    }
}