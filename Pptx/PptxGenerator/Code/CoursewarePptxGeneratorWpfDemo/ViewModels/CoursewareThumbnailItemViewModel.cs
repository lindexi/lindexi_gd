using CoursewarePptxGenerator.Core.Models;

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
    /// <returns>The lightweight thumbnail view model.</returns>
    public static CoursewareThumbnailItemViewModel Create(CoursewareSlideInput slide)
    {
        ArgumentNullException.ThrowIfNull(slide);

        return new CoursewareThumbnailItemViewModel
        {
            PageNumber = slide.PageNumber,
            SlideId = slide.SlideId,
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
        ? $"第 {PageNumber} 页，存在输入警告"
        : $"第 {PageNumber} 页";

    /// <summary>
    /// Gets the warning summary shown by the thumbnail tooltip.
    /// </summary>
    public string WarningSummary => HasWarning
        ? string.Join("；", Warnings.Select(warning => warning.Message))
        : "输入完整";
}