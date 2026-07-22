namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Identifies the severity of one deterministic validation diagnostic.
/// </summary>
public enum CoursewareValidationSeverity
{
    /// <summary>The diagnostic does not block the capability.</summary>
    Warning,

    /// <summary>The diagnostic blocks the capability from passing.</summary>
    Error,
}

/// <summary>
/// Represents one field-addressable validation diagnostic.
/// </summary>
public sealed record CoursewareValidationDiagnostic
{
    /// <summary>Gets the stable diagnostic code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets the related object or property path.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Gets the privacy-safe diagnostic message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets the diagnostic severity.</summary>
    public CoursewareValidationSeverity Severity { get; set; }
}

/// <summary>
/// Records the deterministic validation result for one frozen design system.
/// </summary>
public sealed record CoursewareDesignSystemValidationReport
{
    /// <summary>Gets whether every blocking validation gate passed.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets the number of input slides covered by exactly one page type.</summary>
    public int CoveredSlideCount { get; set; }

    /// <summary>Gets the total number of input slides expected by the validator.</summary>
    public int TotalSlideCount { get; set; }

    /// <summary>Gets unresolved design-token references.</summary>
    public IReadOnlyList<string> UnresolvedTokenIds { get; set; } = [];

    /// <summary>Gets unresolved component references.</summary>
    public IReadOnlyList<string> UnresolvedComponentIds { get; set; } = [];

    /// <summary>Gets unresolved logical resource references.</summary>
    public IReadOnlyList<string> UnresolvedResourceIds { get; set; } = [];

    /// <summary>Gets input slides without one primary page-type assignment.</summary>
    public IReadOnlyList<string> UncoveredSlideIds { get; set; } = [];

    /// <summary>Gets all validation diagnostics.</summary>
    public IReadOnlyList<CoursewareValidationDiagnostic> Diagnostics { get; set; } = [];
}

/// <summary>
/// Represents deterministic structured facts extracted from one slide Markdown source.
/// </summary>
public sealed record CoursewareSlideStructuredFacts
{
    /// <summary>Gets the input slide identifier.</summary>
    public string SlideId { get; set; } = string.Empty;

    /// <summary>Gets the one-based page number.</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets the slide width.</summary>
    public double Width { get; set; }

    /// <summary>Gets the slide height.</summary>
    public double Height { get; set; }

    /// <summary>Gets the total parsed element count.</summary>
    public int ElementCount { get; set; }

    /// <summary>Gets the parsed text-element count.</summary>
    public int TextElementCount { get; set; }

    /// <summary>Gets the parsed image-element count.</summary>
    public int ImageElementCount { get; set; }

    /// <summary>Gets the total text character count.</summary>
    public int TextCharacterCount { get; set; }

    /// <summary>Gets the normalized occupied area estimate from zero to one.</summary>
    public double OccupiedAreaRatio { get; set; }

    /// <summary>Gets the normalized content bounding box.</summary>
    public CoursewareNormalizedRectangle? ContentBounds { get; set; }

    /// <summary>Gets the deterministic layout signature.</summary>
    public string LayoutSignature { get; set; } = string.Empty;

    /// <summary>Gets the heuristic density classification.</summary>
    public string DensityClass { get; set; } = string.Empty;

    /// <summary>Gets the layout-cluster identifier.</summary>
    public string LayoutClusterId { get; set; } = string.Empty;

    /// <summary>Gets parsed font usage counts.</summary>
    public IReadOnlyList<CoursewareFactDistributionItem> Fonts { get; set; } = [];

    /// <summary>Gets parsed font-size usage counts.</summary>
    public IReadOnlyList<CoursewareNumericFactDistributionItem> FontSizes { get; set; } = [];

    /// <summary>Gets parsed hexadecimal color usage counts.</summary>
    public IReadOnlyList<CoursewareFactDistributionItem> Colors { get; set; } = [];

    /// <summary>Gets logical resource identifiers referenced by this slide.</summary>
    public IReadOnlyList<string> ResourceIds { get; set; } = [];

