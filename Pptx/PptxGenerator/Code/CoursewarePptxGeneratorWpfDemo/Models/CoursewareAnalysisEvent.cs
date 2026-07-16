namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Identifies one stage of whole-courseware analysis.
/// </summary>
public enum CoursewareAnalysisStage
{
    /// <summary>Preparing the model input.</summary>
    PreparingInput,
    /// <summary>Reading slide structure.</summary>
    ReadingStructure,
    /// <summary>Analyzing the content hierarchy.</summary>
    AnalyzingContentHierarchy,
    /// <summary>Designing the shared visual theme.</summary>
    DesigningTheme,
    /// <summary>Validating the submitted theme.</summary>
    ValidatingTheme,
    /// <summary>Requesting a corrected theme.</summary>
    RepairingTheme,
    /// <summary>The analysis completed successfully.</summary>
    Completed,
}

/// <summary>
/// Identifies the semantic kind of an analysis event.
/// </summary>
public enum CoursewareAnalysisEventKind
{
    /// <summary>A deterministic workflow progress event.</summary>
    Progress,
    /// <summary>A user-facing model summary.</summary>
    ModelSummary,
    /// <summary>A structured theme submission event.</summary>
    ToolSubmission,
    /// <summary>A non-blocking warning.</summary>
    Warning,
    /// <summary>A blocking error.</summary>
    Error,
}

/// <summary>
/// Identifies the state of an analysis event.
/// </summary>
public enum CoursewareAnalysisEventState
{
    /// <summary>The stage has not started.</summary>
    Pending,
    /// <summary>The stage is currently active.</summary>
    Running,
    /// <summary>The stage completed successfully.</summary>
    Completed,
    /// <summary>The stage completed with a warning.</summary>
    Warning,
    /// <summary>The stage failed.</summary>
    Failed,
}

/// <summary>
/// Represents one stable, user-facing whole-courseware analysis event.
/// </summary>
public sealed record CoursewareAnalysisEvent
{
    /// <summary>Gets the event identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>Gets the analysis stage.</summary>
    public required CoursewareAnalysisStage Stage { get; init; }
    /// <summary>Gets the event kind.</summary>
    public required CoursewareAnalysisEventKind Kind { get; init; }
    /// <summary>Gets the event title.</summary>
    public required string Title { get; init; }
    /// <summary>Gets the event detail.</summary>
    public required string Message { get; init; }
    /// <summary>Gets the event timestamp.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    /// <summary>Gets the event state.</summary>
    public required CoursewareAnalysisEventState State { get; init; }

    /// <summary>Gets a value indicating whether the message replaces the current text for the same event identifier.</summary>
    public bool ReplacesMessage { get; init; }
}