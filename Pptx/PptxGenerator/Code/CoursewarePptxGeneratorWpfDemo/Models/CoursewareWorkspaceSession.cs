using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the lightweight state shared while one courseware package is open.
/// </summary>
public sealed class CoursewareWorkspaceSession
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareWorkspaceSession" /> class.
    /// </summary>
    /// <param name="inputPackage">The loaded courseware input package.</param>
    public CoursewareWorkspaceSession(CoursewareInputPackage inputPackage)
    {
        ArgumentNullException.ThrowIfNull(inputPackage);

        InputPackage = inputPackage;
    }

    /// <summary>
    /// Gets the loaded courseware input package.
    /// </summary>
    public CoursewareInputPackage InputPackage { get; }

    /// <summary>
    /// Gets or sets the latest whole-courseware theme analysis result.
    /// </summary>
    public CoursewareThemeAnalysisResult? ThemeAnalysisResult { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the analysis run allowed to publish into this session.
    /// </summary>
    public Guid? ActiveAnalysisRunId { get; set; }

    /// <summary>
    /// Gets or sets the time when the active analysis started.
    /// </summary>
    public DateTimeOffset? AnalysisStartedAt { get; set; }

}