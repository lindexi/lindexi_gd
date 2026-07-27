using System.IO;
using System.Text.Json.Serialization;
using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Identifies the files required to restore a self-contained theme-analysis snapshot.
/// </summary>
public sealed record CoursewareThemeAnalysisSnapshotManifest
{
    /// <summary>
    /// Gets the supported snapshot schema version.
    /// </summary>
    public const int CurrentSchemaVersion = 2;

    /// <summary>
    /// Gets the snapshot schema version.
    /// </summary>
    [JsonPropertyName("Version")]
    public int Version { get; init; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets the snapshot creation time.
    /// </summary>
    [JsonPropertyName("CreatedAt")]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the relative path to the copied courseware manifest.
    /// </summary>
    [JsonPropertyName("CoursewareManifestFile")]
    public required string CoursewareManifestFile { get; init; }

    /// <summary>
    /// Gets the relative path to the Theme 2.0 JSON file.
    /// </summary>
    [JsonPropertyName("ThemeFile")]
    public required string ThemeFile { get; init; }
}

/// <summary>
/// Represents a theme-analysis snapshot loaded from local storage.
/// </summary>
public sealed record CoursewareThemeAnalysisSnapshot
{
    /// <summary>
    /// Gets the snapshot root directory.
    /// </summary>
    public required DirectoryInfo SnapshotDirectory { get; init; }

    /// <summary>
    /// Gets the courseware input package loaded from the snapshot.
    /// </summary>
    public required CoursewareInputPackage InputPackage { get; init; }

    /// <summary>
    /// Gets the restored theme-analysis result.
    /// </summary>
    public required CoursewareThemeAnalysisResult AnalysisResult { get; init; }
}