using System.Text.Json.Serialization;

namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Represents the root manifest stored in Courseware.json.
/// </summary>
public sealed class CoursewareExportManifest
{
    /// <summary>Gets or sets the export format version.</summary>
    [JsonPropertyName("ExportVersion")]
    public int ExportVersion { get; set; }

    /// <summary>Gets or sets the export creation time.</summary>
    [JsonPropertyName("CreatedAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the courseware display name.</summary>
    [JsonPropertyName("CoursewareName")]
    public string? CoursewareName { get; set; }

    /// <summary>Gets or sets the declared slide count.</summary>
    [JsonPropertyName("SlideCount")]
    public int SlideCount { get; set; }

    /// <summary>Gets or sets slide entries.</summary>
    [JsonPropertyName("Slides")]
    public IReadOnlyList<CoursewareExportSlideEntry> Slides { get; set; } = [];

    /// <summary>Gets or sets the relative resource-index path.</summary>
    [JsonPropertyName("ResourcesFile")]
    public string? ResourcesFile { get; set; }
}

/// <summary>
/// Represents one slide entry in Courseware.json.
/// </summary>
public sealed class CoursewareExportSlideEntry
{
    /// <summary>Gets or sets the zero-based slide index.</summary>
    [JsonPropertyName("SlideIndex")]
    public int SlideIndex { get; set; }

    /// <summary>Gets or sets the stable slide identifier.</summary>
    [JsonPropertyName("SlideId")]
    public string? SlideId { get; set; }

    /// <summary>Gets or sets the slide width.</summary>
    [JsonPropertyName("Width")]
    public double Width { get; set; }

    /// <summary>Gets or sets the slide height.</summary>
    [JsonPropertyName("Height")]
    public double Height { get; set; }

    /// <summary>Gets or sets the relative Markdown path.</summary>
    [JsonPropertyName("MarkdownFile")]
    public string? MarkdownFile { get; set; }

    /// <summary>Gets or sets the relative screenshot path.</summary>
    [JsonPropertyName("ScreenshotFile")]
    public string? ScreenshotFile { get; set; }
}
