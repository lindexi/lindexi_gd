using System.Text.Json.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the root manifest stored in Courseware.json.
/// </summary>
public sealed class CoursewareExportManifest
{
    /// <summary>
    /// Gets the export format version.
    /// </summary>
    [JsonPropertyName("ExportVersion")]
    public int ExportVersion { get; init; }

    /// <summary>
    /// Gets the export creation time.
    /// </summary>
    [JsonPropertyName("CreatedAt")]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the courseware display name.
    /// </summary>
    [JsonPropertyName("CoursewareName")]
    public string? CoursewareName { get; init; }

    /// <summary>
    /// Gets the slide count declared by the manifest.
    /// </summary>
    [JsonPropertyName("SlideCount")]
    public int SlideCount { get; init; }

    /// <summary>
    /// Gets the slide entries declared by the manifest.
    /// </summary>
    [JsonPropertyName("Slides")]
    public IReadOnlyList<CoursewareExportSlideEntry> Slides { get; init; } = [];

    /// <summary>
    /// Gets the resources index path relative to the export root.
    /// </summary>
    [JsonPropertyName("ResourcesFile")]
    public string? ResourcesFile { get; init; }
}
