namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Identifies the language-model workload requested by the application.
/// </summary>
public enum AgentWorkload
{
    /// <summary>
    /// Whole-courseware theme analysis.
    /// </summary>
    ThemeAnalysis,

    /// <summary>
    /// Single-slide generation.
    /// </summary>
    SlideGeneration,

    /// <summary>
    /// Slide and prompt evaluation.
    /// </summary>
    Evaluation,
}