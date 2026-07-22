using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Selects a bounded, deterministic set of screenshot samples for optional visual analysis.
/// </summary>
public sealed class CoursewareVisualSampleSelector
{
    /// <summary>The default maximum screenshot count supplied to one analysis request.</summary>
    public const int DefaultMaximumSampleCount = 6;

    /// <summary>
    /// Selects path-free screenshot samples from structured facts and locally available screenshots.
    /// </summary>
    public IReadOnlyList<CoursewareVisualSample> Select(
        CoursewareInputPackage inputPackage,
        CoursewareStructuredFactReport factReport,
        int maximumSampleCount = DefaultMaximumSampleCount)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);
        ArgumentNullException.ThrowIfNull(factReport);
        if (maximumSampleCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSampleCount));
        }

        var availableSlideIds = inputPackage.Slides
            .Where(slide => slide.ScreenshotFile?.Exists == true)
            .Select(slide => slide.SlideId)
            .ToHashSet(StringComparer.Ordinal);
        if (availableSlideIds.Count == 0)
        {
            return [];
        }

        var selected = new Dictionary<string, CoursewareVisualSample>(StringComparer.Ordinal);
        var factsById = factReport.Slides.ToDictionary(slide => slide.SlideId, StringComparer.Ordinal);
        AddFirstAvailable(inputPackage, availableSlideIds, selected);

        foreach (var cluster in factReport.LayoutClusters)
        {
            AddSample(
                cluster.RepresentativeSlideId,
                CoursewareVisualSampleRole.LayoutRepresentative,
                $"代表布局簇 {cluster.ClusterId}（{cluster.LayoutSignature}）。",
                "验证该布局簇的真实视觉层级、留白和图文关系。",
                availableSlideIds,
                factsById,
                selected);
        }

        var outlier = factReport.Slides
            .Where(slide => availableSlideIds.Contains(slide.SlideId))
            .OrderByDescending(slide => slide.RiskCodes.Count)
            .ThenByDescending(slide => slide.OccupiedAreaRatio)
            .ThenBy(slide => slide.PageNumber)
            .FirstOrDefault();
        if (outlier is not null && outlier.RiskCodes.Count > 0)
        {
            AddSample(
                outlier.SlideId,
                CoursewareVisualSampleRole.Outlier,
                $"结构化事实标记风险：{string.Join("、", outlier.RiskCodes)}。",
                "判断结构化风险候选是否在真实截图中形成拥挤、遮挡或失衡。",
                availableSlideIds,
                factsById,
                selected);
        }

        var assetSlide = factReport.Slides
            .Where(slide => availableSlideIds.Contains(slide.SlideId) && slide.ResourceIds.Count > 0)
            .OrderByDescending(slide => slide.ResourceIds.Count)
            .ThenBy(slide => slide.PageNumber)
            .FirstOrDefault();
        if (assetSlide is not null)
        {
            AddSample(
                assetSlide.SlideId,
                CoursewareVisualSampleRole.AssetUsage,
                $"该页引用 {assetSlide.ResourceIds.Count} 个逻辑素材。",
                "观察素材在页面中的真实角色、裁剪和视觉占比，不推断未显示素材的内容。",
                availableSlideIds,
                factsById,
                selected);
        }

        var boundarySlides = inputPackage.Slides.Count == 1
            ? inputPackage.Slides
            : new[] { inputPackage.Slides[0], inputPackage.Slides[^1] };
        foreach (var slide in boundarySlides)
        {
            AddSample(
                slide.SlideId,
                CoursewareVisualSampleRole.Boundary,
                slide.PageNumber == 1 ? "课件首屏边界样本。" : "课件末页边界样本。",
                "验证课件首尾页面与主体设计系统之间的视觉一致性和必要变化。",
                availableSlideIds,
                factsById,
                selected);
        }

        return selected.Values
            .OrderBy(sample => sample.Role)
            .ThenBy(sample => sample.PageNumber)
            .Take(maximumSampleCount)
            .ToArray();
    }

    private static void AddFirstAvailable(
        CoursewareInputPackage inputPackage,
        IReadOnlySet<string> availableSlideIds,
        IDictionary<string, CoursewareVisualSample> selected)
    {
        var slide = inputPackage.Slides.First(item => availableSlideIds.Contains(item.SlideId));
        selected[slide.SlideId] = new CoursewareVisualSample
        {
            SlideId = slide.SlideId,
            PageNumber = slide.PageNumber,
            Role = CoursewareVisualSampleRole.Overview,
            SelectionReason = "首个可用截图，用于建立课件视觉入口。",
            Question = "观察可见的配色、视觉层级和构图，不覆盖 Markdown 教学事实。",
        };
    }

    private static void AddSample(
        string slideId,
        CoursewareVisualSampleRole role,
        string reason,
        string question,
        IReadOnlySet<string> availableSlideIds,
        IReadOnlyDictionary<string, CoursewareSlideStructuredFacts> factsById,
        IDictionary<string, CoursewareVisualSample> selected)
    {
        if (selected.ContainsKey(slideId)
            || !availableSlideIds.Contains(slideId)
            || !factsById.TryGetValue(slideId, out var facts))
        {
            return;
        }

        selected[slideId] = new CoursewareVisualSample
        {
            SlideId = slideId,
            PageNumber = facts.PageNumber,
            Role = role,
            SelectionReason = reason,
            Question = question,
        };
    }
}
