using PptxGenerator;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents a slide item in the courseware workspace.
/// </summary>
public sealed class CoursewareSlideItem
{
    /// <summary>
    /// Gets the independent SlideML chat manager for this slide.
    /// </summary>
    public required SlideChatManager SlideChatManager { get; init; }

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Gets the exported slide identifier.
    /// </summary>
    public required string SlideId { get; init; }

    /// <summary>
    /// Gets the source slide width.
    /// </summary>
    public required double Width { get; init; }

    /// <summary>
    /// Gets the source slide height.
    /// </summary>
    public required double Height { get; init; }

    /// <summary>
    /// Gets the slide title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the current processing status text.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the source Markdown text.
    /// </summary>
    public required string SourceMarkdownText { get; init; }

    /// <summary>
    /// Gets the source screenshot file path, if available.
    /// </summary>
    public string? SourceScreenshotFilePath { get; init; }

    /// <summary>
    /// Gets a value indicating whether the source screenshot exists.
    /// </summary>
    public bool HasSourceScreenshot => !string.IsNullOrWhiteSpace(SourceScreenshotFilePath);

    /// <summary>
    /// Gets the SlideML content for the slide.
    /// </summary>
    public required string SlideMl { get; init; }

    /// <summary>
    /// Gets the rendering log for the slide.
    /// </summary>
    public required string RenderingLog { get; init; }

    /// <summary>
    /// Gets the callback XML content for the slide.
    /// </summary>
    public required string CallbackXml { get; init; }
}
