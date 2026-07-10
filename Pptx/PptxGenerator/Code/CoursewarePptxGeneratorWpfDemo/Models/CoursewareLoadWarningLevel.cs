namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Defines the severity of a courseware loading warning.
/// </summary>
public enum CoursewareLoadWarningLevel
{
    /// <summary>
    /// Indicates an informational loading message.
    /// </summary>
    Info,

    /// <summary>
    /// Indicates a recoverable loading problem.
    /// </summary>
    Warning,

    /// <summary>
    /// Indicates a blocking loading problem.
    /// </summary>
    Error,
}
