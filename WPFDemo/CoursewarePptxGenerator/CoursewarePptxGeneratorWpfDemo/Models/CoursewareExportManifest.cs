using System.Text.Json.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the root manifest stored in courseware.json.
/// </summary>
public sealed class CoursewareExportManifest
{
    /// <summary>
    /// Gets the export format version.
    /// </summary>
    [JsonPropertyName("exportVersion")]
    public int ExportVersion { get; init; }

    /// <summary>
    /// Gets the export creation time.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the courseware display name.
    /// </summary>
    [JsonPropertyName("coursewareName")]
    public string? CoursewareName { get; init; }

    /// <summary>
    /// Gets the slide count declared by the manifest.
    /// </summary>
    [JsonPropertyName("slideCount")]
    public int SlideCount { get; init; }

    /// <summary>
    /// Gets the slide entries declared by the manifest.
    /// </summary>
    [JsonPropertyName("slides")]
    public IReadOnlyList<CoursewareExportSlideEntry> Slides { get; init; } = [];

    /// <summary>
    /// Gets the resources index path relative to the export root.
    /// </summary>
    [JsonPropertyName("resourcesFile")]
    public string? ResourcesFile { get; init; }
}
