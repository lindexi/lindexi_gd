namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the bounded, path-free input supplied to whole-courseware analysis.
/// </summary>
public sealed record CoursewareAnalysisInput
{
    /// <summary>Gets the formatted model input.</summary>
    public required string Prompt { get; init; }
    /// <summary>Gets the number of slides represented by the input.</summary>
    public required int AnalyzedSlideCount { get; init; }
    /// <summary>Gets the approximate input character count.</summary>
    public required int CharacterCount { get; init; }
    /// <summary>Gets warnings produced while creating the bounded input.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}