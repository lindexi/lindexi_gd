using System.Text.Json.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Provides source-generated JSON metadata for page-generation request envelopes.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(CoursewareSlideGenerationEnvelope))]
public sealed partial class CoursewareSlideGenerationJsonSerializerContext : JsonSerializerContext;
