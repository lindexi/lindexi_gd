using System.Text.Json.Serialization;
using OllamaHubLogViewer.Models;

namespace OllamaHubLogViewer.Services;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(LogMergeManifest))]
[JsonSerializable(typeof(LogMergeIndex))]
internal sealed partial class LogMergeJsonContext : JsonSerializerContext
{
}
