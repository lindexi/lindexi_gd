using System.IO;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the loaded input data for one courseware slide.
/// </summary>
public sealed class CoursewareSlideInput
{
    /// <summary>
    /// Gets the zero-based slide index.
    /// </summary>
    public required int SlideIndex { get; init; }

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Gets the slide identifier.
    /// </summary>
    public required string SlideId { get; init; }

    /// <summary>
    /// Gets the slide width.
    /// </summary>
    public required double Width { get; init; }

    /// <summary>
    /// Gets the slide height.
    /// </summary>
    public required double Height { get; init; }

    /// <summary>
    /// Gets the loaded Markdown file.
    /// </summary>
    public required FileInfo MarkdownFile { get; init; }

    /// <summary>
    /// Gets the loaded screenshot file, if it exists.
    /// </summary>
    public FileInfo? ScreenshotFile { get; init; }

    /// <summary>
    /// Gets the Markdown text.
    /// </summary>
    public required string MarkdownText { get; init; }

    /// <summary>
    /// Gets the warnings associated with this slide.
    /// </summary>
    public IReadOnlyList<CoursewareLoadWarning> Warnings { get; init; } = [];
}
