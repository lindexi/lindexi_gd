namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the deterministic JSON envelope supplied to one slide-generation request.
/// </summary>
public sealed record CoursewareSlideGenerationEnvelope
{
    /// <summary>
    /// Gets the current slide-generation envelope schema version.
    /// </summary>
    public const string CurrentSchemaVersion = "courseware-slide-generation/v1";

    /// <summary>
    /// Gets the envelope schema version.
    /// </summary>
    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets trusted task instructions and the requested page change.
    /// </summary>
    public CoursewareSlideGenerationTask Task { get; init; } = new();

    /// <summary>
    /// Gets the complete current-slide input.
    /// </summary>
    public CoursewareSlideGenerationPage CurrentSlide { get; init; } = new();

    /// <summary>
    /// Gets bounded neighboring-slide summaries.
    /// </summary>
    public CoursewareSlideNeighborContext Neighbors { get; init; } = new();

    /// <summary>
    /// Gets the structured design context derived from whole-courseware theme analysis.
    /// </summary>
    public CoursewarePageDesignContext DesignContext { get; init; } = null!;

    /// <summary>
    /// Gets the visual evidence supplied with this request.
    /// </summary>
    public CoursewareSlideVisualInput VisualInput { get; init; } = new();

    /// <summary>
    /// Gets trusted output requirements for the generated SlideML.
    /// </summary>
    public CoursewareSlideOutputRequirements OutputRequirements { get; init; } = new();
}

/// <summary>
/// Defines trusted instructions for one slide-generation request.
/// </summary>
public sealed record CoursewareSlideGenerationTask
{
    /// <summary>
    /// Gets the local task objective.
    /// </summary>
    public string Objective { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user-requested page change.
    /// </summary>
    public string UserInstruction { get; init; } = string.Empty;

    /// <summary>
    /// Gets mandatory page-generation behavior requirements.
    /// </summary>
    public IReadOnlyList<string> Requirements { get; init; } = [];

    /// <summary>
    /// Gets the untrusted-data boundary statement.
    /// </summary>
    public string DataBoundary { get; init; } = string.Empty;

    /// <summary>
}

/// <summary>
/// Represents the complete current-slide source supplied to generation.
/// </summary>
public sealed record CoursewareSlideGenerationPage
{
    /// <summary>
    /// Gets the stable slide identifier.
    /// </summary>
    public string SlideId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Gets the zero-based slide index.
    /// </summary>
    public int SlideIndex { get; init; }

    /// <summary>
    /// Gets the original logical width from the courseware export.
    /// </summary>
    public double LogicalWidth { get; init; }

    /// <summary>
    /// Gets the original logical height from the courseware export.
    /// </summary>
    public double LogicalHeight { get; init; }

    /// <summary>
    /// Gets the actual page canvas width.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Gets the actual page canvas height.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current user message includes the source screenshot.
    /// </summary>
    public bool ScreenshotAttached { get; init; }

    /// <summary>
    /// Gets path-free load warning codes for the current slide.
    /// </summary>
    public IReadOnlyList<string> WarningCodes { get; init; } = [];

    /// <summary>
    /// Gets deterministic input and canvas diagnostics for the current slide.
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; init; } = [];

    /// <summary>
    /// Gets logical resources referenced by the current slide Markdown.
    /// </summary>
    public IReadOnlyList<CoursewareSlideGenerationResource> Resources { get; init; } = [];

    /// <summary>
    /// Gets the complete privacy-safe Markdown for the current slide.
    /// </summary>
    public string Markdown { get; init; } = string.Empty;
}

/// <summary>
/// Represents one path-free logical resource available to the current slide.
/// </summary>
public sealed record CoursewareSlideGenerationResource
{
    /// <summary>
    /// Gets the logical resource identifier.
    /// </summary>
    public string ResourceId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the resource type.
    /// </summary>
    public string ResourceType { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether the resource exists in the loaded package.
    /// </summary>
    public bool Exists { get; init; }
}

/// <summary>
/// Describes the visual evidence boundary for one request.
/// </summary>
public sealed record CoursewareSlideVisualInput
{
    /// <summary>
    /// Gets whether the source screenshot exists in the loaded package.
    /// </summary>
    public bool SourceScreenshotAvailable { get; init; }

    /// <summary>
    /// Gets whether the same user message includes the source screenshot.
    /// </summary>
    public bool WasAttached { get; init; }

    /// <summary>
    /// Gets the trusted statement limiting how visual evidence may be used.
    /// </summary>
    public string EvidenceBoundary { get; init; } = string.Empty;
}

/// <summary>
/// Defines trusted SlideML output requirements.
/// </summary>
public sealed record CoursewareSlideOutputRequirements
{
    /// <summary>
    /// Gets the required SlideML root element.
    /// </summary>
    public string RootElement { get; init; } = "Page";

    /// <summary>
    /// Gets mandatory output constraints.
    /// </summary>
    public IReadOnlyList<string> Requirements { get; init; } = [];
}

/// <summary>
/// Represents bounded courseware context around the current slide.
/// </summary>
public sealed record CoursewareSlideNeighborContext
{
    /// <summary>
    /// Gets the previous-slide summary when available.
    /// </summary>
    public CoursewareSlideNeighborSummary? Previous { get; init; }

    /// <summary>
    /// Gets the next-slide summary when available.
    /// </summary>
    public CoursewareSlideNeighborSummary? Next { get; init; }
}

/// <summary>
/// Represents one deterministic neighboring-slide summary.
/// </summary>
public sealed record CoursewareSlideNeighborSummary
{
    /// <summary>
    /// Gets the stable slide identifier.
    /// </summary>
    public string SlideId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Gets the deterministic display title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the bounded deterministic content summary.
    /// </summary>
    public string Summary { get; init; } = string.Empty;
}

/// <summary>
/// Represents a built page-generation prompt and its deterministic diagnostics.
/// </summary>
public sealed record CoursewareSlidePromptBuildResult
{
    /// <summary>
    /// Gets the serialized JSON prompt.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Gets the conservatively estimated prompt token count before SlideML prompt wrapping.
    /// </summary>
    public required int EstimatedTokenCount { get; init; }

    /// <summary>
    /// Gets the structured envelope represented by <see cref="Prompt" />.
    /// </summary>
    public required CoursewareSlideGenerationEnvelope Envelope { get; init; }
}
