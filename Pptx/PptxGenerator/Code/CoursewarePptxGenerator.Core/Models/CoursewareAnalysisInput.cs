namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Identifies the product policy applied when local paths are detected.
/// </summary>
public enum CoursewarePathPrivacyMode
{
    /// <summary>Replace paths in the model-facing view and emit audit diagnostics.</summary>
    Redact,

    /// <summary>Reject the request before model transmission.</summary>
    Block,
}

/// <summary>
/// Identifies how a detected local path was handled before model transmission.
/// </summary>
public enum CoursewarePathPrivacyAction
{
    /// <summary>The path was replaced in the model-facing analysis view.</summary>
    Redacted,

    /// <summary>The request was rejected before model transmission.</summary>
    Blocked,
}

/// <summary>
/// Represents a path-privacy policy violation detected before model transmission.
/// </summary>
public sealed class CoursewarePathPrivacyException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new path-privacy exception without retaining the sensitive path value.
    /// </summary>
    internal CoursewarePathPrivacyException(
        string message,
        CoursewarePathPrivacyDiagnostic diagnostic)
        : base(message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("异常消息不能为空。", nameof(message));
        }

        ArgumentNullException.ThrowIfNull(diagnostic);
        Diagnostic = diagnostic;
    }

    /// <summary>Gets the privacy-safe diagnostic describing the blocked value.</summary>
    public CoursewarePathPrivacyDiagnostic Diagnostic { get; }
}

/// <summary>
/// Records one path-privacy transformation without retaining the sensitive path value.
/// </summary>
public sealed class CoursewarePathPrivacyDiagnostic
{
    internal CoursewarePathPrivacyDiagnostic(
        string diagnosticId,
        string section,
        string? slideId,
        int? pageNumber,
        CoursewarePathPrivacyAction action,
        string originalValueFingerprint)
    {
        DiagnosticId = diagnosticId;
        Section = section;
        SlideId = slideId;
        PageNumber = pageNumber;
        Action = action;
        OriginalValueFingerprint = originalValueFingerprint;
    }

    /// <summary>Gets the stable diagnostic identifier.</summary>
    public string DiagnosticId { get; }

    /// <summary>Gets the section containing the path.</summary>
    public string Section { get; }

    /// <summary>Gets the related slide identifier, when applicable.</summary>
    public string? SlideId { get; }

    /// <summary>Gets the one-based page number, when applicable.</summary>
    public int? PageNumber { get; }

    /// <summary>Gets the privacy action.</summary>
    public CoursewarePathPrivacyAction Action { get; }

    /// <summary>Gets a hash of the original path for audit correlation.</summary>
    public string OriginalValueFingerprint { get; }
}

/// <summary>
/// Defines the trusted task instructions in the analysis envelope.
/// </summary>
public sealed record CoursewareAnalysisTaskSection
{
    /// <summary>Gets or sets the analysis objective.</summary>
    public string Objective { get; set; } = string.Empty;

    /// <summary>Gets or sets mandatory analysis requirements.</summary>
    public IReadOnlyList<string> Requirements { get; set; } = [];

    /// <summary>Gets or sets the untrusted-data boundary statement.</summary>
    public string DataBoundary { get; set; } = string.Empty;

    /// <summary>Gets or sets the available visual-evidence boundary.</summary>
    public string VisualEvidenceBoundary { get; set; } = string.Empty;
}

/// <summary>
/// Represents one deterministic count in the courseware overview.
/// </summary>
public sealed record CoursewareAnalysisDistributionItem
{
    /// <summary>Gets or sets the distribution key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the occurrence count.</summary>
    public int Count { get; set; }
}

/// <summary>
/// Represents one path-free warning supplied to the model.
/// </summary>
public sealed record CoursewareAnalysisWarningView
{
    /// <summary>Gets or sets the warning code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the warning message.</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Represents the structured courseware overview supplied to the model.
/// </summary>
public sealed record CoursewareAnalysisOverviewSection
{
    /// <summary>Gets or sets the courseware name.</summary>
    public string CoursewareName { get; set; } = string.Empty;

