using System.Text.Json.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents an exported courseware resource entry.
/// </summary>
public sealed record CoursewareResourceEntry
{
    /// <summary>
    /// Gets the resource identifier.
    /// </summary>
    [JsonPropertyName("ResourceId")]
    public string? ResourceId { get; init; }

    /// <summary>
    /// Gets the resource type.
    /// </summary>
    [JsonPropertyName("ResourceType")]
    public string? ResourceType { get; init; }

    /// <summary>
    /// Gets the exported file path relative to the resources directory.
    /// </summary>
    [JsonPropertyName("ExportFile")]
    public string? ExportFile { get; init; }

    /// <summary>
    /// Gets the resolved local file path.
    /// </summary>
    public string? ResolvedFilePath { get; init; }

    /// <summary>
    /// Gets a value indicating whether the resolved resource file exists.
    /// </summary>
    public bool Exists { get; init; }
}
