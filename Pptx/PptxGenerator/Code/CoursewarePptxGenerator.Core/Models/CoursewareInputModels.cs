using System.IO;
using System.Text.Json.Serialization;

namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Represents a warning discovered while loading courseware facts.
/// </summary>
public sealed record CoursewareLoadWarning(
    string Code,
    string Message,
    string? RelativePath = null,
    int? SlideIndex = null);

/// <summary>
/// Represents one exported courseware resource and its local resolution state.
/// </summary>
public sealed record CoursewareResourceEntry
{
    /// <summary>Gets or sets the logical resource identifier.</summary>
    [JsonPropertyName("ResourceId")]
    public string? ResourceId { get; set; }

    /// <summary>Gets or sets the resource type.</summary>
    [JsonPropertyName("ResourceType")]
    public string? ResourceType { get; set; }

    /// <summary>Gets or sets the exported relative file name.</summary>
    [JsonPropertyName("ExportFile")]
    public string? ExportFile { get; set; }

    /// <summary>Gets or sets the resolved local file path.</summary>
    [JsonIgnore]
    public string? ResolvedFilePath { get; set; }

    /// <summary>Gets or sets whether the resolved resource exists.</summary>
    [JsonIgnore]
    public bool Exists { get; set; }
}

/// <summary>
/// Represents the loaded local facts for one slide.
/// </summary>
public sealed class CoursewareSlideInput
{
    /// <summary>Gets the zero-based slide index.</summary>
    public int SlideIndex { get; init; }

    /// <summary>Gets the one-based page number.</summary>
    public int PageNumber { get; init; }

    /// <summary>Gets the stable slide identifier from the export manifest.</summary>
    public string SlideId { get; init; } = string.Empty;

    /// <summary>Gets the slide width.</summary>
    public double Width { get; init; }

    /// <summary>Gets the slide height.</summary>
    public double Height { get; init; }

    /// <summary>Gets the local Markdown file.</summary>
    public FileInfo MarkdownFile { get; init; } = null!;

    /// <summary>Gets the local screenshot file, when present.</summary>
    public FileInfo? ScreenshotFile { get; init; }

    /// <summary>Gets the exact Markdown text loaded from disk.</summary>
    public string MarkdownText { get; init; } = string.Empty;

    /// <summary>Gets slide-scoped load warnings.</summary>
    public IReadOnlyList<CoursewareLoadWarning> Warnings { get; init; } = [];
}

/// <summary>
/// Represents a loaded local courseware package. Local paths never leave this boundary.
/// </summary>
public sealed class CoursewareInputPackage
{
    /// <summary>Gets the export root directory.</summary>
    public DirectoryInfo RootDirectory { get; init; } = null!;

    /// <summary>Gets the courseware display name.</summary>
    public string CoursewareName { get; init; } = string.Empty;

    /// <summary>Gets the actual loaded slide count.</summary>
    public int SlideCount => Slides.Count;

    /// <summary>Gets loaded slides.</summary>
    public IReadOnlyList<CoursewareSlideInput> Slides { get; init; } = [];

    /// <summary>Gets loaded resources.</summary>
    public IReadOnlyList<CoursewareResourceEntry> Resources { get; init; } = [];

    /// <summary>Gets non-blocking load warnings.</summary>
    public IReadOnlyList<CoursewareLoadWarning> Warnings { get; init; } = [];
}

/// <summary>
/// Represents one immutable logical resource fact used by analysis.
/// </summary>
public sealed record CoursewareAnalysisResourceFact(string ResourceId, string ResourceType, bool Exists);

/// <summary>
/// Represents one immutable slide fact used by analysis.
/// </summary>
public sealed record CoursewareAnalysisSlideFact(
    int SlideIndex,
    int PageNumber,
    string SlideId,
    double Width,
    double Height,
    bool HasScreenshot,
    string MarkdownText,
    string SourceFingerprint);

/// <summary>
/// Represents the immutable local fact snapshot from which a privacy-safe model view is derived.
/// </summary>
public sealed class CoursewareAnalysisSourceSnapshot
{
    /// <summary>
    /// Initializes a new immutable fact snapshot.
    /// </summary>
    internal CoursewareAnalysisSourceSnapshot(
        string coursewareName,
        IEnumerable<CoursewareAnalysisSlideFact> slides,
        IEnumerable<CoursewareAnalysisResourceFact> resources,
        IEnumerable<CoursewareLoadWarning> warnings,
        string sourceFingerprint)
    {
        CoursewareName = coursewareName;
        Slides = Array.AsReadOnly(slides.ToArray());
        Resources = Array.AsReadOnly(resources.ToArray());
        Warnings = Array.AsReadOnly(warnings.ToArray());
        SourceFingerprint = sourceFingerprint;
    }

    /// <summary>Gets the courseware name.</summary>
    public string CoursewareName { get; }

    /// <summary>Gets slides in stable source order.</summary>
    public IReadOnlyList<CoursewareAnalysisSlideFact> Slides { get; }

    /// <summary>Gets logical resources in stable order.</summary>
    public IReadOnlyList<CoursewareAnalysisResourceFact> Resources { get; }

    /// <summary>Gets non-slide load warnings.</summary>
    public IReadOnlyList<CoursewareLoadWarning> Warnings { get; }

    /// <summary>Gets the fingerprint of the original logical facts before privacy transformation.</summary>
    public string SourceFingerprint { get; }
}
