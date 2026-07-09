using System.Text.RegularExpressions;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Extracts display title and summary text from exported slide Markdown.
/// </summary>
public sealed class CoursewareSlideSummaryService
{
    private const int MaxSummaryLength = 110;
    private static readonly Regex DefaultSlideTitlePattern = new("^Slide\\s+\\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex BoldTextPattern = new("\\*\\*(.+?)\\*\\*", RegexOptions.CultureInvariant);

    /// <summary>
    /// Creates a display title for a loaded slide.
    /// </summary>
    /// <param name="markdownText">The slide Markdown text.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <returns>The extracted title.</returns>
    public string CreateTitle(string markdownText, int pageNumber)
    {
        if (string.IsNullOrWhiteSpace(markdownText))
        {
            return $"第 {pageNumber} 页";
        }

        var lines = SplitLines(markdownText);
        var firstHeading = lines
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(firstHeading))
        {
            var title = firstHeading[2..].Trim();
            if (!IsDefaultSlideTitle(title))
            {
                return title;
            }
        }

        var secondaryHeading = lines
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("## ", StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(secondaryHeading))
        {
            return secondaryHeading[3..].Trim();
        }

        var boldText = lines
            .Select(line => BoldTextPattern.Match(line))
            .FirstOrDefault(match => match.Success);
        if (boldText is not null)
        {
            return boldText.Groups[1].Value.Trim();
        }

        return $"第 {pageNumber} 页";
    }

    /// <summary>
    /// Creates a short display summary for a loaded slide.
    /// </summary>
    /// <param name="markdownText">The slide Markdown text.</param>
    /// <returns>The extracted summary.</returns>
    public string CreateSummary(string markdownText)
    {
        if (string.IsNullOrWhiteSpace(markdownText))
        {
            return "已加载页面 Markdown，等待美化。";
        }

        var summaryLines = SplitLines(markdownText)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Where(line => !IsMetadataLine(line))
            .Where(line => !line.StartsWith("# ", StringComparison.Ordinal))
            .Take(3)
            .ToArray();

        if (summaryLines.Length == 0)
        {
            return "已加载页面 Markdown，等待美化。";
        }

        var summary = string.Join(" ", summaryLines);
        return summary.Length <= MaxSummaryLength ? summary : $"{summary[..MaxSummaryLength]}...";
    }

    private static bool IsDefaultSlideTitle(string title)
    {
        return DefaultSlideTitlePattern.IsMatch(title);
    }

    private static bool IsMetadataLine(string line)
    {
        return line == "---"
            || line.StartsWith("- SlideIndex:", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("- SlideId:", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("- Size:", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("- Screenshot:", StringComparison.OrdinalIgnoreCase);
    }

    private static string[] SplitLines(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
    }
}
