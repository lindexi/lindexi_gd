using System.IO;
using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Defines the persisted manifest for a self-contained courseware theme-analysis snapshot.
/// </summary>
public sealed record CoursewareThemeAnalysisSnapshotManifest
{
    /// <summary>
    /// Gets the file name that identifies a theme-analysis snapshot directory.
    /// </summary>
    public const string FileName = "CoursewareThemeAnalysis.json";

    /// <summary>
    /// Gets the snapshot schema version supported by this application.
    /// </summary>
    public const string CurrentSchemaVersion = "courseware-theme-analysis-snapshot/v1";

    /// <summary>
    /// Gets the snapshot schema version.
    /// </summary>
    public required string SchemaVersion { get; init; }

    /// <summary>
    /// Gets the time when the snapshot was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the courseware display name captured by the snapshot.
    /// </summary>
    public required string CoursewareName { get; init; }

    /// <summary>
    /// Gets the number of slides captured by the snapshot.
    /// </summary>
    public required int SlideCount { get; init; }

    /// <summary>
    /// Gets the fingerprint of the logical courseware facts copied into the snapshot.
    /// </summary>
    public required string SourceFingerprint { get; init; }

    /// <summary>
    /// Gets the complete validated theme-analysis result.
    /// </summary>
    public required CoursewareThemeAnalysisResult AnalysisResult { get; init; }
}

/// <summary>
/// Represents a validated theme-analysis snapshot loaded from local storage.
/// </summary>
public sealed record CoursewareThemeAnalysisSnapshot
{
    /// <summary>
    /// Gets the snapshot root directory.
    /// </summary>
    public required DirectoryInfo SnapshotDirectory { get; init; }

    /// <summary>
    /// Gets the normalized courseware input package loaded from the snapshot.
    /// </summary>
    public required CoursewareInputPackage InputPackage { get; init; }

    /// <summary>
    /// Gets the validated persisted manifest.
    /// </summary>
    public required CoursewareThemeAnalysisSnapshotManifest Manifest { get; init; }

    /// <summary>
    /// Gets the restored theme-analysis result.
    /// </summary>
    public CoursewareThemeAnalysisResult AnalysisResult => Manifest.AnalysisResult;
}
