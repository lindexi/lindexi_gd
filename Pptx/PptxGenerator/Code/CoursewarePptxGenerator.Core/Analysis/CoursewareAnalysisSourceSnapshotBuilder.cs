using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Creates a path-free, immutable analysis snapshot and verifies stable slide identity.
/// </summary>
public sealed class CoursewareAnalysisSourceSnapshotBuilder
{
    private const string SourceFingerprintSchemaVersion = "courseware-analysis-source/v2";
    private static readonly Regex PageInformationHeadingPattern = new(
        @"\A(?:\uFEFF)?(?:[ \t]*(?:\r?\n|\r))*[ \t]*##[ \t]+页面信息[ \t]*(?:\r?\n|\r|$)",
        RegexOptions.CultureInvariant);
    private static readonly Regex PageInformationEndPattern = new(
        @"(?m)^[ \t]*(?:---[ \t]*|##[ \t]+[^\r\n]+)[ \t]*\r?$",
        RegexOptions.CultureInvariant);
    private static readonly Regex MarkdownSlideIdPattern = new(
        @"(?im)^[ \t]*-[ \t]*Id[ \t]*:[ \t]*(?<id>[^\r\n]+?)[ \t]*\r?$",
        RegexOptions.CultureInvariant);
    private static readonly Regex MarkdownPageNumberPattern = new(
        @"(?im)^[ \t]*-[ \t]*序号[ \t]*\([ \t]*1-base[ \t]*\)[ \t]*:[ \t]*(?<number>\d+)[ \t]*\r?$",
        RegexOptions.CultureInvariant);
    private static readonly Regex MarkdownDimensionsPattern = new(
        @"(?im)^[ \t]*-[ \t]*尺寸[ \t]*:[ \t]*(?<width>\d+(?:\.\d+)?)[ \t]*[×xX][ \t]*(?<height>\d+(?:\.\d+)?)[ \t]*\r?$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Builds the immutable logical source snapshot.
    /// </summary>
    public CoursewareAnalysisSourceSnapshot Build(
        CoursewareInputPackage inputPackage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(inputPackage.CoursewareName))
        {
            throw new InvalidOperationException("课件名称不能为空。");
        }
        ValidateSingleLineValue(inputPackage.CoursewareName, "CoursewareName");

        var slides = inputPackage.Slides.OrderBy(slide => slide.SlideIndex).ToArray();
        if (slides.Length == 0)
        {
            throw new InvalidOperationException("课件中没有可用于分析的页面。");
        }

        if (slides.Select(slide => slide.SlideId).Distinct(StringComparer.Ordinal).Count() != slides.Length)
        {
            throw new InvalidOperationException("课件页面包含重复的 SlideId。");
        }

        var slideFacts = new CoursewareAnalysisSlideFact[slides.Length];
        for (var position = 0; position < slides.Length; position++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var slide = slides[position];
            ValidateSlide(slide, position);
            slideFacts[position] = new CoursewareAnalysisSlideFact(
                slide.SlideIndex,
                slide.PageNumber,
                slide.SlideId,
                slide.Width,
                slide.Height,
                slide.ScreenshotFile is not null,
                slide.MarkdownText,
                ComputeFingerprint(slide.MarkdownText));
        }

        var resourceFacts = inputPackage.Resources
            .OrderBy(resource => resource.ResourceId, StringComparer.Ordinal)
            .Select(resource =>
            {
                ValidateSingleLineValue(resource.ResourceId, "ResourceId");
                ValidateSingleLineValue(resource.ResourceType, "ResourceType");
                return new CoursewareAnalysisResourceFact(resource.ResourceId!, resource.ResourceType!, resource.Exists);
            })
            .ToArray();
        if (resourceFacts.Select(resource => resource.ResourceId).Distinct(StringComparer.Ordinal).Count() != resourceFacts.Length)
        {
            throw new InvalidOperationException("课件资源目录包含重复的 ResourceId。");
        }

        var warnings = inputPackage.Warnings
            .Where(warning => warning.SlideIndex is null)
            .Select(warning =>
            {
                ValidateSingleLineValue(warning.Code, "Warning.Code");
                ArgumentNullException.ThrowIfNull(warning.Message);
                return warning;
            })
            .OrderBy(warning => warning.Code, StringComparer.Ordinal)
            .ThenBy(warning => warning.Message, StringComparer.Ordinal)
            .ThenBy(warning => warning.RelativePath, StringComparer.Ordinal)
            .ThenBy(warning => warning.SlideIndex)
            .ToArray();
        var sourceFingerprint = ComputeSnapshotFingerprint(
            inputPackage.CoursewareName,
            slideFacts,
            resourceFacts,
            warnings);

        return new CoursewareAnalysisSourceSnapshot(
            inputPackage.CoursewareName,
            slideFacts,
            resourceFacts,
            warnings,
            sourceFingerprint);
    }

