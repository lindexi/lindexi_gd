namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the lightweight visual theme shared by all slides in a courseware package.
/// </summary>
public sealed record CoursewareTheme
{
    /// <summary>
    /// Gets the current theme schema version.
    /// </summary>
    public const string CurrentSchemaVersion = "2.1";

    /// <summary>
    /// Gets the theme schema version.
    /// </summary>
    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets the suggested colors and their intended uses.
    /// </summary>
    public IReadOnlyList<CoursewareColorSuggestion> ColorSuggestions { get; init; } = [];

    /// <summary>
    /// Gets the recommended Chinese and Western fonts.
    /// </summary>
    public required CoursewareFontSuggestions Fonts { get; init; }

    /// <summary>
    /// Gets the original font-size hierarchy and usage rules produced by theme analysis.
    /// </summary>
    public required string FontSizeRules { get; init; }

    /// <summary>
    /// Gets the visual style description.
    /// </summary>
    public required string Style { get; init; }

    /// <summary>
    /// Gets the content safe-area ratios.
    /// </summary>
    public required CoursewareSafeAreaRatios SafeArea { get; init; }

    /// <summary>
    /// Gets the spacing and visual-effects guidance.
    /// </summary>
    public required string SpacingAndVisualEffects { get; init; }

    /// <summary>
    /// Gets the layout principles.
    /// </summary>
    public required string LayoutPrinciples { get; init; }

    /// <summary>
    /// Gets the reference SlideML for a cover page.
    /// </summary>
    public required string CoverPageSlideMl { get; init; }

    /// <summary>
    /// Gets the reference SlideML for a content page.
    /// </summary>
    public required string ContentPageSlideMl { get; init; }
}

/// <summary>
/// Represents one suggested theme color.
/// </summary>
public sealed record CoursewareColorSuggestion
{
    /// <summary>
    /// Gets the semantic color name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the intended usage.
    /// </summary>
    public required string Usage { get; init; }

    /// <summary>
    /// Gets the color value in hexadecimal notation.
    /// </summary>
    public required string Hex { get; init; }
}

/// <summary>
/// Represents recommended Chinese and Western fonts.
/// </summary>
public sealed record CoursewareFontSuggestions
{
    /// <summary>
    /// Gets the Chinese font recommendation.
    /// </summary>
    public required string Chinese { get; init; }

    /// <summary>
    /// Gets the Western font recommendation.
    /// </summary>
    public required string Western { get; init; }
}

/// <summary>
/// Represents safe-area ratios relative to the slide dimensions.
/// </summary>
public sealed record CoursewareSafeAreaRatios
{
    /// <summary>Gets the left ratio.</summary>
    public required double LeftRatio { get; init; }

    /// <summary>Gets the top ratio.</summary>
    public required double TopRatio { get; init; }

    /// <summary>Gets the right ratio.</summary>
    public required double RightRatio { get; init; }

    /// <summary>Gets the bottom ratio.</summary>
    public required double BottomRatio { get; init; }
}