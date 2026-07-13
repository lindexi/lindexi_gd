using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.ViewModels;

/// <summary>
/// Provides lightweight display data for one slide in the courseware overview.
/// </summary>
public sealed class CoursewareThumbnailItemViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareThumbnailItemViewModel" /> class.
    /// </summary>
    /// <param name="slide">The source slide.</param>
    /// <param name="title">The extracted slide title.</param>
    public CoursewareThumbnailItemViewModel(CoursewareSlideInput slide, string title)
    {
        ArgumentNullException.ThrowIfNull(slide);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        PageNumber = slide.PageNumber;
        SlideId = slide.SlideId;
        Title = title;
        ScreenshotFilePath = slide.ScreenshotFile?.FullName;
        HasScreenshot = slide.ScreenshotFile is not null;
        HasWarning = slide.Warnings.Count > 0 || !HasScreenshot;
        WarningSummary = slide.Warnings.Count > 0
            ? string.Join("；", slide.Warnings.Select(warning => warning.Message))
            : HasScreenshot ? string.Empty : "缺少原始页面截图";
    }

    /// <summary>
    /// Gets the one-based page number.
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the slide identifier.
    /// </summary>
    public string SlideId { get; }

    /// <summary>
    /// Gets the extracted slide title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the screenshot file path, if available.
    /// </summary>
    public string? ScreenshotFilePath { get; }

    /// <summary>
    /// Gets a value indicating whether the source screenshot is available.
    /// </summary>
    public bool HasScreenshot { get; }

    /// <summary>
    /// Gets a value indicating whether this slide has an input warning.
    /// </summary>
    public bool HasWarning { get; }

    /// <summary>
    /// Gets the warning summary.
    /// </summary>
    public string WarningSummary { get; }

    /// <summary>
    /// Gets the accessible description of this thumbnail.
    /// </summary>
    public string AccessibleName => HasWarning
        ? $"第 {PageNumber} 页，{Title}，{WarningSummary}"
        : $"第 {PageNumber} 页，{Title}，摘要已读取";
}
