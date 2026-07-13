namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Extracts display title and summary text from exported slide Markdown.
/// </summary>
public sealed class CoursewareSlideSummaryService
{
    private const int MaxSummaryLength = 110;

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

        return ExtractReadableLines(markdownText).FirstOrDefault() ?? $"第 {pageNumber} 页";
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

        var summaryLines = ExtractReadableLines(markdownText)
            .Take(3)
            .ToArray();

        if (summaryLines.Length == 0)
        {
            return "已加载页面 Markdown，等待美化。";
        }

        var summary = string.Join(" ", summaryLines);
        return summary.Length <= MaxSummaryLength ? summary : $"{summary[..MaxSummaryLength]}...";
    }

    private static IEnumerable<string> ExtractReadableLines(string markdownText)
    {
        var inContentCodeBlock = false;
        var expectingContentCodeBlock = false;
        foreach (var rawLine in SplitLines(markdownText))
        {
            var line = rawLine.Trim();
            if (line == "#### 内容")
            {
                expectingContentCodeBlock = true;
                continue;
            }

            if (IsCodeFence(line))
            {
                if (expectingContentCodeBlock)
                {
                    inContentCodeBlock = true;
                    expectingContentCodeBlock = false;
                }
                else if (inContentCodeBlock)
                {
                    inContentCodeBlock = false;
                }

                continue;
            }

            if (inContentCodeBlock && !string.IsNullOrWhiteSpace(line))
            {
                yield return line;
            }
        }
    }

    private static bool IsCodeFence(string line)
    {
        return line.Length >= 3 && line.All(character => character == '`');
    }

    private static string[] SplitLines(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
    }
}
