using System.Text.Json.Serialization;

namespace MiniMaxSdk.Music.Internal.Payloads;

internal sealed record CoverPreprocessResponsePayload(
    [property: JsonPropertyName("cover_feature_id")] string? CoverFeatureId,
    [property: JsonPropertyName("formatted_lyrics")] string? FormattedLyrics,
    [property: JsonPropertyName("structure_result")] string? StructureResult,
    [property: JsonPropertyName("audio_duration")] double? AudioDuration,
    [property: JsonPropertyName("trace_id")] string? TraceId,
    [property: JsonPropertyName("base_resp")] BaseResponsePayload? BaseResp);
