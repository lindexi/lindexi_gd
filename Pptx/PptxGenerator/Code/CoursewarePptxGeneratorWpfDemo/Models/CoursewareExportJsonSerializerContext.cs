using System.Text.Json.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Provides source-generated JSON metadata for courseware export files.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip)]
[JsonSerializable(typeof(CoursewareExportManifest))]
[JsonSerializable(typeof(CoursewareExportSlideEntry))]
[JsonSerializable(typeof(CoursewareResourceManifest))]
[JsonSerializable(typeof(CoursewareResourceEntry))]
[JsonSerializable(typeof(CoursewareResourceEntry[]))]
public sealed partial class CoursewareExportJsonSerializerContext : JsonSerializerContext;
