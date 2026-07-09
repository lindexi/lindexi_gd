using System.Text.Json.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents an object-wrapped resources index file.
/// </summary>
public sealed class CoursewareResourceManifest
{
    /// <summary>
    /// Gets the resources declared by the index.
    /// </summary>
    [JsonPropertyName("resources")]
    public IReadOnlyList<CoursewareResourceEntry> Resources { get; init; } = [];
}
