using System.Text.Json.Serialization;
using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Serialization;

/// <summary>
/// Provides source-generated JSON metadata for the versioned courseware design contract.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(CoursewareDesignAnalysisResult))]
[JsonSerializable(typeof(CoursewareDesignSystem))]
[JsonSerializable(typeof(CoursewareAnalysisCapabilityStates))]
[JsonSerializable(typeof(CoursewareEvidenceReference))]
[JsonSerializable(typeof(CoursewareDesignDecisionEvidence))]
[JsonSerializable(typeof(CoursewareDesignAssumption))]
[JsonSerializable(typeof(CoursewareDesignIntent))]
[JsonSerializable(typeof(CoursewareCanvasDesignProfile[]))]
[JsonSerializable(typeof(CoursewareSpacingToken[]))]
[JsonSerializable(typeof(CoursewareTypographyToken[]))]
[JsonSerializable(typeof(CoursewareColorToken[]))]
[JsonSerializable(typeof(CoursewareEffectToken[]))]
[JsonSerializable(typeof(CoursewareComponentSpecification[]))]
[JsonSerializable(typeof(CoursewareAssetUsageRule[]))]
[JsonSerializable(typeof(CoursewarePageTypeContract[]))]
[JsonSerializable(typeof(CoursewarePageTypeAssignment[]))]
[JsonSerializable(typeof(CoursewarePageTemplate[]))]
[JsonSerializable(typeof(CoursewareStructuredFactReport))]
[JsonSerializable(typeof(CoursewareSlideStructuredFacts[]))]
[JsonSerializable(typeof(CoursewareLayoutCluster[]))]
[JsonSerializable(typeof(CoursewareVisualAnalysisReport))]
[JsonSerializable(typeof(CoursewareVisualSample[]))]
[JsonSerializable(typeof(CoursewareVisualObservation[]))]
[JsonSerializable(typeof(CoursewareDesignSystemValidationReport))]
[JsonSerializable(typeof(CoursewareTemplateValidationReport))]
[JsonSerializable(typeof(CoursewareTemplateStressSampleResult[]))]
[JsonSerializable(typeof(CoursewareValidationDiagnostic[]))]
public sealed partial class CoursewareDesignJsonSerializerContext : JsonSerializerContext;
