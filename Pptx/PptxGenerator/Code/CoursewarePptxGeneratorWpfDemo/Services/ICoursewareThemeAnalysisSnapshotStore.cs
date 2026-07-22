using System.IO;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Saves and restores self-contained courseware theme-analysis snapshots.
/// </summary>
public interface ICoursewareThemeAnalysisSnapshotStore
{
    /// <summary>
    /// Gets the file name that identifies a snapshot directory.
    /// </summary>
    string ManifestFileName { get; }

    /// <summary>
    /// Saves a self-contained snapshot for the specified analysis result.
    /// </summary>
    /// <param name="inputPackage">The courseware facts consumed by theme analysis.</param>
    /// <param name="analysisResult">The validated theme-analysis result.</param>
    /// <param name="cancellationToken">The token used to cancel the save operation.</param>
    /// <returns>The published snapshot directory.</returns>
    Task<DirectoryInfo> SaveAsync(
        CoursewareInputPackage inputPackage,
        CoursewareThemeAnalysisResult analysisResult,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads and validates an existing snapshot directory.
    /// </summary>
    /// <param name="folderPath">The snapshot directory path.</param>
    /// <param name="cancellationToken">The token used to cancel the load operation.</param>
    /// <returns>The validated snapshot.</returns>
    Task<CoursewareThemeAnalysisSnapshot> LoadAsync(
        string folderPath,
        CancellationToken cancellationToken = default);
}
