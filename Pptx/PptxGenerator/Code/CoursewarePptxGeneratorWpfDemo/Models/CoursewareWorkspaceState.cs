namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Identifies the current state of the real courseware loading workflow.
/// </summary>
public enum CoursewareWorkspaceState
{
    /// <summary>
    /// No courseware has been selected.
    /// </summary>
    Welcome,

    /// <summary>
    /// The selected courseware folder is being parsed.
    /// </summary>
    LoadingCourseware,

    /// <summary>
    /// The courseware input package and thumbnails are available.
    /// </summary>
    CoursewareLoaded,

    /// <summary>
    /// The selected courseware folder could not be loaded.
    /// </summary>
    LoadFailed,
}