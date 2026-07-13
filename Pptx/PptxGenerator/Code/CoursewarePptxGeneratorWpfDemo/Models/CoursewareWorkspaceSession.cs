using CoursewarePptxGeneratorWpfDemo.ViewModels;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the shared state of one opened courseware workspace.
/// </summary>
public sealed class CoursewareWorkspaceSession
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareWorkspaceSession" /> class.
    /// </summary>
    /// <param name="inputPackage">The loaded input package.</param>
    /// <param name="thumbnails">The lightweight slide overview items.</param>
    public CoursewareWorkspaceSession(
        CoursewareInputPackage inputPackage,
        IReadOnlyList<CoursewareThumbnailItemViewModel> thumbnails)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);
        ArgumentNullException.ThrowIfNull(thumbnails);

        InputPackage = inputPackage;
        Thumbnails = thumbnails;
    }

    /// <summary>
    /// Gets the loaded input package.
    /// </summary>
    public CoursewareInputPackage InputPackage { get; }

    /// <summary>
    /// Gets the lightweight overview thumbnails.
    /// </summary>
    public IReadOnlyList<CoursewareThumbnailItemViewModel> Thumbnails { get; }

    /// <summary>
    /// Gets or sets the validated global theme result.
    /// </summary>
    public CoursewareThemeAnalysisResult? Theme { get; set; }

    /// <summary>
    /// Gets or sets the selected zero-based slide index.
    /// </summary>
    public int SelectedSlideIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether generated page results have not been saved.
    /// </summary>
    public bool HasUnsavedResults { get; set; }
}
