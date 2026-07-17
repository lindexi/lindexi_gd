namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the complete, path-free input supplied to whole-courseware analysis.
/// </summary>
public sealed record CoursewareAnalysisInput
{
    /// <summary>Gets the formatted model input.</summary>
    public required string Prompt { get; init; }

    /// <summary>Gets the total number of slides in the loaded courseware.</summary>
    public required int TotalSlideCount { get; init; }

    /// <summary>Gets the number of slides represented by the input.</summary>
    public required int AnalyzedSlideCount { get; init; }

    /// <summary>Gets the exact input character count.</summary>
    public required int CharacterCount { get; init; }

    /// <summary>Gets the conservatively estimated input token count.</summary>
    public required int EstimatedTokenCount { get; init; }

    /// <summary>Gets a value indicating whether any input content was truncated.</summary>
    public required bool WasTruncated { get; init; }

    /// <summary>Gets character counts for the stable prompt sections.</summary>
    public required CoursewareAnalysisInputSectionCharacterCounts SectionCharacterCounts { get; init; }

    /// <summary>Gets the deterministic statistics version, if statistics were included.</summary>
    public string? StatisticsVersion { get; init; }

    /// <summary>Gets the deterministic fingerprint of the complete analysis input.</summary>
    public required string InputFingerprint { get; init; }

    /// <summary>Gets warnings produced while creating the complete input.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>
/// Records the character count of each stable analysis prompt section.
/// </summary>
public sealed record CoursewareAnalysisInputSectionCharacterCounts
{
    /// <summary>Gets the task section character count.</summary>
    public required int Task { get; init; }

    /// <summary>Gets the courseware overview section character count.</summary>
    public required int CoursewareOverview { get; init; }

    /// <summary>Gets the resource catalog section character count.</summary>
    public required int ResourceCatalog { get; init; }

    /// <summary>Gets the slides section character count.</summary>
    public required int Slides { get; init; }

    /// <summary>Gets the output requirements section character count.</summary>
    public required int OutputRequirements { get; init; }
}
