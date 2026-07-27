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
}