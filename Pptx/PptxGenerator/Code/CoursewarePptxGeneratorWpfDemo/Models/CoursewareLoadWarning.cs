namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents a warning or error discovered while loading a courseware export folder.
/// </summary>
/// <param name="Code">The stable warning code.</param>
/// <param name="Message">The user-facing warning message.</param>
/// <param name="RelativePath">The related relative path, if any.</param>
/// <param name="SlideIndex">The related zero-based slide index, if any.</param>
public sealed record CoursewareLoadWarning(
    string Code,
    string Message,
    string? RelativePath = null,
    int? SlideIndex = null);
