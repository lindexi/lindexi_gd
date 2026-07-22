using System.IO;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Represents the classified result of opening a courseware workspace directory.
/// </summary>
public sealed record CoursewareWorkspaceFolderLoadResult
{
    /// <summary>
    /// Gets the loaded courseware input package.
    /// </summary>
    public required CoursewareInputPackage InputPackage { get; init; }

    /// <summary>
    /// Gets the restored analysis result when the selected directory is a snapshot.
    /// </summary>
    public CoursewareThemeAnalysisResult? AnalysisResult { get; init; }

    /// <summary>
    /// Gets a value indicating whether the selected directory is a theme-analysis snapshot.
    /// </summary>
    public bool IsThemeAnalysisSnapshot => AnalysisResult is not null;
}

/// <summary>
/// Classifies and loads ordinary courseware exports and theme-analysis snapshots through one entry point.
/// </summary>
public sealed class CoursewareWorkspaceFolderLoader
{
    private readonly CoursewareFolderLoader _coursewareFolderLoader;
    private readonly ICoursewareThemeAnalysisSnapshotStore _snapshotStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareWorkspaceFolderLoader" /> class.
    /// </summary>
    /// <param name="coursewareFolderLoader">The ordinary courseware folder loader.</param>
    /// <param name="snapshotStore">The theme-analysis snapshot store.</param>
    public CoursewareWorkspaceFolderLoader(
        CoursewareFolderLoader coursewareFolderLoader,
        ICoursewareThemeAnalysisSnapshotStore snapshotStore)
    {
        ArgumentNullException.ThrowIfNull(coursewareFolderLoader);
        ArgumentNullException.ThrowIfNull(snapshotStore);

        _coursewareFolderLoader = coursewareFolderLoader;
        _snapshotStore = snapshotStore;
    }

    /// <summary>
    /// Loads and classifies a selected courseware workspace directory.
    /// </summary>
    /// <param name="folderPath">The selected directory path.</param>
    /// <param name="cancellationToken">The token used to cancel the load operation.</param>
    /// <returns>The classified folder load result.</returns>
    public async Task<CoursewareWorkspaceFolderLoadResult> LoadAsync(
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("课件目录不能为空。", nameof(folderPath));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var manifestPath = Path.Join(folderPath, _snapshotStore.ManifestFileName);
        if (File.Exists(manifestPath))
        {
            var snapshot = await _snapshotStore.LoadAsync(folderPath, cancellationToken).ConfigureAwait(false);
            return new CoursewareWorkspaceFolderLoadResult
            {
                InputPackage = snapshot.InputPackage,
                AnalysisResult = snapshot.AnalysisResult,
            };
        }

        var inputPackage = await _coursewareFolderLoader.LoadAsync(folderPath, cancellationToken)
            .ConfigureAwait(false);
        return new CoursewareWorkspaceFolderLoadResult
        {
            InputPackage = inputPackage,
        };
    }
}
