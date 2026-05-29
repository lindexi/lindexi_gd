using System.Text.Json.Serialization;

namespace MiniMaxSdk.Images.Internal.Payloads;

internal sealed record MetadataPayload(
    [property: JsonPropertyName("success_count")] int SuccessCount,
    [property: JsonPropertyName("failed_count")] int FailedCount);