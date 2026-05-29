using System.Text.Json.Serialization;

namespace MiniMaxSdk;

internal sealed record BaseResponsePayload(
    [property: JsonPropertyName("status_code")] int StatusCode,
    [property: JsonPropertyName("status_msg")] string? StatusMessage);