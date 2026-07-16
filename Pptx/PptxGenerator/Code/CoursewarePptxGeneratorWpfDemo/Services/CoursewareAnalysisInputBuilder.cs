using System.Text;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Builds a deterministic, bounded text snapshot for whole-courseware theme analysis.
/// </summary>
public sealed class CoursewareAnalysisInputBuilder : ICoursewareAnalysisInputBuilder
{
    private const int DefaultMaximumCharacterCount = 60_000;
    private const int RegularSlideContentLimit = 500;
    private const int KeySlideContentLimit = 1_500;
    private readonly CoursewareSlideSummaryService _summaryService;
    private readonly int _maximumCharacterCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareAnalysisInputBuilder" /> class.
    /// </summary>
    /// <param name="maximumCharacterCount">The maximum approximate prompt length.</param>
    public CoursewareAnalysisInputBuilder(int maximumCharacterCount = DefaultMaximumCharacterCount)
    {
        if (maximumCharacterCount < 4_000)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCharacterCount), "分析输入字符预算不能小于 4000。");
        }

        _maximumCharacterCount = maximumCharacterCount;
        _summaryService = new CoursewareSlideSummaryService();
    }

    /// <inheritdoc />
    public CoursewareAnalysisInput Build(CoursewareInputPackage inputPackage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);
        cancellationToken.ThrowIfCancellationRequested();

        var builder = new StringBuilder(Math.Min(_maximumCharacterCount, 16_384));
        AppendHeader(builder, inputPackage);
        var warnings = new List<string>();
        var analyzedSlideCount = 0;

        foreach (var slide in inputPackage.Slides.OrderBy(slide => slide.SlideIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var block = CreateSlideBlock(slide, IsKeySlide(slide, inputPackage.SlideCount));
            if (builder.Length + block.Length > _maximumCharacterCount)
            {
                var compactBlock = CreateCompactSlideBlock(slide);
                if (builder.Length + compactBlock.Length > _maximumCharacterCount)
                {
                    warnings.Add($"分析输入达到字符预算，第 {slide.PageNumber} 页及后续页面仅由全局页数统计表示。");
                    break;
                }

                block = compactBlock;
            }

            builder.Append(block);
            analyzedSlideCount++;
        }

        if (analyzedSlideCount == 0)
        {
            throw new InvalidOperationException("无法在当前字符预算内构建任何页面的分析输入。");
        }

        return new CoursewareAnalysisInput
        {
            Prompt = builder.ToString(),
            AnalyzedSlideCount = analyzedSlideCount,
            CharacterCount = builder.Length,
            Warnings = warnings,
        };
    }

    private static void AppendHeader(StringBuilder builder, CoursewareInputPackage inputPackage)
    {
        var dimensions = inputPackage.Slides
            .GroupBy(slide => $"{slide.Width:0.##}×{slide.Height:0.##}")
            .Select(group => $"{group.Key}: {group.Count()} 页");
        var resources = inputPackage.Resources
            .GroupBy(resource => resource.ResourceType ?? "unknown", StringComparer.OrdinalIgnoreCase)
            .Select(group => $"{group.Key}: {group.Count()} 个");

        builder.AppendLine("# 课件全局主题分析输入")
            .Append("课件名称：").AppendLine(inputPackage.CoursewareName)
            .Append("总页数：").AppendLine(inputPackage.SlideCount.ToString())
            .Append("包含截图页数：").AppendLine(inputPackage.Slides.Count(slide => slide.ScreenshotFile is not null).ToString())
            .Append("页面尺寸分布：").AppendLine(string.Join("；", dimensions))
            .Append("资源统计：").AppendLine(inputPackage.Resources.Count == 0 ? "无" : string.Join("；", resources))
            .Append("输入警告数：").AppendLine(inputPackage.Warnings.Count.ToString())
            .AppendLine()
            .AppendLine("# 页面快照");
    }

    private string CreateSlideBlock(CoursewareSlideInput slide, bool isKeySlide)
    {
        var title = _summaryService.CreateTitle(slide.MarkdownText, slide.PageNumber);
        var readableText = ExtractReadableText(slide.MarkdownText);
        var contentLimit = isKeySlide ? KeySlideContentLimit : RegularSlideContentLimit;
        var content = Truncate(readableText, contentLimit);
        var warningText = slide.Warnings.Count == 0
            ? "无"
            : string.Join("；", slide.Warnings.Select(warning => warning.Message));

        return $"""

            ## 第 {slide.PageNumber} 页
            - SlideId: {slide.SlideId}
            - 尺寸: {slide.Width:0.##}×{slide.Height:0.##}
            - 标题: {title}
            - 页面警告: {warningText}
            - 内容摘要: {content}
            """;
    }

    private string CreateCompactSlideBlock(CoursewareSlideInput slide)
    {
        var title = _summaryService.CreateTitle(slide.MarkdownText, slide.PageNumber);
        return $"\n## 第 {slide.PageNumber} 页\n- SlideId: {slide.SlideId}\n- 标题: {title}\n";
    }

    private static bool IsKeySlide(CoursewareSlideInput slide, int slideCount)
    {
        if (slide.PageNumber <= 2 || slide.PageNumber == slideCount)
        {
            return true;
        }

        var text = slide.MarkdownText;
        return text.Contains("目录", StringComparison.OrdinalIgnoreCase)
            || text.Contains("章节", StringComparison.OrdinalIgnoreCase)
            || text.Contains("小结", StringComparison.OrdinalIgnoreCase)
            || text.Contains("总结", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractReadableText(string markdownText)
    {
        var lines = markdownText.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var builder = new StringBuilder();
        var inContentBlock = false;
        var expectingContentBlock = false;
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line == "#### 内容")
            {
                expectingContentBlock = true;
                continue;
            }

            if (line.Length >= 3 && line.All(character => character == '`'))
            {
                if (expectingContentBlock)
                {
                    inContentBlock = true;
                    expectingContentBlock = false;
                }
                else if (inContentBlock)
                {
                    inContentBlock = false;
                }

                continue;
            }

            if (inContentBlock && !string.IsNullOrWhiteSpace(line))
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(line);
            }
        }

        return builder.Length == 0 ? "未提取到可读文本。" : builder.ToString();
    }

    private static string Truncate(string text, int maximumLength)
    {
        return text.Length <= maximumLength ? text : $"{text[..maximumLength]}...";
    }
}