    /// <summary>Gets or sets the total slide count.</summary>
    public int TotalSlideCount { get; set; }

    /// <summary>Gets or sets the loaded Markdown slide count.</summary>
    public int LoadedMarkdownSlideCount { get; set; }

    /// <summary>Gets or sets the number of locally available screenshots.</summary>
    public int AvailableScreenshotCount { get; set; }

    /// <summary>Gets or sets the slide-dimension distribution.</summary>
    public IReadOnlyList<CoursewareAnalysisDistributionItem> DimensionDistribution { get; set; } = [];

    /// <summary>Gets or sets the resource-type distribution.</summary>
    public IReadOnlyList<CoursewareAnalysisDistributionItem> ResourceTypeDistribution { get; set; } = [];

    /// <summary>Gets or sets the deterministic statistics version.</summary>
    public string StatisticsVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the original logical source fingerprint.</summary>
    public string SourceFingerprint { get; set; } = string.Empty;

    /// <summary>Gets or sets path-free load warnings.</summary>
    public IReadOnlyList<CoursewareAnalysisWarningView> Warnings { get; set; } = [];
}

/// <summary>
/// Represents one logical resource in the model-facing envelope.
/// </summary>
public sealed record CoursewareAnalysisResourceView
{
    /// <summary>Gets or sets the logical resource identifier.</summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the resource type.</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the local resource file exists.</summary>
    public bool Exists { get; set; }
}

/// <summary>
/// Represents one slide in the model-facing analysis envelope.
/// </summary>
public sealed record CoursewareAnalysisSlideView
{
    /// <summary>Gets or sets the stable slide identifier.</summary>
    public string SlideId { get; set; } = string.Empty;

    /// <summary>Gets or sets the one-based page number.</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets or sets the zero-based slide index.</summary>
    public int SlideIndex { get; set; }

    /// <summary>Gets or sets the slide width.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the slide height.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the original Markdown fingerprint.</summary>
    public string SourceFingerprint { get; set; } = string.Empty;

    /// <summary>Gets or sets the privacy-safe Markdown analysis view.</summary>
    public string Markdown { get; set; } = string.Empty;
}

/// <summary>
/// Defines the required structured output behavior.
/// </summary>
public sealed record CoursewareAnalysisOutputRequirementsSection
{
    /// <summary>Gets or sets output requirements.</summary>
    public IReadOnlyList<string> Requirements { get; set; } = [];

    /// <summary>Gets or sets the required submission protocol.</summary>
    public string SubmissionProtocol { get; set; } = string.Empty;
}

/// <summary>
/// Represents the source-generated JSON envelope sent to the model.
/// </summary>
public sealed record CoursewareAnalysisEnvelope
{
    /// <summary>The current analysis-envelope schema version.</summary>
    public const string CurrentSchemaVersion = "courseware-analysis-envelope/v2";

    /// <summary>Gets or sets the envelope schema version.</summary>
    public string SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Gets or sets trusted task instructions.</summary>
    public CoursewareAnalysisTaskSection Task { get; set; } = new();

    /// <summary>Gets or sets the courseware overview.</summary>
    public CoursewareAnalysisOverviewSection CoursewareOverview { get; set; } = new();

    /// <summary>Gets or sets logical resources.</summary>
    public IReadOnlyList<CoursewareAnalysisResourceView> Resources { get; set; } = [];

    /// <summary>Gets or sets slides with stable identity and privacy-safe Markdown.</summary>
    public IReadOnlyList<CoursewareAnalysisSlideView> Slides { get; set; } = [];

