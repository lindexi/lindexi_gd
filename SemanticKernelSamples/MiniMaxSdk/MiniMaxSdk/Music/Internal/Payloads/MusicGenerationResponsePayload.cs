using System.Text.Json.Serialization;

namespace MiniMaxSdk.Music.Internal.Payloads;

internal sealed record MusicGenerationResponsePayload(
    [property: JsonPropertyName("data")] MusicDataPayload? Data,
    [property: JsonPropertyName("base_resp")] BaseResponsePayload? BaseResp);
