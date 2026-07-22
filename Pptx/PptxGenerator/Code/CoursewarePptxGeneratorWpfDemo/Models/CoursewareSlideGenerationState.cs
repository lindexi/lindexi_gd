namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Identifies the latest page generation or manual rendering state.
/// </summary>
public enum CoursewareSlideGenerationState
{
    NotStarted,
    Generating,
    Rendering,
    Completed,
    Failed,
    Canceled,
}