    /// <summary>Gets or sets structured output requirements.</summary>
    public CoursewareAnalysisOutputRequirementsSection OutputRequirements { get; set; } = new();
}

/// <summary>
/// Represents complete, privacy-safe model input derived from an immutable source snapshot.
/// </summary>
public sealed class CoursewareAnalysisInput
{
    internal CoursewareAnalysisInput(
        string prompt,
        int totalSlideCount,
        int analyzedSlideCount,
        int characterCount,
        int estimatedTokenCount,
        bool wasTruncated,
        CoursewareAnalysisInputSectionCharacterCounts sectionCharacterCounts,
        string statisticsVersion,
        string sourceFingerprint,
        string analysisViewFingerprint,
        IEnumerable<CoursewarePathPrivacyDiagnostic> privacyDiagnostics,
        IEnumerable<string> warnings)
    {
        Prompt = prompt;
        TotalSlideCount = totalSlideCount;
        AnalyzedSlideCount = analyzedSlideCount;
        CharacterCount = characterCount;
        EstimatedTokenCount = estimatedTokenCount;
        WasTruncated = wasTruncated;
        SectionCharacterCounts = sectionCharacterCounts;
        StatisticsVersion = statisticsVersion;
        SourceFingerprint = sourceFingerprint;
        AnalysisViewFingerprint = analysisViewFingerprint;
        PrivacyDiagnostics = Array.AsReadOnly(privacyDiagnostics.ToArray());
        Warnings = Array.AsReadOnly(warnings.ToArray());
    }

    /// <summary>Gets the formatted model input.</summary>
    public string Prompt { get; }

    /// <summary>Gets the total number of source slides.</summary>
    public int TotalSlideCount { get; }

    /// <summary>Gets the number of slides represented by the prompt.</summary>
    public int AnalyzedSlideCount { get; }

    /// <summary>Gets the exact prompt character count.</summary>
    public int CharacterCount { get; }

    /// <summary>Gets the conservatively estimated prompt token count.</summary>
    public int EstimatedTokenCount { get; }

    /// <summary>Gets whether any source content was truncated.</summary>
    public bool WasTruncated { get; }

    /// <summary>Gets character counts for stable prompt sections.</summary>
    public CoursewareAnalysisInputSectionCharacterCounts SectionCharacterCounts { get; }

    /// <summary>Gets the deterministic statistics version.</summary>
    public string StatisticsVersion { get; }

    /// <summary>Gets the original logical source fingerprint.</summary>
    public string SourceFingerprint { get; }

    /// <summary>Gets the model-facing analysis-view fingerprint.</summary>
    public string AnalysisViewFingerprint { get; }

    /// <summary>Gets the model-facing fingerprint retained for downstream compatibility.</summary>
    public string InputFingerprint => AnalysisViewFingerprint;

    /// <summary>Gets path-privacy transformations applied to the analysis view.</summary>
    public IReadOnlyList<CoursewarePathPrivacyDiagnostic> PrivacyDiagnostics { get; }

    /// <summary>Gets non-blocking input warnings.</summary>
    public IReadOnlyList<string> Warnings { get; }
}

/// <summary>
/// Records the character count of each stable prompt section.
/// </summary>
public sealed class CoursewareAnalysisInputSectionCharacterCounts
{
    internal CoursewareAnalysisInputSectionCharacterCounts(
        int task,
        int coursewareOverview,
        int resourceCatalog,
        int slides,
        int outputRequirements)
    {
        Task = task;
        CoursewareOverview = coursewareOverview;
        ResourceCatalog = resourceCatalog;
        Slides = slides;
        OutputRequirements = outputRequirements;
    }

    /// <summary>Gets the task section character count.</summary>
    public int Task { get; }

    /// <summary>Gets the courseware overview section character count.</summary>
    public int CoursewareOverview { get; }

    /// <summary>Gets the resource catalog section character count.</summary>
    public int ResourceCatalog { get; }

    /// <summary>Gets the slides section character count.</summary>
    public int Slides { get; }

    /// <summary>Gets the output-requirements section character count.</summary>
    public int OutputRequirements { get; }
}
