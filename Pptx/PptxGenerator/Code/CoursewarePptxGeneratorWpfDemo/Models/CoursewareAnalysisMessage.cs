namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents one user-facing item in the read-only analysis process.
/// </summary>
/// <param name="Title">The concise stage title.</param>
/// <param name="Description">The user-facing stage description.</param>
/// <param name="Kind">The semantic kind of the message.</param>
/// <param name="Timestamp">The time at which the message was published.</param>
public sealed record CoursewareAnalysisMessage(
    string Title,
    string Description,
    CoursewareAnalysisMessageKind Kind,
    DateTimeOffset Timestamp);