    /// <summary>Gets deterministic and heuristic risk codes.</summary>
    public IReadOnlyList<string> RiskCodes { get; set; } = [];

    /// <summary>Gets privacy-safe parse diagnostics.</summary>
    public IReadOnlyList<string> Diagnostics { get; set; } = [];
}

/// <summary>
/// Represents a rectangle normalized to the slide canvas.
/// </summary>
public sealed record CoursewareNormalizedRectangle
{
    /// <summary>Gets the normalized left coordinate.</summary>
    public double X { get; set; }

    /// <summary>Gets the normalized top coordinate.</summary>
    public double Y { get; set; }

    /// <summary>Gets the normalized width.</summary>
    public double Width { get; set; }

    /// <summary>Gets the normalized height.</summary>
    public double Height { get; set; }
}

/// <summary>
/// Represents one string-valued deterministic fact distribution entry.
/// </summary>
public sealed record CoursewareFactDistributionItem
{
    /// <summary>Gets the distribution value.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets the occurrence count.</summary>
    public int Count { get; set; }
}

/// <summary>
/// Represents one numeric deterministic fact distribution entry.
/// </summary>
public sealed record CoursewareNumericFactDistributionItem
{
    /// <summary>Gets the numeric distribution value.</summary>
    public double Value { get; set; }

    /// <summary>Gets the occurrence count.</summary>
    public int Count { get; set; }
}

/// <summary>
/// Represents the complete deterministic structured-fact report for one courseware.
/// </summary>
public sealed record CoursewareStructuredFactReport
{
    /// <summary>The structured-fact algorithm version.</summary>
    public const string CurrentVersion = "courseware-structured-facts/v1";

    /// <summary>Gets the fact algorithm version.</summary>
    public string Version { get; set; } = CurrentVersion;

    /// <summary>Gets the analysis-view fingerprint from which the facts were extracted.</summary>
    public string InputFingerprint { get; set; } = string.Empty;

    /// <summary>Gets facts in stable slide order.</summary>
    public IReadOnlyList<CoursewareSlideStructuredFacts> Slides { get; set; } = [];

    /// <summary>Gets global font usage.</summary>
    public IReadOnlyList<CoursewareFactDistributionItem> Fonts { get; set; } = [];

    /// <summary>Gets global font-size usage.</summary>
    public IReadOnlyList<CoursewareNumericFactDistributionItem> FontSizes { get; set; } = [];

    /// <summary>Gets global color usage.</summary>
    public IReadOnlyList<CoursewareFactDistributionItem> Colors { get; set; } = [];

    /// <summary>Gets global logical-resource reference counts.</summary>
    public IReadOnlyList<CoursewareFactDistributionItem> ResourceReferences { get; set; } = [];

    /// <summary>Gets layout clusters and their representative slides.</summary>
    public IReadOnlyList<CoursewareLayoutCluster> LayoutClusters { get; set; } = [];

    /// <summary>Gets privacy-safe extraction diagnostics.</summary>
    public IReadOnlyList<string> Diagnostics { get; set; } = [];
}

/// <summary>
/// Represents one deterministic layout cluster.
/// </summary>
public sealed record CoursewareLayoutCluster
{
    /// <summary>Gets the stable cluster identifier.</summary>
    public string ClusterId { get; set; } = string.Empty;

    /// <summary>Gets the shared layout signature.</summary>
    public string LayoutSignature { get; set; } = string.Empty;

    /// <summary>Gets all slide identifiers assigned to the cluster.</summary>
    public IReadOnlyList<string> SlideIds { get; set; } = [];

    /// <summary>Gets the representative slide identifier.</summary>
    public string RepresentativeSlideId { get; set; } = string.Empty;
}

/// <summary>
/// Identifies why one screenshot was selected for visual analysis.
/// </summary>
public enum CoursewareVisualSampleRole
{
    /// <summary>The first available courseware screenshot.</summary>
    Overview,

    /// <summary>A representative of one deterministic layout cluster.</summary>
    LayoutRepresentative,

