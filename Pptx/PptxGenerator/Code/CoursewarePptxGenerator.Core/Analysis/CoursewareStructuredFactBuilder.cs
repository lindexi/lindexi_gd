using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGenerator.Core.Serialization;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Extracts deterministic structured facts from the complete courseware Markdown analysis view.
/// </summary>
public sealed class CoursewareStructuredFactBuilder
{
    private static readonly Regex ElementRectangleRegex = new(
        @"^\s*-\s*(?<kind>[^:：]+?)\s*[:：]\s*\(\s*(?<x>-?\d+(?:\.\d+)?)\s*[,，]\s*(?<y>-?\d+(?:\.\d+)?)\s*\)\s*(?<width>\d+(?:\.\d+)?)\s*[×xX*]\s*(?<height>\d+(?:\.\d+)?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
    private static readonly Regex FontRegex = new(
        @"字体\s*[:：]\s*(?<font>[^|\r\n]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex FontSizeRegex = new(
        @"字号\s*[:：]\s*(?<size>\d+(?:\.\d+)?)\s*(?:px|pt)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex ColorRegex = new(
        @"(?<![0-9A-Fa-f])#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6})(?![0-9A-Fa-f])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex CodeBlockRegex = new(
        @"```(?:[^\r\n]*)\r?\n(?<content>[\s\S]*?)\r?\n```",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Builds a complete deterministic fact report from one validated analysis input.
    /// </summary>
    public CoursewareStructuredFactReport Build(
        CoursewareAnalysisInput analysisInput,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysisInput);
        cancellationToken.ThrowIfCancellationRequested();
        CoursewareAnalysisInputValidator.ValidateForTransmission(analysisInput, cancellationToken);

        var envelope = JsonSerializer.Deserialize(
            analysisInput.Prompt,
            CoursewareAnalysisJsonSerializerContext.Default.CoursewareAnalysisEnvelope)
            ?? throw new InvalidOperationException("课件分析信封为空。");
        var resourceIds = envelope.Resources
            .Select(resource => resource.ResourceId)
            .Where(resourceId => !string.IsNullOrWhiteSpace(resourceId))
            .ToHashSet(StringComparer.Ordinal);
        var slides = envelope.Slides
            .OrderBy(slide => slide.SlideIndex)
            .Select(slide => BuildSlideFacts(slide, resourceIds, cancellationToken))
            .ToArray();
        var clusters = BuildClusters(slides);
        var clusterBySlideId = clusters
            .SelectMany(cluster => cluster.SlideIds.Select(slideId => (slideId, cluster.ClusterId)))
            .ToDictionary(item => item.slideId, item => item.ClusterId, StringComparer.Ordinal);
        slides = slides
            .Select(slide => slide with { LayoutClusterId = clusterBySlideId[slide.SlideId] })
            .ToArray();

        return new CoursewareStructuredFactReport
        {
            InputFingerprint = analysisInput.AnalysisViewFingerprint,
            Slides = slides,
            Fonts = AggregateStringDistributions(slides.SelectMany(slide => slide.Fonts)),
            FontSizes = AggregateNumericDistributions(slides.SelectMany(slide => slide.FontSizes)),
            Colors = AggregateStringDistributions(slides.SelectMany(slide => slide.Colors)),
            ResourceReferences = slides
                .SelectMany(slide => slide.ResourceIds)
                .GroupBy(resourceId => resourceId, StringComparer.Ordinal)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new CoursewareFactDistributionItem { Value = group.Key, Count = group.Count() })
                .ToArray(),
            LayoutClusters = clusters,
        };
    }

    private static CoursewareSlideStructuredFacts BuildSlideFacts(
        CoursewareAnalysisSlideView slide,
        IReadOnlySet<string> knownResourceIds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = new List<string>();
        var rectangles = new List<ElementRectangle>();
        foreach (Match match in ElementRectangleRegex.Matches(slide.Markdown))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryParseRectangle(match, out var rectangle))
            {
                diagnostics.Add($"ElementRectangleParseFailed:{match.Groups["kind"].Value.Trim()}");
                continue;
            }

