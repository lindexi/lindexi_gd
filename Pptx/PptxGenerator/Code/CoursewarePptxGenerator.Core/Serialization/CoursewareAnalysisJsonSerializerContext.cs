using System.Text.Json.Serialization;
using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Serialization;

/// <summary>
/// Provides source-generated JSON metadata for immutable analysis facts and model-facing input diagnostics.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(CoursewareAnalysisEnvelope))]
[JsonSerializable(typeof(CoursewareAnalysisTaskSection))]
[JsonSerializable(typeof(CoursewareAnalysisOverviewSection))]
[JsonSerializable(typeof(CoursewareAnalysisDistributionItem[]))]
[JsonSerializable(typeof(CoursewareAnalysisWarningView[]))]
[JsonSerializable(typeof(CoursewareAnalysisResourceView[]))]
[JsonSerializable(typeof(CoursewareAnalysisSlideView[]))]
[JsonSerializable(typeof(CoursewareAnalysisOutputRequirementsSection))]
public sealed partial class CoursewareAnalysisJsonSerializerContext : JsonSerializerContext;
