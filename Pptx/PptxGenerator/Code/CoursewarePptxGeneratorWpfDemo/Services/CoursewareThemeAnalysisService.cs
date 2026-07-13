using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Produces a deterministic baseline theme from a loaded courseware package.
/// </summary>
/// <remarks>
/// This service is intentionally independent from slide rendering and page chat sessions so that a model-backed
/// analyzer can replace it without changing the workspace workflow.
/// </remarks>
public sealed class CoursewareThemeAnalysisService : ICoursewareThemeAnalysisService
{
    /// <inheritdoc />
    public async Task<CoursewareThemeAnalysisResult> AnalyzeAsync(
        CoursewareInputPackage package,
        IProgress<CoursewareAnalysisMessage>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(package);
        if (package.Slides.Count == 0)
        {
            throw new InvalidOperationException("课件至少需要包含一页有效页面。 ");
        }

        await ReportAsync(
            progress,
            "课件读取完成",
            $"已读取《{package.CoursewareName}》的 {package.SlideCount} 页内容与资源清单。",
            CoursewareAnalysisMessageKind.Completed,
            cancellationToken);
        await ReportAsync(
            progress,
            "识别内容结构",
            "正在归纳封面、章节与普通内容页的内容层级。",
            CoursewareAnalysisMessageKind.Progress,
            cancellationToken);
        await ReportAsync(
            progress,
            "分析视觉方向",
            "正在结合页面尺寸、文本密度与现有截图形成统一视觉方向。",
            CoursewareAnalysisMessageKind.Progress,
            cancellationToken);
        await ReportAsync(
            progress,
            "生成主题规范",
            "正在整理配色、字体层级、版式原则与安全区。",
            CoursewareAnalysisMessageKind.Progress,
            cancellationToken);

        var result = CreateTheme(package);
        Validate(result);

        await ReportAsync(
            progress,
            "主题分析完成",
            $"已完整分析 {package.SlideCount} 页课件，主题结构校验通过。",
            CoursewareAnalysisMessageKind.Completed,
            cancellationToken);
        return result;
    }

    private static CoursewareThemeAnalysisResult CreateTheme(CoursewareInputPackage package)
    {
        var combinedMarkdown = string.Join('\n', package.Slides.Select(slide => slide.MarkdownText));
        var isMathematics = ContainsAny(combinedMarkdown, "数学", "函数", "几何", "方程", "定理", "例题", "证明");
        var subjectDescription = isMathematics ? "数学推导与理性空间" : "清晰教学与现代课堂";
        var summary = isMathematics
            ? "以深蓝建立理性秩序，以青色强调关键推导，配合暖橙提示例题与重点；版式保持清晰对齐和充足留白。"
            : "以稳定的蓝青色建立清晰教学秩序，通过统一层级、对齐线和留白强化内容重点与课堂阅读节奏。";
        var missingScreenshotCount = package.Slides.Count(slide => slide.ScreenshotFile is null);
        var missingResourceCount = package.Resources.Count(resource => !resource.Exists);

        return new CoursewareThemeAnalysisResult
        {
            Title = $"主题方案：{subjectDescription}",
            Summary = summary,
            StyleKeywords = isMathematics
                ? ["清晰几何", "现代简约", "严谨理性", "互动讲解"]
                : ["现代清晰", "层级分明", "教学友好", "轻量专业"],
            Colors =
            [
                new CoursewareThemeColor("主色", "#173F5F", "标题、章节与主视觉"),
                new CoursewareThemeColor("辅助色", "#2496A6", "图表、推导与信息强调"),
                new CoursewareThemeColor("强调色", "#F59E42", "例题、重点与操作提示"),
                new CoursewareThemeColor("背景色", "#F5F8FA", "页面背景与内容分区"),
                new CoursewareThemeColor("正文色", "#263238", "正文与关键说明"),
            ],
            Typography =
            [
                new CoursewareTypographyLevel("一级标题", "Microsoft YaHei UI", 32, "Bold"),
                new CoursewareTypographyLevel("二级标题", "Microsoft YaHei UI", 24, "SemiBold"),
                new CoursewareTypographyLevel("正文", "Microsoft YaHei UI", 18, "Regular"),
                new CoursewareTypographyLevel("辅助文字", "Microsoft YaHei UI", 14, "Regular"),
            ],
            LayoutPrinciples =
            [
                "标题区保持稳定高度与统一左对齐线",
                "每页只保留一个主视觉焦点",
                "图文内容遵守三分法与统一留白",
                "章节页与内容页形成明确节奏变化",
            ],
            SafeArea = new CoursewareSafeArea(5, 5, 5, 5),
            PageTypeRecommendations =
            [
                "封面页：突出课程主题，减少次要信息",
                "章节页：使用大标题和单一视觉符号建立节奏",
                "内容页：优先采用左文右图或三分栏结构",
                "结尾页：回收核心结论并保留课堂互动入口",
            ],
            Coverage = new CoursewareAnalysisCoverage(
                package.SlideCount,
                missingScreenshotCount,
                missingResourceCount,
                package.Warnings.Count),
        };
    }

    private static async Task ReportAsync(
        IProgress<CoursewareAnalysisMessage>? progress,
        string title,
        string description,
        CoursewareAnalysisMessageKind kind,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new CoursewareAnalysisMessage(title, description, kind, DateTimeOffset.Now));
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static void Validate(CoursewareThemeAnalysisResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Title)
            || string.IsNullOrWhiteSpace(result.Summary)
            || result.Colors.Count < 4
            || result.Typography.Count < 3
            || result.LayoutPrinciples.Count == 0)
        {
            throw new InvalidOperationException("主题分析结果缺少必要的结构化内容。 ");
        }
    }
}
