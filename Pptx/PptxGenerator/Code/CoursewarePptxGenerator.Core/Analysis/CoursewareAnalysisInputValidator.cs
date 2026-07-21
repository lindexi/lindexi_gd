using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Validates an immutable analysis input immediately before model transmission.
/// </summary>
public static class CoursewareAnalysisInputValidator
{
    /// <summary>
    /// Validates protocol shape, canonical serialization, fingerprints, coverage and privacy invariants.
    /// </summary>
    public static void ValidateForTransmission(
        CoursewareAnalysisInput analysisInput,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysisInput);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(analysisInput.Prompt)
            || analysisInput.WasTruncated
            || analysisInput.TotalSlideCount <= 0
            || analysisInput.AnalyzedSlideCount != analysisInput.TotalSlideCount
            || analysisInput.CharacterCount != analysisInput.Prompt.Length
            || !string.Equals(
                analysisInput.StatisticsVersion,
                CoursewareAnalysisInputBuilder.CurrentStatisticsVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("课件分析输入的协议元数据无效。");
        }

        var actualViewFingerprint = ComputeFingerprint(analysisInput.Prompt);
        if (!string.Equals(
                actualViewFingerprint,
                analysisInput.AnalysisViewFingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("课件分析输入的发送视图指纹校验失败。");
        }

        ValidateFingerprint(analysisInput.SourceFingerprint, "原始事实指纹");
        CoursewareAnalysisEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize(
                analysisInput.Prompt,
                CoursewareAnalysisInputBuilder.ModelInputJsonContext.CoursewareAnalysisEnvelope)
                ?? throw new InvalidOperationException("课件分析输入 JSON 信封为空。");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("课件分析输入不是合法的 JSON 协议信封。", ex);
        }

        var canonicalPrompt = JsonSerializer.Serialize(
            envelope,
            CoursewareAnalysisInputBuilder.ModelInputJsonContext.CoursewareAnalysisEnvelope);
        if (!string.Equals(canonicalPrompt, analysisInput.Prompt, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("课件分析输入不是规范的 JSON 协议信封，可能包含未知、重复或重排字段。");
        }

        ValidateEnvelope(envelope, analysisInput, cancellationToken);
        CoursewareAnalysisInputBuilder.ThrowIfAbsolutePathRemains(analysisInput.Prompt);

        var expectedSectionCounts = CoursewareAnalysisInputBuilder.GetSectionCharacterCounts(
            envelope,
            analysisInput.Prompt.Length);
        if (analysisInput.SectionCharacterCounts.Task != expectedSectionCounts.Task
            || analysisInput.SectionCharacterCounts.CoursewareOverview != expectedSectionCounts.CoursewareOverview
            || analysisInput.SectionCharacterCounts.ResourceCatalog != expectedSectionCounts.ResourceCatalog
            || analysisInput.SectionCharacterCounts.Slides != expectedSectionCounts.Slides
            || analysisInput.SectionCharacterCounts.OutputRequirements != expectedSectionCounts.OutputRequirements)
        {
            throw new InvalidOperationException("课件分析输入的分区字符统计无效。");
        }

        if (analysisInput.EstimatedTokenCount
            != CoursewareTokenEstimator.Estimate(analysisInput.Prompt, cancellationToken))
        {
            throw new InvalidOperationException("课件分析输入的 Token 估算无效。");
        }

        ValidatePrivacyDiagnostics(analysisInput);
    }

