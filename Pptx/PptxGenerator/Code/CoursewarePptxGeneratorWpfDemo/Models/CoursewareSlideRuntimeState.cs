namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Identifies the lifecycle state of one lazily created page runtime.
/// </summary>
public enum CoursewareSlideRuntimeState
{
    NotCreated,
    Creating,
    Ready,
    ModelUnavailable,
    Failed,
    Canceled,
}
