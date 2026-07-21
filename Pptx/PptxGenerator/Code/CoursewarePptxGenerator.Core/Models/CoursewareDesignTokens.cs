namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Describes the user-visible design direction for a courseware.
/// </summary>
public sealed record CoursewareDesignIntent
{
    /// <summary>Gets the design-system name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the concise design-system summary.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets the style keywords.</summary>
    public IReadOnlyList<string> StyleKeywords { get; set; } = [];

    /// <summary>Gets the inferred or user-provided subject.</summary>
    public string? Subject { get; set; }

    /// <summary>Gets the inferred or user-provided audience.</summary>
    public string? Audience { get; set; }

    /// <summary>Gets the inferred or user-provided usage scenario.</summary>
    public string? UsageScenario { get; set; }

    /// <summary>Gets the rationale for the selected direction.</summary>
    public string Rationale { get; set; } = string.Empty;
}

/// <summary>
/// Describes one supported slide canvas.
/// </summary>
public sealed record CoursewareCanvasDesignProfile
{
    /// <summary>Gets the stable canvas identifier.</summary>
    public string CanvasId { get; set; } = string.Empty;

    /// <summary>Gets the canvas width.</summary>
    public double Width { get; set; }

    /// <summary>Gets the canvas height.</summary>
    public double Height { get; set; }

    /// <summary>Gets whether this is the primary canvas.</summary>
    public bool IsPrimary { get; set; }
}

/// <summary>
/// Defines safe-area and grid behavior.
/// </summary>
public sealed record CoursewareGridSystem
{
    /// <summary>Gets the left safe-area inset.</summary>
    public double SafeLeft { get; set; }

    /// <summary>Gets the top safe-area inset.</summary>
    public double SafeTop { get; set; }

    /// <summary>Gets the right safe-area inset.</summary>
    public double SafeRight { get; set; }

    /// <summary>Gets the bottom safe-area inset.</summary>
    public double SafeBottom { get; set; }

    /// <summary>Gets the column count.</summary>
    public int ColumnCount { get; set; }

    /// <summary>Gets the gutter width.</summary>
    public double Gutter { get; set; }

    /// <summary>Gets the baseline-grid increment.</summary>
    public double Baseline { get; set; }
}

/// <summary>
/// Defines one semantic spacing token.
/// </summary>
public sealed record CoursewareSpacingToken
{
    /// <summary>Gets the stable token identifier.</summary>
    public string TokenId { get; set; } = string.Empty;

    /// <summary>Gets the resolved spacing value.</summary>
    public double Value { get; set; }

    /// <summary>Gets the intended use.</summary>
    public string Purpose { get; set; } = string.Empty;
}

/// <summary>
/// Defines the spacing scale.
/// </summary>
public sealed record CoursewareSpacingScale
{
    /// <summary>Gets the spacing tokens.</summary>
    public IReadOnlyList<CoursewareSpacingToken> Tokens { get; set; } = [];
}

/// <summary>
/// Defines one semantic typography token.
/// </summary>
public sealed record CoursewareTypographyToken
{
    /// <summary>Gets the stable token identifier.</summary>
    public string TokenId { get; set; } = string.Empty;

    /// <summary>Gets the East Asian font stack.</summary>
    public IReadOnlyList<string> EastAsianFontStack { get; set; } = [];

    /// <summary>Gets the Latin font stack.</summary>
    public IReadOnlyList<string> LatinFontStack { get; set; } = [];

    /// <summary>Gets the resolved font size.</summary>
    public double FontSize { get; set; }

    /// <summary>Gets the font weight.</summary>
    public string FontWeight { get; set; } = string.Empty;

    /// <summary>Gets the line-height multiplier.</summary>
    public double LineHeight { get; set; }

    /// <summary>Gets the intended use.</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Gets page types allowed to use the token.</summary>
    public IReadOnlyList<string> AllowedPageTypeIds { get; set; } = [];
}

/// <summary>
/// Defines the typography system.
/// </summary>
public sealed record CoursewareTypographySystem
{
    /// <summary>Gets the typography tokens.</summary>
    public IReadOnlyList<CoursewareTypographyToken> Tokens { get; set; } = [];
}

/// <summary>
/// Defines one semantic color token.
/// </summary>
public sealed record CoursewareColorToken
{
    /// <summary>Gets the stable token identifier.</summary>
    public string TokenId { get; set; } = string.Empty;

    /// <summary>Gets the resolved hexadecimal color.</summary>
    public string HexValue { get; set; } = string.Empty;

    /// <summary>Gets the intended use.</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Gets prohibited uses.</summary>
    public IReadOnlyList<string> ProhibitedUses { get; set; } = [];

    /// <summary>Gets background token identifiers allowed with this color.</summary>
    public IReadOnlyList<string> AllowedBackgroundTokenIds { get; set; } = [];

    /// <summary>Gets the decision provenance.</summary>
    public CoursewareEvidenceSourceKind SourceKind { get; set; }
}

/// <summary>
/// Defines the semantic color system.
/// </summary>
public sealed record CoursewareColorSystem
{
    /// <summary>Gets the color tokens.</summary>
    public IReadOnlyList<CoursewareColorToken> Tokens { get; set; } = [];
}

/// <summary>
/// Defines one reusable visual-effect token.
/// </summary>
public sealed record CoursewareEffectToken
{
    /// <summary>Gets the stable token identifier.</summary>
    public string TokenId { get; set; } = string.Empty;

    /// <summary>Gets the effect kind.</summary>
    public string EffectKind { get; set; } = string.Empty;

    /// <summary>Gets the serialized effect value understood by the compiler.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets the intended use.</summary>
    public string Purpose { get; set; } = string.Empty;
}

/// <summary>
/// Defines reusable effect tokens.
/// </summary>
public sealed record CoursewareEffectSystem
{
    /// <summary>Gets the effect tokens.</summary>
    public IReadOnlyList<CoursewareEffectToken> Tokens { get; set; } = [];
}
