namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Summarizes slide execution states for the active courseware workspace.
/// </summary>
public sealed record CoursewareSlideWorkspaceSummary
{
    /// <summary>
    /// Gets the total number of slides in the workspace.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets the number of slides that have not started.
    /// </summary>
    public required int NotStartedCount { get; init; }

    /// <summary>
    /// Gets the number of slides currently initializing, generating, or rendering.
    /// </summary>
    public required int InProgressCount { get; init; }

    /// <summary>
    /// Gets the number of slides whose runtime is ready and idle.
    /// </summary>
    public required int ReadyCount { get; init; }

    /// <summary>
    /// Gets the number of slides whose latest operation completed successfully.
    /// </summary>
    public required int CompletedCount { get; init; }

    /// <summary>
    /// Gets the number of slides whose latest operation failed.
    /// </summary>
    public required int FailedCount { get; init; }

    /// <summary>
    /// Gets the number of slides whose latest operation was canceled.
    /// </summary>
    public required int CanceledCount { get; init; }
}
