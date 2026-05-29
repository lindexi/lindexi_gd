using System.Text.Json.Serialization;

namespace MiniMaxSdk;

internal sealed record MetadataPayload(
    [property: JsonPropertyName("success_count")] int SuccessCount,
    [property: JsonPropertyName("failed_count")] int FailedCount);