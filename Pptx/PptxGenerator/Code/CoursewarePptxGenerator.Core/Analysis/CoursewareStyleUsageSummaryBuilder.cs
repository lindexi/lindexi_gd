using System.Text.RegularExpressions;
using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Builds a lightweight style usage summary from exported Markdown metadata.
/// </summary>
public sealed class CoursewareStyleUsageSummaryBuilder
{
    private const int MaximumItemsPerCategory = 12;
    private static readonly Regex ElementHeadingRegex = new(
        "^###\\s+([^.]*)\\.\\d+\\s*$",
        RegexOptions.CultureInvariant);
    private static readonly Regex TextStyleRegex = new(
        "^字号:\\s*([0-9]+(?:\\.[0-9]+)?)\\s*(px|pt)\\s*\\|\\s*字体:\\s*(.+?)\\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ColorRegex = new(
        "#[0-9A-Fa-f]{6}(?:[0-9A-Fa-f]{2})?",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Builds a style usage summary for the specified courseware package.
    /// </summary>
    public CoursewareStyleUsageSummary Build(
        CoursewareInputPackage inputPackage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);
        return Build(inputPackage.Slides, cancellationToken);
    }

    /// <summary>
    /// Builds a style usage summary for the specified slides.
    /// </summary>
    public CoursewareStyleUsageSummary Build(
        IReadOnlyList<CoursewareSlideInput> slides,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slides);

        var fonts = new Dictionary<string, UsageCounter>(StringComparer.OrdinalIgnoreCase);
        var fontSizes = new Dictionary<string, int>(StringComparer.Ordinal);
        var colors = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var slide in slides)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(slide);
            ScanMarkdown(slide.MarkdownText, fonts, fontSizes, colors, cancellationToken);
        }

        return new CoursewareStyleUsageSummary
        {
            Fonts = CreateFontItems(fonts),
            FontSizes = CreateItems(fontSizes),
            Colors = CreateItems(colors),
        };
    }

    private static void ScanMarkdown(
        string markdown,
        Dictionary<string, UsageCounter> fonts,
        Dictionary<string, int> fontSizes,
        Dictionary<string, int> colors,
        CancellationToken cancellationToken)
    {
        using var reader = new StringReader(markdown ?? string.Empty);
        string? fenceMarker = null;
        var currentElementIsText = false;
        while (reader.ReadLine() is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TryGetFenceMarker(line, out var marker))
            {
                if (fenceMarker is null)
                {
                    fenceMarker = marker;
                }
                else if (marker[0] == fenceMarker[0] && marker.Length >= fenceMarker.Length)
                {
                    fenceMarker = null;
                }

                continue;
            }

            if (fenceMarker is not null)
            {
                continue;
            }

            var headingMatch = ElementHeadingRegex.Match(line);
            if (headingMatch.Success)
            {
                currentElementIsText = string.Equals(
                    headingMatch.Groups[1].Value,
                    "文本",
                    StringComparison.Ordinal);
            }
            else if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                currentElementIsText = false;
            }

            AddColors(line, colors);
            if (!currentElementIsText)
            {
                continue;
            }

            var styleMatch = TextStyleRegex.Match(line);
            if (!styleMatch.Success)
            {
                continue;
            }

            var fontSize = NormalizeFontSize(styleMatch.Groups[1].Value, styleMatch.Groups[2].Value);
            Increment(fontSizes, fontSize);

            var font = styleMatch.Groups[3].Value.Trim();
            if (font.Length > 0)
            {
                if (fonts.TryGetValue(font, out var counter))
                {
                    fonts[font] = counter with { Count = counter.Count + 1 };
                }
                else
                {
                    fonts.Add(font, new UsageCounter(font, 1));
                }
            }
        }
    }

    private static bool TryGetFenceMarker(string line, out string marker)
    {
        var trimmedLine = line.TrimStart();
        if (trimmedLine.Length < 3 || trimmedLine[0] is not ('`' or '~'))
        {
            marker = string.Empty;
            return false;
        }

        var markerCharacter = trimmedLine[0];
        var markerLength = 1;
        while (markerLength < trimmedLine.Length && trimmedLine[markerLength] == markerCharacter)
        {
            markerLength++;
        }

        if (markerLength < 3)
        {
            marker = string.Empty;
            return false;
        }

        marker = trimmedLine[..markerLength];
        return true;
    }

    private static void AddColors(string line, Dictionary<string, int> colors)
    {
        foreach (Match match in ColorRegex.Matches(line))
        {
            Increment(colors, match.Value.ToUpperInvariant());
        }
    }

    private static string NormalizeFontSize(string value, string unit)
    {
        var normalizedValue = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)
            .ToString("0.############################", System.Globalization.CultureInfo.InvariantCulture);
        return normalizedValue + unit.ToLowerInvariant();
    }

    private static IReadOnlyList<CoursewareStyleUsageItem> CreateFontItems(
        Dictionary<string, UsageCounter> counters)
    {
        return counters.Values
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Value, StringComparer.Ordinal)
            .Take(MaximumItemsPerCategory)
            .Select(item => new CoursewareStyleUsageItem(item.Value, item.Count))
            .ToArray();
    }

    private static IReadOnlyList<CoursewareStyleUsageItem> CreateItems(Dictionary<string, int> counters)
    {
        return counters
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .Take(MaximumItemsPerCategory)
            .Select(item => new CoursewareStyleUsageItem(item.Key, item.Value))
            .ToArray();
    }

    private static void Increment(Dictionary<string, int> counters, string value)
    {
        counters[value] = counters.GetValueOrDefault(value) + 1;
    }

    private sealed record UsageCounter(string Value, int Count);

}
