using System.Text.Json.Serialization;

namespace MiniMaxSdk.Music.Internal.Payloads;

internal sealed record MusicDataPayload(
    [property: JsonPropertyName("status")] int? Status,
    [property: JsonPropertyName("audio")] string? Audio);
