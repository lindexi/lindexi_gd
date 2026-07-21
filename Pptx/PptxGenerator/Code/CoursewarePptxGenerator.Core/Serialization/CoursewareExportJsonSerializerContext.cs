using System.Text.Json.Serialization;
using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Serialization;

/// <summary>
/// Provides source-generated JSON metadata for courseware export files.
/// </summary>
[JsonSourceGenerationOptions]
[JsonSerializable(typeof(CoursewareExportManifest))]
[JsonSerializable(typeof(CoursewareExportSlideEntry))]
[JsonSerializable(typeof(CoursewareResourceEntry))]
[JsonSerializable(typeof(CoursewareResourceEntry[]))]
public sealed partial class CoursewareExportJsonSerializerContext : JsonSerializerContext;
