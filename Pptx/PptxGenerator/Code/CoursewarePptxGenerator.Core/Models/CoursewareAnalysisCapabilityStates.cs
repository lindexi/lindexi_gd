namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Identifies the execution status of one independently verifiable courseware capability.
/// </summary>
public enum CoursewareCapabilityStatus
{
    /// <summary>The capability was not requested for the current run.</summary>
    NotRequested,

    /// <summary>The current runtime cannot execute the capability.</summary>
    NotSupported,

    /// <summary>The capability was executed but failed its quality gate.</summary>
    Failed,

    /// <summary>The capability completed and passed its quality gate.</summary>
    Passed,
}

/// <summary>
/// Records capability states without collapsing optional or unsupported work into overall success.
/// </summary>
public sealed record CoursewareAnalysisCapabilityStates
{
    /// <summary>Gets the full-text analysis status.</summary>
    public CoursewareCapabilityStatus TextAnalysis { get; set; } = CoursewareCapabilityStatus.NotRequested;

    /// <summary>Gets the executable design-system status.</summary>
    public CoursewareCapabilityStatus DesignSystem { get; set; } = CoursewareCapabilityStatus.NotRequested;

    /// <summary>Gets the structured theme-suggestion status during transition to the v2 contract.</summary>
    public CoursewareCapabilityStatus ThemeSuggestion { get; set; } = CoursewareCapabilityStatus.NotRequested;

    /// <summary>Gets the SlideML template validation status.</summary>
    public CoursewareCapabilityStatus TemplateValidation { get; set; } = CoursewareCapabilityStatus.NotRequested;

    /// <summary>Gets the image-backed visual-analysis status.</summary>
    public CoursewareCapabilityStatus VisualAnalysis { get; set; } = CoursewareCapabilityStatus.NotRequested;

    /// <summary>Gets the real page-generation status.</summary>
    public CoursewareCapabilityStatus PageGeneration { get; set; } = CoursewareCapabilityStatus.NotRequested;
}
