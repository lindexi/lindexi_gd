using System.Text.Json.Serialization;

namespace MiniMaxSdk.Images.Internal.Payloads;

internal sealed record BaseResponsePayload(
    [property: JsonPropertyName("status_code")] int StatusCode,
    [property: JsonPropertyName("status_msg")] string? StatusMessage);