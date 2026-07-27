using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Analysis;

internal static class CoursewareMarkdownPageInfoValidator
{
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

    internal static void Validate(CoursewareSlideInput slide, int expectedPosition)
    {
        ArgumentNullException.ThrowIfNull(slide);
        ArgumentNullException.ThrowIfNull(slide.MarkdownText);
        if (slide.SlideIndex != expectedPosition || slide.PageNumber != expectedPosition + 1)
        {
            throw new InvalidDataException("课件页面的 SlideIndex 和页码必须连续且顺序一致。");
        }

        if (!double.IsFinite(slide.Width)
            || !double.IsFinite(slide.Height)
            || slide.Width <= 0
            || slide.Height <= 0)
        {
            throw new InvalidDataException($"第 {slide.PageNumber} 页的页面尺寸必须为有限正数。");
        }

        var pageInformation = ExtractPageInformation(slide.MarkdownText, slide.PageNumber);
        var markdownSlideId = ReadSingleMarkdownValue(MarkdownSlideIdPattern, pageInformation, "Id", slide.PageNumber);
        if (!string.Equals(markdownSlideId, slide.SlideId, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"第 {slide.PageNumber} 页 Markdown Id 与清单 SlideId 不一致。");
        }

        var markdownPageNumber = ReadSingleMarkdownValue(
            MarkdownPageNumberPattern,
            pageInformation,
            "序号(1-base)",
            slide.PageNumber);
        if (!int.TryParse(markdownPageNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPageNumber)
            || parsedPageNumber != slide.PageNumber)
        {
            throw new InvalidDataException($"第 {slide.PageNumber} 页 Markdown 序号与清单页码不一致。");
        }

        var dimensionMatches = MarkdownDimensionsPattern.Matches(pageInformation);
        if (dimensionMatches.Count > 1)
        {
            throw new InvalidDataException($"第 {slide.PageNumber} 页 Markdown 包含多个尺寸字段。");
        }

        if (dimensionMatches.Count == 0
            || !double.TryParse(dimensionMatches[0].Groups["width"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var markdownWidth)
            || !double.TryParse(dimensionMatches[0].Groups["height"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var markdownHeight))
        {
            throw new InvalidDataException($"第 {slide.PageNumber} 页 Markdown 缺少合法的尺寸字段。");
        }

        if (Math.Abs(markdownWidth - slide.Width) > 0.01 || Math.Abs(markdownHeight - slide.Height) > 0.01)
        {
            throw new InvalidDataException($"第 {slide.PageNumber} 页 Markdown 尺寸与清单尺寸不一致。");
        }
    }

    private static string ExtractPageInformation(string markdown, int pageNumber)
    {
        var headingMatch = PageInformationHeadingPattern.Match(markdown);
        if (!headingMatch.Success)
        {
            throw new InvalidDataException($"第 {pageNumber} 页 Markdown 必须以“## 页面信息”章节开始。");
        }

        var sectionStart = headingMatch.Index + headingMatch.Length;
        var sectionEndMatch = PageInformationEndPattern.Match(markdown, sectionStart);
        var sectionEnd = sectionEndMatch.Success ? sectionEndMatch.Index : markdown.Length;
        return markdown[sectionStart..sectionEnd];
    }

    private static string ReadSingleMarkdownValue(Regex pattern, string markdown, string fieldName, int pageNumber)
    {
        var matches = pattern.Matches(markdown);
        if (matches.Count > 1)
        {
            throw new InvalidDataException($"第 {pageNumber} 页 Markdown 包含多个 {fieldName} 字段。");
        }

        if (matches.Count == 0)
        {
            throw new InvalidDataException($"第 {pageNumber} 页 Markdown 缺少必需的 {fieldName} 字段。");
        }

        return matches[0].Groups[1].Value.Trim();
    }
}
