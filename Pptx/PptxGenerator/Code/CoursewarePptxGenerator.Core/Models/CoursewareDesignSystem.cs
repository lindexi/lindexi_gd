namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Represents the immutable, versioned design system consumed by downstream page generation.
/// </summary>
public sealed record CoursewareDesignSystem
{
    /// <summary>The schema version emitted by this implementation.</summary>
    public const string CurrentSchemaVersion = "2.0";

    /// <summary>Gets the schema version.</summary>
    public string SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Gets the stable design-system identifier.</summary>
    public string DesignSystemId { get; set; } = string.Empty;

    /// <summary>Gets the design intent.</summary>
    public CoursewareDesignIntent DesignIntent { get; set; } = new();

    /// <summary>Gets supported canvas profiles.</summary>
    public IReadOnlyList<CoursewareCanvasDesignProfile> CanvasProfiles { get; set; } = [];

    /// <summary>Gets the grid system.</summary>
    public CoursewareGridSystem Grid { get; set; } = new();

    /// <summary>Gets the spacing system.</summary>
    public CoursewareSpacingScale Spacing { get; set; } = new();

    /// <summary>Gets the typography system.</summary>
    public CoursewareTypographySystem Typography { get; set; } = new();

    /// <summary>Gets the color system.</summary>
    public CoursewareColorSystem Colors { get; set; } = new();

    /// <summary>Gets the effect system.</summary>
    public CoursewareEffectSystem Effects { get; set; } = new();

    /// <summary>Gets reusable component specifications.</summary>
    public IReadOnlyList<CoursewareComponentSpecification> Components { get; set; } = [];

    /// <summary>Gets asset usage rules.</summary>
    public CoursewareAssetUsagePolicy AssetPolicy { get; set; } = new();

    /// <summary>Gets dynamic page-type contracts.</summary>
    public IReadOnlyList<CoursewarePageTypeContract> PageTypes { get; set; } = [];

    /// <summary>Gets complete input-slide coverage assignments.</summary>
    public IReadOnlyList<CoursewarePageTypeAssignment> PageTypeAssignments { get; set; } = [];

    /// <summary>Gets compiled page templates.</summary>
    public IReadOnlyList<CoursewarePageTemplate> PageTemplates { get; set; } = [];

    /// <summary>Gets accessibility quality gates.</summary>
    public CoursewareAccessibilityRules Accessibility { get; set; } = new();

    /// <summary>Gets cross-slide consistency rules.</summary>
    public CoursewareConsistencyRules Consistency { get; set; } = new();

    /// <summary>Gets decision provenance.</summary>
    public IReadOnlyList<CoursewareDesignDecisionEvidence> Evidence { get; set; } = [];

    /// <summary>Gets unresolved assumptions.</summary>
    public IReadOnlyList<CoursewareDesignAssumption> Assumptions { get; set; } = [];
}

/// <summary>
/// Represents the versioned result of whole-courseware design analysis.
/// </summary>
public sealed record CoursewareDesignAnalysisResult
{
    /// <summary>The result schema version emitted by this implementation.</summary>
    public const string CurrentSchemaVersion = "2.0";

    /// <summary>Gets the result schema version.</summary>
    public string SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Gets the validated design system.</summary>
    public CoursewareDesignSystem DesignSystem { get; set; } = new();

    /// <summary>Gets independently verifiable capability states.</summary>
    public CoursewareAnalysisCapabilityStates CapabilityStates { get; set; } = new();

    /// <summary>Gets the completion timestamp.</summary>
    public DateTimeOffset AnalyzedAt { get; set; }

    /// <summary>Gets the total input slide count.</summary>
    public int TotalSlideCount { get; set; }

    /// <summary>Gets the number of slides included in the analysis.</summary>
    public int AnalyzedSlideCount { get; set; }

    /// <summary>Gets the deterministic analysis-input fingerprint.</summary>
    public string InputFingerprint { get; set; } = string.Empty;

    /// <summary>Gets non-blocking warnings.</summary>
    public IReadOnlyList<string> Warnings { get; set; } = [];
}
