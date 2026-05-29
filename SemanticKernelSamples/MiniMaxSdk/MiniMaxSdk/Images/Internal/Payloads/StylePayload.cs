using System.Text.Json.Serialization;

namespace MiniMaxSdk.Images.Internal.Payloads;

internal sealed record StylePayload(
    [property: JsonPropertyName("style_type")] string StyleType,
    [property: JsonPropertyName("style_weight")] float? StyleWeight);