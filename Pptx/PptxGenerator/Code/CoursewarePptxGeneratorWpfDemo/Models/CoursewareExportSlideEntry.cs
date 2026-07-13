using System.Text.Json.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents a slide entry in Courseware.json.
/// </summary>
public sealed class CoursewareExportSlideEntry
{
    /// <summary>
    /// Gets the zero-based slide index.
    /// </summary>
    [JsonPropertyName("SlideIndex")]
    public int SlideIndex { get; init; }

    /// <summary>
    /// Gets the exported slide identifier.
    /// </summary>
    [JsonPropertyName("SlideId")]
    public string? SlideId { get; init; }

    /// <summary>
    /// Gets the slide width.
    /// </summary>
    [JsonPropertyName("Width")]
    public double Width { get; init; }

    /// <summary>
    /// Gets the slide height.
    /// </summary>
    [JsonPropertyName("Height")]
    public double Height { get; init; }

    /// <summary>
    /// Gets the Markdown file path relative to the export root.
    /// </summary>
    [JsonPropertyName("MarkdownFile")]
    public string? MarkdownFile { get; init; }

    /// <summary>
    /// Gets the screenshot file path relative to the export root.
    /// </summary>
    [JsonPropertyName("ScreenshotFile")]
    public string? ScreenshotFile { get; init; }
}
