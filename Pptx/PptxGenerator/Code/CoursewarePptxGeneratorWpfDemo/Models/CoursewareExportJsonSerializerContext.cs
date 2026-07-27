using System.Text.Json.Serialization;
using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Provides source-generated JSON metadata for courseware export files.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip)]
[JsonSerializable(typeof(CoursewareTheme))]
[JsonSerializable(typeof(CoursewareThemeAnalysisResult))]
[JsonSerializable(typeof(CoursewareThemeAnalysisSnapshotManifest))]
[JsonSerializable(typeof(CoursewareColorSuggestion))]
[JsonSerializable(typeof(CoursewareFontSuggestions))]
[JsonSerializable(typeof(CoursewareSafeAreaRatios))]
[JsonSerializable(typeof(CoursewareExportManifest))]
[JsonSerializable(typeof(CoursewareExportSlideEntry))]
[JsonSerializable(typeof(CoursewareResourceEntry[]))]
public sealed partial class CoursewareExportJsonSerializerContext : JsonSerializerContext;
