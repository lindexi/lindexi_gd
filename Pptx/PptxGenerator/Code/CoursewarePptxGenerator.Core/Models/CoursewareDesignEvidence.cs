namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Identifies the provenance of a design decision.
/// </summary>
public enum CoursewareEvidenceSourceKind
{
    /// <summary>A fact present in one slide's Markdown source.</summary>
    SlideMarkdownFact,

    /// <summary>A deterministic statistic calculated locally.</summary>
    DeterministicStatistic,

    /// <summary>A heuristic candidate calculated locally.</summary>
    HeuristicCandidate,

    /// <summary>An explicit user requirement.</summary>
    UserConstraint,

    /// <summary>An observation derived from supplied image content.</summary>
    VisualObservation,

    /// <summary>A model-authored design inference.</summary>
    DesignInference,
}

/// <summary>
/// References one source used to support a design decision.
/// </summary>
public sealed record CoursewareEvidenceReference
{
    /// <summary>Gets the evidence source kind.</summary>
    public CoursewareEvidenceSourceKind SourceKind { get; set; }

    /// <summary>Gets the related slide identifier, when applicable.</summary>
    public string? SlideId { get; set; }

    /// <summary>Gets the one-based page number, when applicable.</summary>
    public int? PageNumber { get; set; }

    /// <summary>Gets the related resource identifier, when applicable.</summary>
    public string? ResourceId { get; set; }

    /// <summary>Gets the related deterministic statistic identifier, when applicable.</summary>
    public string? StatisticId { get; set; }

    /// <summary>Gets a concise description of the referenced fact.</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Records one important design decision and its provenance.
/// </summary>
public sealed record CoursewareDesignDecisionEvidence
{
    /// <summary>Gets the stable decision identifier.</summary>
    public string DecisionId { get; set; } = string.Empty;

    /// <summary>Gets the semantic decision kind.</summary>
    public string DecisionKind { get; set; } = string.Empty;

    /// <summary>Gets the selected value or rule.</summary>
    public string SelectedValue { get; set; } = string.Empty;

    /// <summary>Gets the confidence from zero to one.</summary>
    public double Confidence { get; set; }

    /// <summary>Gets the sources supporting the decision.</summary>
    public IReadOnlyList<CoursewareEvidenceReference> Sources { get; set; } = [];

    /// <summary>Gets rejected alternatives and their reasons.</summary>
    public IReadOnlyList<string> RejectedAlternatives { get; set; } = [];
}

/// <summary>
/// Records an unresolved assumption that must not be presented as fact.
/// </summary>
public sealed record CoursewareDesignAssumption
{
    /// <summary>Gets the stable assumption identifier.</summary>
    public string AssumptionId { get; set; } = string.Empty;

    /// <summary>Gets the assumption text.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets the confidence from zero to one.</summary>
    public double Confidence { get; set; }

    /// <summary>Gets the evidence available for the assumption.</summary>
    public IReadOnlyList<CoursewareEvidenceReference> Sources { get; set; } = [];
}
