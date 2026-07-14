using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.ViewModels;

/// <summary>
/// Represents one lightweight thumbnail on the whole-courseware overview page.
/// </summary>
public sealed class CoursewareThumbnailItemViewModel
{
    /// <summary>
    /// Creates a thumbnail from a loaded slide input.
    /// </summary>
    /// <param name="slide">The loaded slide input.</param>
    /// <param name="title">The display title extracted from the slide Markdown.</param>
    /// <returns>The lightweight thumbnail view model.</returns>
    public static CoursewareThumbnailItemViewModel Create(CoursewareSlideInput slide, string title)
    {
        ArgumentNullException.ThrowIfNull(slide);
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("页面标题不能为空。", nameof(title));
        }

        return new CoursewareThumbnailItemViewModel
        {
            PageNumber = slide.PageNumber,
            SlideId = slide.SlideId,
            Title = title,
            Width = slide.Width,
            Height = slide.Height,
            ScreenshotFilePath = slide.ScreenshotFile?.FullName,
            Warnings = slide.Warnings,
        };
    }

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Gets the exported slide identifier.
    /// </summary>
    public required string SlideId { get; init; }

    /// <summary>
    /// Gets the slide title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the source slide width.
    /// </summary>
    public required double Width { get; init; }

    /// <summary>
    /// Gets the source slide height.
    /// </summary>
    public required double Height { get; init; }

    /// <summary>
    /// Gets the source screenshot path when the screenshot exists.
    /// </summary>
    public string? ScreenshotFilePath { get; init; }

    /// <summary>
    /// Gets the warnings associated with the source slide.
    /// </summary>
    public IReadOnlyList<CoursewareLoadWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the source screenshot is available.
    /// </summary>
    public bool HasScreenshot => !string.IsNullOrWhiteSpace(ScreenshotFilePath);

    /// <summary>
    /// Gets a value indicating whether the source slide has warnings.
    /// </summary>
    public bool HasWarning => Warnings.Count > 0;

    /// <summary>
    /// Gets the accessible description for the thumbnail.
    /// </summary>
    public string AccessibleName => HasWarning
        ? $"第 {PageNumber} 页，{Title}，存在输入警告"
        : $"第 {PageNumber} 页，{Title}";

    /// <summary>
    /// Gets the warning summary shown by the thumbnail tooltip.
    /// </summary>
    public string WarningSummary => HasWarning
        ? string.Join("；", Warnings.Select(warning => warning.Message))
        : "输入完整";
}