    /// <summary>A high-density or geometric outlier.</summary>
    Outlier,

    /// <summary>A page with significant logical asset usage.</summary>
    AssetUsage,

    /// <summary>The first or last courseware page.</summary>
    Boundary,
}

/// <summary>
/// Describes one path-free screenshot sample selected for visual analysis.
/// </summary>
public sealed record CoursewareVisualSample
{
    /// <summary>Gets the related slide identifier.</summary>
    public string SlideId { get; set; } = string.Empty;

    /// <summary>Gets the one-based page number.</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets the sample role.</summary>
    public CoursewareVisualSampleRole Role { get; set; }

    /// <summary>Gets the deterministic selection reason.</summary>
    public string SelectionReason { get; set; } = string.Empty;

    /// <summary>Gets the visual question assigned to the model.</summary>
    public string Question { get; set; } = string.Empty;
}

/// <summary>
/// Records one image-backed visual observation.
/// </summary>
public sealed record CoursewareVisualObservation
{
    /// <summary>Gets the stable observation identifier.</summary>
    public string ObservationId { get; set; } = string.Empty;

    /// <summary>Gets the related slide identifier.</summary>
    public string SlideId { get; set; } = string.Empty;

    /// <summary>Gets the one-based page number.</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets the observation category.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets the observed visual fact.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets confidence from zero to one.</summary>
    public double Confidence { get; set; }

    /// <summary>Gets the sample role that supports the observation.</summary>
    public CoursewareVisualSampleRole SampleRole { get; set; }
}

/// <summary>
/// Records the requested and executed visual-analysis evidence.
/// </summary>
public sealed record CoursewareVisualAnalysisReport
{
    /// <summary>Gets whether visual analysis was requested.</summary>
    public bool WasRequested { get; set; }

    /// <summary>Gets whether the selected model supported image attachments.</summary>
    public bool ModelSupportedImages { get; set; }

    /// <summary>Gets the selected screenshot samples.</summary>
    public IReadOnlyList<CoursewareVisualSample> Samples { get; set; } = [];

    /// <summary>Gets validated image-backed observations.</summary>
    public IReadOnlyList<CoursewareVisualObservation> Observations { get; set; } = [];

    /// <summary>Gets visual-analysis diagnostics.</summary>
    public IReadOnlyList<CoursewareValidationDiagnostic> Diagnostics { get; set; } = [];
}

/// <summary>
/// Records one compiled SlideML stress sample.
/// </summary>
public sealed record CoursewareTemplateStressSampleResult
{
    /// <summary>Gets the stress-sample identifier.</summary>
    public string SampleId { get; set; } = string.Empty;

    /// <summary>Gets the template identifier.</summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>Gets the canvas identifier.</summary>
    public string CanvasId { get; set; } = string.Empty;

    /// <summary>Gets the sample category.</summary>
    public string SampleKind { get; set; } = string.Empty;

    /// <summary>Gets whether compilation and rendering passed.</summary>
    public bool Passed { get; set; }

    /// <summary>Gets the compiled SlideML supplied to the parser.</summary>
    public string CompiledSlideMl { get; set; } = string.Empty;

    /// <summary>Gets validation diagnostics for this sample.</summary>
    public IReadOnlyList<CoursewareValidationDiagnostic> Diagnostics { get; set; } = [];
}

/// <summary>
/// Records static compilation and real-content stress-test results for page templates.
/// </summary>
public sealed record CoursewareTemplateValidationReport
{
    /// <summary>Gets whether every submitted template passed all required gates.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets the number of submitted templates.</summary>
    public int TemplateCount { get; set; }

    /// <summary>Gets the number of templates that passed all samples.</summary>
    public int PassedTemplateCount { get; set; }

    /// <summary>Gets all compiled stress-sample results.</summary>
    public IReadOnlyList<CoursewareTemplateStressSampleResult> Samples { get; set; } = [];

    /// <summary>Gets template-level diagnostics.</summary>
    public IReadOnlyList<CoursewareValidationDiagnostic> Diagnostics { get; set; } = [];
}
