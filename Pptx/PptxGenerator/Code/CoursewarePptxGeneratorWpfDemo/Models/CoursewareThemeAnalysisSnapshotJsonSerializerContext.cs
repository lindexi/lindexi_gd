using System.Text.Json.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Provides source-generated JSON metadata for theme-analysis snapshot manifests.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
    WriteIndented = true)]
[JsonSerializable(typeof(CoursewareThemeAnalysisSnapshotManifest))]
public sealed partial class CoursewareThemeAnalysisSnapshotJsonSerializerContext : JsonSerializerContext;
