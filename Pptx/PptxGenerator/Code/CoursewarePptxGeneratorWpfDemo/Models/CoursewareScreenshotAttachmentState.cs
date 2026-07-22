namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Identifies the current source-screenshot attachment state for one courseware slide.
/// </summary>
public enum CoursewareScreenshotAttachmentState
{
    /// <summary>
    /// The slide has not started its first generation request.
    /// </summary>
    NotPrepared,

    /// <summary>
    /// The source screenshot will be or was attached to the first request.
    /// </summary>
    Attached,

    /// <summary>
    /// The source screenshot is not available on disk.
    /// </summary>
    FileMissing,

    /// <summary>
    /// The source screenshot could not be sent.
    /// </summary>
    SendFailed,
}
