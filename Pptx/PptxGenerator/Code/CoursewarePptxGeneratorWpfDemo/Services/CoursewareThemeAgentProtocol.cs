using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoursewarePptxGeneratorWpfDemo.Services;

internal sealed record CoursewareThemeRepairEnvelope
{
    internal const string CurrentSchemaVersion = "courseware-theme-repair/v1";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string Objective { get; init; } = string.Empty;

    public IReadOnlyList<string> ValidationErrors { get; init; } = [];

    public IReadOnlyList<string> Requirements { get; init; } = [];

    public JsonElement OriginalAnalysisEnvelope { get; init; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(CoursewareThemeRepairEnvelope))]
internal sealed partial class CoursewareThemeAgentJsonSerializerContext : JsonSerializerContext;
