namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the structured theme context supplied to one slide-generation request.
/// </summary>
public sealed record CoursewarePageDesignContext
{
    /// <summary>
    /// Gets the current design capability represented by this context.
    /// </summary>
    public string Capability { get; init; } = "ThemeSuggestion";

    /// <summary>
    /// Gets the theme title.
    /// </summary>
    public required string ThemeTitle { get; init; }

    /// <summary>
    /// Gets the theme summary.
    /// </summary>
    public required string ThemeSummary { get; init; }

    /// <summary>
    /// Gets concise style keywords.
    /// </summary>
    public IReadOnlyList<string> StyleKeywords { get; init; } = [];

    /// <summary>
    /// Gets the page color scheme.
    /// </summary>
    public required CoursewareColorScheme Colors { get; init; }

    /// <summary>
    /// Gets the typography hierarchy scaled for the current slide canvas.
    /// </summary>
    public required CoursewareTypography Typography { get; init; }

    /// <summary>
    /// Gets the recommended fonts.
    /// </summary>
    public required CoursewareFontRecommendation Fonts { get; init; }

    /// <summary>
    /// Gets the safe area scaled for the current slide canvas.
    /// </summary>
    public required CoursewareSafeArea SafeArea { get; init; }

    /// <summary>
    /// Gets shared layout principles.
    /// </summary>
    public IReadOnlyList<string> LayoutPrinciples { get; init; } = [];

    /// <summary>
    /// Gets page-type recommendations.
    /// </summary>
    public required CoursewarePageTypeRecommendations PageTypes { get; init; }

    /// <summary>
    /// Gets rules for presenting subject content.
    /// </summary>
    public IReadOnlyList<string> ContentPresentationRules { get; init; } = [];

    /// <summary>
    /// Gets compact generation guidance supplied by theme analysis.
    /// </summary>
    public required string GenerationPromptSummary { get; init; }

    /// <summary>
    /// Gets the reference canvas width used by whole-courseware theme analysis.
    /// </summary>
    public required int ReferenceCanvasWidth { get; init; }

    /// <summary>
    /// Gets the reference canvas height used by whole-courseware theme analysis.
    /// </summary>
    public required int ReferenceCanvasHeight { get; init; }

    /// <summary>
    /// Gets the current slide canvas width.
    /// </summary>
    public required int CanvasWidth { get; init; }

    /// <summary>
    /// Gets the current slide canvas height.
    /// </summary>
    public required int CanvasHeight { get; init; }

    /// <summary>
    /// Gets deterministic diagnostics produced while adapting the theme to this canvas.
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
