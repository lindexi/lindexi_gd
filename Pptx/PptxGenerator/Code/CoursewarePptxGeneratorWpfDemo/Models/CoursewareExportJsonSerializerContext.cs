using System.Text.Json.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Provides source-generated JSON metadata for courseware export files.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip)]
[JsonSerializable(typeof(CoursewareTheme))]
[JsonSerializable(typeof(CoursewareThemeAnalysisResult))]
public sealed partial class CoursewareExportJsonSerializerContext : JsonSerializerContext;