            rectangles.Add(rectangle);
        }

        var textCount = rectangles.Count(rectangle => IsTextKind(rectangle.Kind));
        var imageCount = rectangles.Count(rectangle => IsImageKind(rectangle.Kind));
        var textCharacterCount = CodeBlockRegex.Matches(slide.Markdown)
            .Select(match => match.Groups["content"].Value.Count(character => !char.IsWhiteSpace(character)))
            .Sum();
        var clippedRectangles = rectangles
            .Select(rectangle => ClipToCanvas(rectangle, slide.Width, slide.Height))
            .Where(rectangle => rectangle.Width > 0 && rectangle.Height > 0)
            .ToArray();
        var contentBounds = CreateContentBounds(clippedRectangles, slide.Width, slide.Height);
        var occupiedAreaRatio = slide.Width <= 0 || slide.Height <= 0
            ? 0
            : Math.Clamp(clippedRectangles.Sum(rectangle => rectangle.Width * rectangle.Height) / (slide.Width * slide.Height), 0, 1);
        var riskCodes = new List<string>();
        if (rectangles.Any(rectangle => IsOutOfBounds(rectangle, slide.Width, slide.Height)))
        {
            riskCodes.Add("ElementOutOfBounds");
        }

        if (HasSignificantOverlap(clippedRectangles))
        {
            riskCodes.Add("ElementOverlapCandidate");
        }

        var densityClass = GetDensityClass(rectangles.Count, textCharacterCount, occupiedAreaRatio);
        if (densityClass == "High")
        {
            riskCodes.Add("HighDensityCandidate");
        }

        var resourceIds = knownResourceIds
            .Where(resourceId => slide.Markdown.Contains(resourceId, StringComparison.Ordinal))
            .OrderBy(resourceId => resourceId, StringComparer.Ordinal)
            .ToArray();
        return new CoursewareSlideStructuredFacts
        {
            SlideId = slide.SlideId,
            PageNumber = slide.PageNumber,
            Width = slide.Width,
            Height = slide.Height,
            ElementCount = rectangles.Count,
            TextElementCount = textCount,
            ImageElementCount = imageCount,
            TextCharacterCount = textCharacterCount,
            OccupiedAreaRatio = Math.Round(occupiedAreaRatio, 4),
            ContentBounds = contentBounds,
            LayoutSignature = CreateLayoutSignature(rectangles, slide.Width, slide.Height, textCount, imageCount),
            DensityClass = densityClass,
            Fonts = CreateStringDistribution(FontRegex.Matches(slide.Markdown).Select(match => match.Groups["font"].Value.Trim())),
            FontSizes = CreateNumericDistribution(FontSizeRegex.Matches(slide.Markdown).Select(match => match.Groups["size"].Value)),
            Colors = CreateStringDistribution(ColorRegex.Matches(slide.Markdown).Select(match => match.Value.ToUpperInvariant())),
            ResourceIds = resourceIds,
            RiskCodes = riskCodes,
            Diagnostics = diagnostics,
        };
    }

    private static bool TryParseRectangle(Match match, out ElementRectangle rectangle)
    {
        var parsedX = double.TryParse(match.Groups["x"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var x);
        var parsedY = double.TryParse(match.Groups["y"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var y);
        var parsedWidth = double.TryParse(match.Groups["width"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var width);
        var parsedHeight = double.TryParse(match.Groups["height"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var height);
        var succeeded = parsedX && parsedY && parsedWidth && parsedHeight && width > 0 && height > 0;
        rectangle = succeeded
            ? new ElementRectangle(match.Groups["kind"].Value.Trim(), x, y, width, height)
            : default;
        return succeeded;
    }

    private static ElementRectangle ClipToCanvas(ElementRectangle rectangle, double width, double height)
    {
        var left = Math.Clamp(rectangle.X, 0, width);
        var top = Math.Clamp(rectangle.Y, 0, height);
        var right = Math.Clamp(rectangle.X + rectangle.Width, 0, width);
        var bottom = Math.Clamp(rectangle.Y + rectangle.Height, 0, height);
        return rectangle with
        {
            X = left,
            Y = top,
            Width = Math.Max(0, right - left),
            Height = Math.Max(0, bottom - top),
        };
    }

    private static CoursewareNormalizedRectangle? CreateContentBounds(
        IReadOnlyList<ElementRectangle> rectangles,
        double width,
        double height)
    {
        if (rectangles.Count == 0 || width <= 0 || height <= 0)
        {
            return null;
        }

        var left = rectangles.Min(rectangle => rectangle.X);
        var top = rectangles.Min(rectangle => rectangle.Y);
        var right = rectangles.Max(rectangle => rectangle.X + rectangle.Width);
        var bottom = rectangles.Max(rectangle => rectangle.Y + rectangle.Height);
        return new CoursewareNormalizedRectangle
        {
            X = Math.Round(left / width, 4),
            Y = Math.Round(top / height, 4),
            Width = Math.Round((right - left) / width, 4),
            Height = Math.Round((bottom - top) / height, 4),
        };
    }

    private static bool IsOutOfBounds(ElementRectangle rectangle, double width, double height)
    {
        return rectangle.X < 0
            || rectangle.Y < 0
            || rectangle.X + rectangle.Width > width
            || rectangle.Y + rectangle.Height > height;
    }

    private static bool HasSignificantOverlap(IReadOnlyList<ElementRectangle> rectangles)
    {
        for (var index = 0; index < rectangles.Count; index++)
        {
            for (var otherIndex = index + 1; otherIndex < rectangles.Count; otherIndex++)
            {
                var first = rectangles[index];
                var second = rectangles[otherIndex];
                var overlapWidth = Math.Max(0, Math.Min(first.X + first.Width, second.X + second.Width) - Math.Max(first.X, second.X));
                var overlapHeight = Math.Max(0, Math.Min(first.Y + first.Height, second.Y + second.Height) - Math.Max(first.Y, second.Y));
                var overlapArea = overlapWidth * overlapHeight;
                var smallerArea = Math.Min(first.Width * first.Height, second.Width * second.Height);
                if (smallerArea > 0 && overlapArea / smallerArea >= 0.25)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string GetDensityClass(int elementCount, int textCharacterCount, double occupiedAreaRatio)
    {
        var score = elementCount / 12d + textCharacterCount / 500d + occupiedAreaRatio;
        return score switch
        {
            >= 2.4 => "High",
            >= 1.1 => "Medium",
            _ => "Low",
        };
    }

    private static string CreateLayoutSignature(
        IReadOnlyList<ElementRectangle> rectangles,
        double width,
        double height,
        int textCount,
        int imageCount)
    {
        if (rectangles.Count == 0 || width <= 0 || height <= 0)
        {
            return "empty";
        }

        var leftCount = rectangles.Count(rectangle => rectangle.X + rectangle.Width / 2 < width / 2);
        var rightCount = rectangles.Count - leftCount;
        var topCount = rectangles.Count(rectangle => rectangle.Y + rectangle.Height / 2 < height / 2);
        var bottomCount = rectangles.Count - topCount;
        var horizontalBalance = Math.Abs(leftCount - rightCount) <= Math.Max(1, rectangles.Count / 4) ? "balanced-x" : leftCount > rightCount ? "left-heavy" : "right-heavy";
        var verticalBalance = Math.Abs(topCount - bottomCount) <= Math.Max(1, rectangles.Count / 4) ? "balanced-y" : topCount > bottomCount ? "top-heavy" : "bottom-heavy";
        var mediaKind = imageCount == 0 ? "text-only" : textCount == 0 ? "image-only" : "mixed";
        return $"{mediaKind}:{horizontalBalance}:{verticalBalance}:{Math.Min(rectangles.Count, 9)}";
    }

    private static IReadOnlyList<CoursewareLayoutCluster> BuildClusters(IReadOnlyList<CoursewareSlideStructuredFacts> slides)
    {
        return slides
            .GroupBy(slide => slide.LayoutSignature, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select((group, index) => new CoursewareLayoutCluster
            {
                ClusterId = $"layout-{index + 1:D2}",
                LayoutSignature = group.Key,
                SlideIds = group.Select(slide => slide.SlideId).OrderBy(slideId => slideId, StringComparer.Ordinal).ToArray(),
                RepresentativeSlideId = group
                    .OrderBy(slide => Math.Abs(slide.OccupiedAreaRatio - group.Average(item => item.OccupiedAreaRatio)))
                    .ThenBy(slide => slide.PageNumber)
                    .First()
                    .SlideId,
            })
            .ToArray();
    }

    private static IReadOnlyList<CoursewareFactDistributionItem> CreateStringDistribution(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CoursewareFactDistributionItem { Value = group.Key, Count = group.Count() })
            .ToArray();
    }

    private static IReadOnlyList<CoursewareNumericFactDistributionItem> CreateNumericDistribution(IEnumerable<string> values)
    {
        return values
            .Select(value => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : (double?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .GroupBy(value => value)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => new CoursewareNumericFactDistributionItem { Value = group.Key, Count = group.Count() })
            .ToArray();
    }

    private static IReadOnlyList<CoursewareFactDistributionItem> AggregateStringDistributions(
        IEnumerable<CoursewareFactDistributionItem> distributions)
    {
        return distributions
            .GroupBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Sum(item => item.Count))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CoursewareFactDistributionItem { Value = group.Key, Count = group.Sum(item => item.Count) })
            .ToArray();
    }

    private static IReadOnlyList<CoursewareNumericFactDistributionItem> AggregateNumericDistributions(
        IEnumerable<CoursewareNumericFactDistributionItem> distributions)
    {
        return distributions
            .GroupBy(item => item.Value)
            .OrderByDescending(group => group.Sum(item => item.Count))
            .ThenBy(group => group.Key)
            .Select(group => new CoursewareNumericFactDistributionItem { Value = group.Key, Count = group.Sum(item => item.Count) })
            .ToArray();
    }

    private static bool IsTextKind(string kind)
    {
        return kind.Contains("文本", StringComparison.OrdinalIgnoreCase)
            || kind.Contains("Text", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImageKind(string kind)
    {
        return kind.Contains("图片", StringComparison.OrdinalIgnoreCase)
            || kind.Contains("图像", StringComparison.OrdinalIgnoreCase)
            || kind.Contains("Image", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct ElementRectangle(string Kind, double X, double Y, double Width, double Height);
}
