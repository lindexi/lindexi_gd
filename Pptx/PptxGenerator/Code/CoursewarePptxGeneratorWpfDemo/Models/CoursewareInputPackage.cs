using System.IO;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents a loaded courseware input package.
/// </summary>
public sealed class CoursewareInputPackage
{
    /// <summary>
    /// Gets the export root directory.
    /// </summary>
    public required DirectoryInfo RootDirectory { get; init; }

    /// <summary>
    /// Gets the courseware display name.
    /// </summary>
    public required string CoursewareName { get; init; }

    /// <summary>
    /// Gets the actual loaded slide count.
    /// </summary>
    public int SlideCount => Slides.Count;

    /// <summary>
    /// Gets the loaded slides.
    /// </summary>
    public IReadOnlyList<CoursewareSlideInput> Slides { get; init; } = [];

    /// <summary>
    /// Gets the loaded resources.
    /// </summary>
    public IReadOnlyList<CoursewareResourceEntry> Resources { get; init; } = [];

    /// <summary>
    /// Gets all non-blocking warnings discovered while loading.
    /// </summary>
    public IReadOnlyList<CoursewareLoadWarning> Warnings { get; init; } = [];
}
