using System.Text;
using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Builds the natural-language input for whole-courseware theme analysis.
/// </summary>
public sealed class CoursewareThemeAnalysisPromptBuilder : ICoursewareThemeAnalysisPromptBuilder
{
    private const string SlideSeparator = "\n\n---\n\n";

    /// <inheritdoc />
    public string Build(CoursewareInputPackage inputPackage, CoursewareStyleUsageSummary styleUsageSummary)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);
        ArgumentNullException.ThrowIfNull(styleUsageSummary);

        var builder = new StringBuilder();
        builder.AppendLine("请分析以下课件内容，并形成统一、可执行的课件视觉主题。");
        builder.AppendLine("下面的字体、字号和颜色分布仅供参考，不要机械继承；应结合全部页面内容重新判断主题方案。");
        AppendDistribution(builder, "字体分布", styleUsageSummary.Fonts);
        AppendDistribution(builder, "字号分布", styleUsageSummary.FontSizes);
        AppendDistribution(builder, "颜色分布", styleUsageSummary.Colors);
        builder.AppendLine();
        builder.AppendLine("以下是按输入顺序提供的每页原始 Markdown：");

        for (var index = 0; index < inputPackage.Slides.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(SlideSeparator);
            }

            builder.Append(inputPackage.Slides[index].MarkdownText);
        }

        return builder.ToString();
    }

    private static void AppendDistribution(
        StringBuilder builder,
        string title,
        IReadOnlyList<CoursewareStyleUsageItem> items)
    {
        builder.Append(title);
        builder.Append('：');
        if (items.Count == 0)
        {
            builder.AppendLine("未发现。");
            return;
        }

        for (var index = 0; index < items.Count; index++)
        {
            if (index > 0)
            {
                builder.Append("，");
            }

            builder.Append(items[index].Value);
            builder.Append("（");
            builder.Append(items[index].Count);
            builder.Append(" 次）");
        }

        builder.AppendLine("。");
    }
}
