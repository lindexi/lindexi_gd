namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Identifies the current execution state of one courseware slide.
/// </summary>
public enum CoursewareSlideState
{
    /// <summary>
    /// The slide has not created its page runtime.
    /// </summary>
    NotStarted,

    /// <summary>
    /// The slide is creating its page runtime.
    /// </summary>
    Initializing,

    /// <summary>
    /// The slide runtime is ready and no operation is running.
    /// </summary>
    Ready,

    /// <summary>
    /// The slide is generating SlideML through the language model.
    /// </summary>
    Generating,

    /// <summary>
    /// The slide is rendering existing SlideML.
    /// </summary>
    Rendering,

    /// <summary>
    /// The latest slide operation completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// The latest slide operation failed.
    /// </summary>
    Failed,

    /// <summary>
    /// The latest slide operation was canceled.
    /// </summary>
    Canceled,
}
