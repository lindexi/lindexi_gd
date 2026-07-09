using System.Text.Json.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents an exported courseware resource entry.
/// </summary>
public sealed record CoursewareResourceEntry
{
    /// <summary>
    /// Gets the resource image identifier.
    /// </summary>
    [JsonPropertyName("imageId")]
    public string? ImageId { get; init; }

    /// <summary>
    /// Gets the original source file name.
    /// </summary>
    [JsonPropertyName("sourceFileName")]
    public string? SourceFileName { get; init; }

    /// <summary>
    /// Gets the exported file path relative to the resources directory.
    /// </summary>
    [JsonPropertyName("exportFile")]
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
