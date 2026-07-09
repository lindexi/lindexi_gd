using System.Text.Json.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents a slide entry in courseware.json.
/// </summary>
public sealed class CoursewareExportSlideEntry
{
    /// <summary>
    /// Gets the zero-based slide index.
    /// </summary>
    [JsonPropertyName("slideIndex")]
    public int SlideIndex { get; init; }

    /// <summary>
    /// Gets the exported slide identifier.
    /// </summary>
    [JsonPropertyName("slideId")]
    public string? SlideId { get; init; }

    /// <summary>
    /// Gets the slide width.
    /// </summary>
    [JsonPropertyName("width")]
    public double Width { get; init; }

    /// <summary>
    /// Gets the slide height.
    /// </summary>
    [JsonPropertyName("height")]
    public double Height { get; init; }

    /// <summary>
    /// Gets the Markdown file path relative to the export root.
    /// </summary>
    [JsonPropertyName("markdownFile")]
    public string? MarkdownFile { get; init; }

    /// <summary>
    /// Gets the screenshot file path relative to the export root.
    /// </summary>
    [JsonPropertyName("screenshotFile")]
    public string? ScreenshotFile { get; init; }
}
