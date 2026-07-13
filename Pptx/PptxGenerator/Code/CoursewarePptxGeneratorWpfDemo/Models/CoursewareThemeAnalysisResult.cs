namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents one color and its intended role in a courseware theme.
/// </summary>
/// <param name="Name">The user-facing color name.</param>
/// <param name="HexValue">The hexadecimal color value.</param>
/// <param name="Usage">The recommended use of the color.</param>
public sealed record CoursewareThemeColor(string Name, string HexValue, string Usage);

/// <summary>
/// Represents one typography level in a courseware theme.
/// </summary>
/// <param name="Name">The hierarchy level name.</param>
/// <param name="FontFamily">The preferred font family.</param>
/// <param name="SizeInPoints">The recommended font size in points.</param>
/// <param name="Weight">The recommended font weight.</param>
public sealed record CoursewareTypographyLevel(string Name, string FontFamily, double SizeInPoints, string Weight);

/// <summary>
/// Represents the safe margins of a courseware slide.
/// </summary>
/// <param name="Left">The left safe margin.</param>
/// <param name="Top">The top safe margin.</param>
/// <param name="Right">The right safe margin.</param>
/// <param name="Bottom">The bottom safe margin.</param>
public sealed record CoursewareSafeArea(double Left, double Top, double Right, double Bottom);

/// <summary>
/// Represents the input coverage and risk summary of a theme analysis.
/// </summary>
/// <param name="SlideCount">The total number of loaded slides.</param>
/// <param name="MissingScreenshotCount">The number of slides without screenshots.</param>
/// <param name="MissingResourceCount">The number of missing resources.</param>
/// <param name="WarningCount">The total number of non-blocking warnings.</param>
public sealed record CoursewareAnalysisCoverage(
    int SlideCount,
    int MissingScreenshotCount,
    int MissingResourceCount,
    int WarningCount);

/// <summary>
/// Represents a validated global visual theme for a courseware package.
/// </summary>
public sealed class CoursewareThemeAnalysisResult
{
    /// <summary>
    /// Gets the concise theme title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the summary of the recommended visual direction.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Gets the short visual style keywords.
    /// </summary>
    public IReadOnlyList<string> StyleKeywords { get; init; } = [];

    /// <summary>
    /// Gets the core theme colors.
    /// </summary>
    public IReadOnlyList<CoursewareThemeColor> Colors { get; init; } = [];

    /// <summary>
    /// Gets the typography hierarchy.
    /// </summary>
    public IReadOnlyList<CoursewareTypographyLevel> Typography { get; init; } = [];

    /// <summary>
    /// Gets the concise layout principles.
    /// </summary>
    public IReadOnlyList<string> LayoutPrinciples { get; init; } = [];

    /// <summary>
    /// Gets the slide safe area.
    /// </summary>
    public required CoursewareSafeArea SafeArea { get; init; }

    /// <summary>
    /// Gets the recommendations for common page types.
    /// </summary>
    public IReadOnlyList<string> PageTypeRecommendations { get; init; } = [];

    /// <summary>
    /// Gets the input coverage and risk summary.
    /// </summary>
    public required CoursewareAnalysisCoverage Coverage { get; init; }
}