    private static void ValidateEnvelope(
        CoursewareAnalysisEnvelope envelope,
        CoursewareAnalysisInput analysisInput,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(
                envelope.SchemaVersion,
                CoursewareAnalysisEnvelope.CurrentSchemaVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("课件分析输入 JSON 信封版本不受支持。");
        }

        if (!string.Equals(
                envelope.Task.Objective,
                CoursewareAnalysisInputBuilder.TaskObjective,
                StringComparison.Ordinal)
            || !envelope.Task.Requirements.SequenceEqual(
                CoursewareAnalysisInputBuilder.TaskRequirements,
                StringComparer.Ordinal)
            || !string.Equals(
                envelope.Task.DataBoundary,
                CoursewareAnalysisInputBuilder.TaskDataBoundary,
                StringComparison.Ordinal)
            || !string.Equals(
                envelope.Task.VisualEvidenceBoundary,
                CoursewareAnalysisInputBuilder.VisualEvidenceBoundary,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("课件分析输入的可信任务指令已被修改。");
        }

        if (!envelope.OutputRequirements.Requirements.SequenceEqual(
                CoursewareAnalysisInputBuilder.OutputRequirements,
                StringComparer.Ordinal)
            || !string.Equals(
                envelope.OutputRequirements.SubmissionProtocol,
                CoursewareAnalysisInputBuilder.SubmissionProtocol,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("课件分析输入的输出协议已被修改。");
        }

        var overview = envelope.CoursewareOverview;
        if (string.IsNullOrWhiteSpace(overview.CoursewareName)
            || overview.CoursewareName.Contains('\r')
            || overview.CoursewareName.Contains('\n')
            || overview.TotalSlideCount != analysisInput.TotalSlideCount
            || overview.LoadedMarkdownSlideCount != analysisInput.AnalyzedSlideCount
            || overview.AvailableScreenshotCount < 0
            || overview.AvailableScreenshotCount > analysisInput.TotalSlideCount
            || !string.Equals(
                overview.StatisticsVersion,
                analysisInput.StatisticsVersion,
                StringComparison.Ordinal)
            || !string.Equals(
                overview.SourceFingerprint,
                analysisInput.SourceFingerprint,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("课件分析输入概览与本地输入元数据不一致。");
        }

        ValidateSlides(envelope.Slides, analysisInput.AnalyzedSlideCount, cancellationToken);
        ValidateResources(envelope.Resources);
        ValidateDistributions(envelope);
        ValidateWarnings(overview.Warnings);
    }

    private static void ValidateSlides(
        IReadOnlyList<CoursewareAnalysisSlideView> slides,
        int expectedCount,
        CancellationToken cancellationToken)
    {
        if (slides.Count != expectedCount)
        {
            throw new InvalidOperationException("课件分析输入的页面数组不完整。");
        }

        var slideIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < slides.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var slide = slides[index];
            if (string.IsNullOrWhiteSpace(slide.SlideId)
                || slide.SlideId.Contains('\r')
                || slide.SlideId.Contains('\n')
                || !slideIds.Add(slide.SlideId)
                || slide.SlideIndex != index
                || slide.PageNumber != index + 1
                || !double.IsFinite(slide.Width)
                || !double.IsFinite(slide.Height)
                || slide.Width <= 0
                || slide.Height <= 0
                || slide.Markdown is null)
            {
                throw new InvalidOperationException($"课件分析输入中的第 {index + 1} 页身份无效。");
            }

            ValidateFingerprint(slide.SourceFingerprint, $"第 {index + 1} 页源指纹");
        }
    }

    private static void ValidateResources(IReadOnlyList<CoursewareAnalysisResourceView> resources)
    {
        var resourceIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < resources.Count; index++)
        {
            var resource = resources[index];
            if (string.IsNullOrWhiteSpace(resource.ResourceId)
                || resource.ResourceId.Contains('\r')
                || resource.ResourceId.Contains('\n')
                || string.IsNullOrWhiteSpace(resource.ResourceType)
                || resource.ResourceType.Contains('\r')
                || resource.ResourceType.Contains('\n')
                || !resourceIds.Add(resource.ResourceId))
            {
                throw new InvalidOperationException($"课件分析输入中的第 {index + 1} 个资源无效。");
            }
        }
    }

    private static void ValidateDistributions(CoursewareAnalysisEnvelope envelope)
    {
        var expectedDimensions = envelope.Slides
            .GroupBy(slide => slide.Width.ToString("0.##", CultureInfo.InvariantCulture)
                + "×"
                + slide.Height.ToString("0.##", CultureInfo.InvariantCulture))
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => (group.Key, Count: group.Count()))
            .ToArray();
        var actualDimensions = envelope.CoursewareOverview.DimensionDistribution;
        if (actualDimensions.Count != expectedDimensions.Length
            || actualDimensions.Where((item, index) =>
                    !string.Equals(item.Key, expectedDimensions[index].Key, StringComparison.Ordinal)
                    || item.Count != expectedDimensions[index].Count)
                .Any())
        {
            throw new InvalidOperationException("课件分析输入的页面尺寸分布无效。");
        }

        var expectedResourceTypes = envelope.Resources
            .GroupBy(resource => resource.ResourceType, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => (group.Key, Count: group.Count()))
            .ToArray();
        var actualResourceTypes = envelope.CoursewareOverview.ResourceTypeDistribution;
        if (actualResourceTypes.Count != expectedResourceTypes.Length
            || actualResourceTypes.Where((item, index) =>
                    !string.Equals(item.Key, expectedResourceTypes[index].Key, StringComparison.Ordinal)
                    || item.Count != expectedResourceTypes[index].Count)
                .Any())
        {
            throw new InvalidOperationException("课件分析输入的资源类型分布无效。");
        }
    }

    private static void ValidateWarnings(IReadOnlyList<CoursewareAnalysisWarningView> warnings)
    {
        for (var index = 0; index < warnings.Count; index++)
        {
            var warning = warnings[index];
            if (string.IsNullOrWhiteSpace(warning.Code)
                || warning.Code.Contains('\r')
                || warning.Code.Contains('\n')
                || warning.Message is null
                || warning.Message.Contains('\r')
                || warning.Message.Contains('\n'))
            {
                throw new InvalidOperationException($"课件分析输入中的第 {index + 1} 条警告无效。");
            }
        }
    }

    private static void ValidatePrivacyDiagnostics(CoursewareAnalysisInput analysisInput)
    {
        for (var index = 0; index < analysisInput.PrivacyDiagnostics.Count; index++)
        {
            var diagnostic = analysisInput.PrivacyDiagnostics[index];
            if (!string.Equals(
                    diagnostic.DiagnosticId,
                    $"path-{index + 1:D4}",
                    StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(diagnostic.Section)
                || diagnostic.Action != CoursewarePathPrivacyAction.Redacted)
            {
                throw new InvalidOperationException("课件分析输入的路径隐私诊断无效。");
            }

            ValidateFingerprint(diagnostic.OriginalValueFingerprint, "路径隐私诊断指纹");
        }

        var expectedWarnings = analysisInput.PrivacyDiagnostics.Count == 0
            ? Array.Empty<string>()
            : [$"发送给模型的分析视图已脱敏 {analysisInput.PrivacyDiagnostics.Count} 处本地绝对路径；本地原始事实未被修改。"];
        if (!analysisInput.Warnings.SequenceEqual(expectedWarnings, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("课件分析输入的隐私警告无效。");
        }
    }

    private static void ValidateFingerprint(string fingerprint, string fieldName)
    {
        if (fingerprint.Length != 64 || fingerprint.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException($"{fieldName}无效。");
        }
    }

    private static string ComputeFingerprint(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
