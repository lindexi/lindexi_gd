namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Defines one reusable component specification.
/// </summary>
public sealed record CoursewareComponentSpecification
{
    /// <summary>Gets the stable component identifier.</summary>
    public string ComponentId { get; set; } = string.Empty;

    /// <summary>Gets the component name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the component purpose.</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Gets referenced design-token identifiers.</summary>
    public IReadOnlyList<string> TokenIds { get; set; } = [];

    /// <summary>Gets constraints applied to every component instance.</summary>
    public IReadOnlyList<string> Constraints { get; set; } = [];
}

/// <summary>
/// Describes one known resource's permitted role.
/// </summary>
public sealed record CoursewareAssetUsageRule
{
    /// <summary>Gets the logical resource identifier.</summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>Gets confirmed uses.</summary>
    public IReadOnlyList<string> ConfirmedUses { get; set; } = [];

    /// <summary>Gets candidate uses that require further evidence.</summary>
    public IReadOnlyList<string> CandidateUses { get; set; } = [];

    /// <summary>Gets prohibited uses.</summary>
    public IReadOnlyList<string> ProhibitedUses { get; set; } = [];

    /// <summary>Gets crop and scale rules.</summary>
    public IReadOnlyList<string> TransformRules { get; set; } = [];
}

/// <summary>
/// Defines courseware-level asset usage rules.
/// </summary>
public sealed record CoursewareAssetUsagePolicy
{
    /// <summary>Gets per-resource usage rules.</summary>
    public IReadOnlyList<CoursewareAssetUsageRule> ResourceRules { get; set; } = [];

    /// <summary>Gets rules applied to resources without an explicit entry.</summary>
    public IReadOnlyList<string> DefaultRules { get; set; } = [];
}

/// <summary>
/// Defines accessibility quality gates.
/// </summary>
public sealed record CoursewareAccessibilityRules
{
    /// <summary>Gets the minimum body font size.</summary>
    public double MinimumBodyFontSize { get; set; }

    /// <summary>Gets the minimum normal-text contrast ratio.</summary>
    public double MinimumNormalTextContrastRatio { get; set; }

    /// <summary>Gets the minimum large-text contrast ratio.</summary>
    public double MinimumLargeTextContrastRatio { get; set; }

    /// <summary>Gets additional accessibility constraints.</summary>
    public IReadOnlyList<string> Rules { get; set; } = [];
}

/// <summary>
/// Defines cross-slide consistency rules.
/// </summary>
public sealed record CoursewareConsistencyRules
{
    /// <summary>Gets properties that must remain invariant across slides.</summary>
    public IReadOnlyList<string> Invariants { get; set; } = [];

    /// <summary>Gets properties that page types may vary.</summary>
    public IReadOnlyList<string> AllowedVariations { get; set; } = [];
}
