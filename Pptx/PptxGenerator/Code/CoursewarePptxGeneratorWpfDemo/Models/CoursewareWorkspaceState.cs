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
    /// The loaded courseware is being analyzed as a whole.
    /// </summary>
    AnalyzingCourseware,

    /// <summary>
    /// The courseware theme analysis result is available for review.
    /// </summary>
    AnalysisReady,

    /// <summary>
    /// The selected courseware folder could not be loaded.
    /// </summary>
    LoadFailed,

    /// <summary>
    /// The courseware was loaded, but its theme analysis failed.
    /// </summary>
    AnalysisFailed,

    /// <summary>
    /// The current courseware workflow was canceled.
    /// </summary>
    Canceled,
}