    private static void ValidateSlide(CoursewareSlideInput slide, int expectedPosition)
    {
        ArgumentNullException.ThrowIfNull(slide.MarkdownText);
        ValidateSingleLineValue(slide.SlideId, "SlideId");
        if (slide.SlideIndex != expectedPosition || slide.PageNumber != expectedPosition + 1)
        {
            throw new InvalidOperationException("课件页面的 SlideIndex 和页码必须连续且顺序一致。");
        }

        if (!double.IsFinite(slide.Width)
            || !double.IsFinite(slide.Height)
            || slide.Width <= 0
            || slide.Height <= 0)
        {
            throw new InvalidOperationException($"第 {slide.PageNumber} 页的页面尺寸必须大于 0。");
        }

        var pageInformation = ExtractPageInformation(slide.MarkdownText, slide.PageNumber);
        var markdownSlideId = ReadSingleMarkdownValue(MarkdownSlideIdPattern, pageInformation, "Id", slide.PageNumber);
        if (!string.Equals(markdownSlideId, slide.SlideId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"第 {slide.PageNumber} 页 Markdown Id 与清单 SlideId 不一致。");
        }

        var markdownPageNumber = ReadSingleMarkdownValue(
            MarkdownPageNumberPattern,
            pageInformation,
            "序号(1-base)",
            slide.PageNumber);
        if (!int.TryParse(markdownPageNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPageNumber)
            || parsedPageNumber != slide.PageNumber)
        {
            throw new InvalidOperationException(
                $"第 {slide.PageNumber} 页 Markdown 序号与清单页码不一致。");
        }

        var dimensionMatches = MarkdownDimensionsPattern.Matches(pageInformation);
        if (dimensionMatches.Count > 1)
        {
            throw new InvalidOperationException($"第 {slide.PageNumber} 页 Markdown 包含多个尺寸字段。");
        }

        if (dimensionMatches.Count == 0
            || !double.TryParse(dimensionMatches[0].Groups["width"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var markdownWidth)
            || !double.TryParse(dimensionMatches[0].Groups["height"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var markdownHeight))
        {
            throw new InvalidOperationException($"第 {slide.PageNumber} 页 Markdown 缺少合法的尺寸字段。");
        }

        if (Math.Abs(markdownWidth - slide.Width) > 0.01 || Math.Abs(markdownHeight - slide.Height) > 0.01)
        {
            throw new InvalidOperationException(
                $"第 {slide.PageNumber} 页 Markdown 尺寸与清单尺寸不一致。");
        }
    }

    private static string ExtractPageInformation(string markdown, int pageNumber)
    {
        var headingMatch = PageInformationHeadingPattern.Match(markdown);
        if (!headingMatch.Success)
        {
            throw new InvalidOperationException($"第 {pageNumber} 页 Markdown 必须以“## 页面信息”章节开始。");
        }

        var sectionStart = headingMatch.Index + headingMatch.Length;
        var sectionEndMatch = PageInformationEndPattern.Match(markdown, sectionStart);
        var sectionEnd = sectionEndMatch.Success ? sectionEndMatch.Index : markdown.Length;
        return markdown[sectionStart..sectionEnd];
    }

    private static string ReadSingleMarkdownValue(
        Regex pattern,
        string markdown,
        string fieldName,
        int pageNumber)
    {
        var matches = pattern.Matches(markdown);
        if (matches.Count > 1)
        {
            throw new InvalidOperationException($"第 {pageNumber} 页 Markdown 包含多个 {fieldName} 字段。");
        }

        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"第 {pageNumber} 页 Markdown 缺少必需的 {fieldName} 字段。");
        }

        return matches[0].Groups[1].Value.Trim();
    }

    private static string ComputeSnapshotFingerprint(
        string coursewareName,
        IReadOnlyList<CoursewareAnalysisSlideFact> slides,
        IReadOnlyList<CoursewareAnalysisResourceFact> resources,
        IReadOnlyList<CoursewareLoadWarning> warnings)
    {
        var builder = new StringBuilder();
        AppendLengthPrefixed(builder, SourceFingerprintSchemaVersion);
        builder.Append("courseware|");
        AppendLengthPrefixed(builder, coursewareName);
        builder.Append("slides|").Append(slides.Count).Append('|');
        foreach (var slide in slides)
        {
            builder.Append(slide.SlideIndex).Append('|')
                .Append(slide.PageNumber).Append('|')
                .Append(slide.Width.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                .Append(slide.Height.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                .Append(slide.HasScreenshot ? '1' : '0').Append('|');
            AppendLengthPrefixed(builder, slide.SlideId);
            AppendLengthPrefixed(builder, slide.MarkdownText);
        }

        builder.Append("resources|").Append(resources.Count).Append('|');
        foreach (var resource in resources)
        {
            AppendLengthPrefixed(builder, resource.ResourceId);
            AppendLengthPrefixed(builder, resource.ResourceType);
            builder.Append(resource.Exists ? '1' : '0').Append('|');
        }

        builder.Append("warnings|").Append(warnings.Count).Append('|');
        foreach (var warning in warnings)
        {
            AppendLengthPrefixed(builder, warning.Code);
            AppendLengthPrefixed(builder, warning.Message);
            AppendLengthPrefixed(builder, warning.RelativePath ?? string.Empty);
            builder.Append(warning.SlideIndex?.ToString(CultureInfo.InvariantCulture) ?? "null").Append('|');
        }

        return ComputeFingerprint(builder.ToString());
    }

    private static void AppendLengthPrefixed(StringBuilder builder, string value)
    {
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('|');
    }

    private static string ComputeFingerprint(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static void ValidateSingleLineValue(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{parameterName} 不能为空。");
        }

        if (value.Contains('\r') || value.Contains('\n'))
        {
            throw new InvalidOperationException($"{parameterName} 不能包含换行符。");
        }

        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{parameterName} 不能包含首尾空白字符。");
        }
    }
}
