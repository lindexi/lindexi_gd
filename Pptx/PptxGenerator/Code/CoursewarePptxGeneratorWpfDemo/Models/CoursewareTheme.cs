namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the validated visual theme shared by all slides in a courseware package.
/// </summary>
public sealed record CoursewareTheme
{
    /// <summary>
    /// Gets the current theme schema version.
    /// </summary>
    public const string CurrentSchemaVersion = "1.0";

    /// <summary>
    /// Gets the theme schema version.
    /// </summary>
    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets the user-facing theme title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the user-facing theme summary.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Gets concise style keywords.
    /// </summary>
    public IReadOnlyList<string> StyleKeywords { get; init; } = [];

    /// <summary>
    /// Gets the theme color scheme.
    /// </summary>
    public required CoursewareColorScheme Colors { get; init; }

    /// <summary>
    /// Gets the theme typography hierarchy.
    /// </summary>
    public required CoursewareTypography Typography { get; init; }

    /// <summary>
    /// Gets font recommendations.
    /// </summary>
    public required CoursewareFontRecommendation Fonts { get; init; }

    /// <summary>
    /// Gets the content safe area in slide coordinate units.
    /// </summary>
    public required CoursewareSafeArea SafeArea { get; init; }

    /// <summary>
    /// Gets shared layout principles.
    /// </summary>
    public IReadOnlyList<string> LayoutPrinciples { get; init; } = [];

    /// <summary>
    /// Gets layout recommendations for common page types.
    /// </summary>
    public required CoursewarePageTypeRecommendations PageTypes { get; init; }

    /// <summary>
    /// Gets rules for presenting subject content.
    /// </summary>
    public IReadOnlyList<string> ContentPresentationRules { get; init; } = [];

    /// <summary>
    /// Gets the compact theme instructions supplied to later slide generation.
    /// </summary>
    public required string GenerationPromptSummary { get; init; }
}

/// <summary>
/// Represents a semantic color used by a courseware theme.
/// </summary>
public sealed record CoursewareThemeColor
{
    /// <summary>
    /// Gets the color usage label.
    /// </summary>
    public required string Usage { get; init; }

    /// <summary>
    /// Gets the descriptive color name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the color value in hexadecimal notation.
    /// </summary>
    public required string HexValue { get; init; }
}

/// <summary>
/// Represents the complete courseware color scheme.
/// </summary>
public sealed record CoursewareColorScheme
{
    /// <summary>
    /// Gets the primary color.
    /// </summary>
    public required CoursewareThemeColor Primary { get; init; }

    /// <summary>
    /// Gets the accent color.
    /// </summary>
    public required CoursewareThemeColor Accent { get; init; }

    /// <summary>
    /// Gets the page background color.
    /// </summary>
    public required CoursewareThemeColor Background { get; init; }

    /// <summary>
    /// Gets the primary text color.
    /// </summary>
    public required CoursewareThemeColor PrimaryText { get; init; }

    /// <summary>
    /// Gets the secondary text color.
    /// </summary>
    public required CoursewareThemeColor SecondaryText { get; init; }

    /// <summary>
    /// Gets the rationale for the selected color scheme.
    /// </summary>
    public required string Rationale { get; init; }

    /// <summary>
    /// Enumerates colors in display order.
    /// </summary>
    /// <returns>The ordered theme colors.</returns>
    public IEnumerable<CoursewareThemeColor> EnumerateColors()
    {
        yield return Primary;
        yield return Accent;
        yield return Background;
        yield return PrimaryText;
        yield return SecondaryText;
    }
}

/// <summary>
/// Represents one typography level.
/// </summary>
public sealed record CoursewareTypographyLevel
{
    /// <summary>
    /// Gets the level name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the font size in slide coordinate units.
    /// </summary>
    public required double FontSize { get; init; }

    /// <summary>
    /// Gets the font weight name.
    /// </summary>
    public required string FontWeight { get; init; }

    /// <summary>
    /// Gets the intended purpose of this level.
    /// </summary>
    public required string Purpose { get; init; }
}

/// <summary>
/// Represents the complete typography hierarchy.
/// </summary>
public sealed record CoursewareTypography
{
    /// <summary>
    /// Gets the primary heading level.
    /// </summary>
    public required CoursewareTypographyLevel PrimaryHeading { get; init; }

    /// <summary>
    /// Gets the secondary heading level.
    /// </summary>
    public required CoursewareTypographyLevel SecondaryHeading { get; init; }

    /// <summary>
    /// Gets the body text level.
    /// </summary>
    public required CoursewareTypographyLevel Body { get; init; }

    /// <summary>
    /// Gets the supporting text level.
    /// </summary>
    public required CoursewareTypographyLevel Supporting { get; init; }

    /// <summary>
    /// Enumerates typography levels from strongest to weakest.
    /// </summary>
    /// <returns>The ordered typography levels.</returns>
    public IEnumerable<CoursewareTypographyLevel> EnumerateLevels()
    {
        yield return PrimaryHeading;
        yield return SecondaryHeading;
        yield return Body;
        yield return Supporting;
    }
}

/// <summary>
/// Represents recommended fonts for the theme.
/// </summary>
public sealed record CoursewareFontRecommendation
{
    /// <summary>
    /// Gets the recommended East Asian heading font.
    /// </summary>
    public required string EastAsianHeading { get; init; }

    /// <summary>
    /// Gets the recommended East Asian body font.
    /// </summary>
    public required string EastAsianBody { get; init; }

    /// <summary>
    /// Gets the recommended Latin heading font.
    /// </summary>
    public required string LatinHeading { get; init; }

    /// <summary>
    /// Gets the recommended Latin body font.
    /// </summary>
    public required string LatinBody { get; init; }
}

/// <summary>
/// Represents slide content safe-area margins.
/// </summary>
public sealed record CoursewareSafeArea
{
    /// <summary>
    /// Gets the left margin.
    /// </summary>
    public required double Left { get; init; }

    /// <summary>
    /// Gets the top margin.
    /// </summary>
    public required double Top { get; init; }

    /// <summary>
    /// Gets the right margin.
    /// </summary>
    public required double Right { get; init; }

    /// <summary>
    /// Gets the bottom margin.
    /// </summary>
    public required double Bottom { get; init; }
}

/// <summary>
/// Represents a layout recommendation for one page type.
/// </summary>
public sealed record CoursewarePageTypeRecommendation
{
    /// <summary>
    /// Gets the page type name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the layout recommendation.
    /// </summary>
    public required string Description { get; init; }
}

/// <summary>
/// Represents recommendations for common courseware page types.
/// </summary>
public sealed record CoursewarePageTypeRecommendations
{
    /// <summary>
    /// Gets the cover-page recommendation.
    /// </summary>
    public required CoursewarePageTypeRecommendation Cover { get; init; }

    /// <summary>
    /// Gets the section-page recommendation.
    /// </summary>
    public required CoursewarePageTypeRecommendation Section { get; init; }

    /// <summary>
    /// Gets the regular content-page recommendation.
    /// </summary>
    public required CoursewarePageTypeRecommendation Content { get; init; }

    /// <summary>
    /// Gets the ending-page recommendation.
    /// </summary>
    public required CoursewarePageTypeRecommendation Ending { get; init; }

    /// <summary>
    /// Enumerates recommendations in display order.
    /// </summary>
    /// <returns>The ordered recommendations.</returns>
    public IEnumerable<CoursewarePageTypeRecommendation> EnumerateRecommendations()
    {
        yield return Cover;
        yield return Section;
        yield return Content;
        yield return Ending;
    }
}