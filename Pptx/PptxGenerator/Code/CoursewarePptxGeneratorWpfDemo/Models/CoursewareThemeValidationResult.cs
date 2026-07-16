namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents deterministic validation results for a courseware theme.
/// </summary>
public sealed record CoursewareThemeValidationResult
{
    /// <summary>Gets a value indicating whether the theme is valid.</summary>
    public bool IsValid => Errors.Count == 0;
    /// <summary>Gets field-level validation errors.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
}