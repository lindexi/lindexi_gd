using CoursewarePptxGenerator.Core.Models;
using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the result of analyzing one courseware package as a whole.
/// </summary>
public sealed record CoursewareThemeAnalysisResult
{
    /// <summary>
    /// Gets the validated whole-courseware theme.
    /// </summary>
    public required CoursewareTheme Theme { get; init; }

    /// <summary>
    /// Gets the frozen executable design system.
    /// </summary>
    public required CoursewareDesignSystem DesignSystem { get; init; }

    /// <summary>
    /// Gets deterministic facts extracted from complete slide Markdown.
    /// </summary>
    public CoursewareStructuredFactReport StructuredFacts { get; init; } = new();

    /// <summary>
    /// Gets deterministic design-system validation results.
    /// </summary>
    public CoursewareDesignSystemValidationReport DesignSystemValidation { get; init; } = new();

    /// <summary>
    /// Gets SlideML template compilation and stress-test results.
    /// </summary>
    public CoursewareTemplateValidationReport TemplateValidation { get; init; } = new();

    /// <summary>
    /// Gets optional image-backed visual-analysis evidence.
    /// </summary>
    public CoursewareVisualAnalysisReport VisualAnalysis { get; init; } = new();

    /// <summary>
    /// Gets the reference canvas used when the theme coordinates were produced.
    /// </summary>
    public required SlideDocumentContext ReferenceCanvas { get; init; }

    /// <summary>
    /// Gets the independently verifiable capability states for this result.
    /// </summary>
    public CoursewareAnalysisCapabilityStates CapabilityStates { get; init; } = new();

    /// <summary>
    /// Gets the generated theme display name.
    /// </summary>
    public string ThemeTitle => Theme.Title;

    /// <summary>
    /// Gets the generated theme summary.
    /// </summary>
    public string ThemeDescription => Theme.Summary;

    /// <summary>
    /// Gets the time when the analysis completed.
    /// </summary>
    public required DateTimeOffset AnalyzedAt { get; init; }

    /// <summary>
    /// Gets the number of slides included in the analysis.
    /// </summary>
    public required int TotalSlideCount { get; init; }

    /// <summary>
    /// Gets the number of slides represented in the model input.
    /// </summary>
    public required int AnalyzedSlideCount { get; init; }

    /// <summary>
    /// Gets the total analysis duration.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets non-blocking warnings produced by the analysis.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
