namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Represents the most frequently used fonts, font sizes, and colors in courseware metadata.
/// </summary>
public sealed record CoursewareStyleUsageSummary
{
    /// <summary>Gets font usage ordered by frequency.</summary>
    public IReadOnlyList<CoursewareStyleUsageItem> Fonts { get; init; } = [];

    /// <summary>Gets font-size usage ordered by frequency.</summary>
    public IReadOnlyList<CoursewareStyleUsageItem> FontSizes { get; init; } = [];

    /// <summary>Gets color usage ordered by frequency.</summary>
    public IReadOnlyList<CoursewareStyleUsageItem> Colors { get; init; } = [];
}

/// <summary>
/// Represents one normalized style value and its usage count.
/// </summary>
/// <param name="Value">The normalized style value.</param>
/// <param name="Count">The number of occurrences.</param>
public sealed record CoursewareStyleUsageItem(string Value, int Count);
