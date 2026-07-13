namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Identifies the page currently displayed by the courseware workspace.
/// </summary>
public enum CoursewareWorkspacePage
{
    /// <summary>
    /// The courseware import and global analysis page.
    /// </summary>
    CoursewareAnalysis,

    /// <summary>
    /// The per-slide beautification workspace.
    /// </summary>
    SlideWorkspace,
}

/// <summary>
/// Identifies the current stage of the courseware analysis workflow.
/// </summary>
public enum CoursewareAnalysisStage
{
    /// <summary>
    /// No courseware has been opened.
    /// </summary>
    Welcome,

    /// <summary>
    /// The selected courseware package is being loaded.
    /// </summary>
    LoadingCourseware,

    /// <summary>
    /// The global visual theme is being analyzed.
    /// </summary>
    AnalyzingTheme,

    /// <summary>
    /// A validated global theme is available.
    /// </summary>
    AnalysisReady,

    /// <summary>
    /// Loading or theme analysis failed.
    /// </summary>
    AnalysisFailed,

    /// <summary>
    /// Theme analysis was canceled after the package was loaded.
    /// </summary>
    Canceled,
}

/// <summary>
/// Identifies the kind of a user-facing analysis process message.
/// </summary>
public enum CoursewareAnalysisMessageKind
{
    /// <summary>
    /// A normal workflow stage message.
    /// </summary>
    Progress,

    /// <summary>
    /// A successfully completed workflow stage.
    /// </summary>
    Completed,

    /// <summary>
    /// A non-blocking input or analysis warning.
    /// </summary>
    Warning,

    /// <summary>
    /// A blocking workflow error.
    /// </summary>
    Error,
}
