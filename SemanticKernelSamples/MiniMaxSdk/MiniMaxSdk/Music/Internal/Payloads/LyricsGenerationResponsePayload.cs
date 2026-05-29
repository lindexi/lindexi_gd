using System.Text.Json.Serialization;

namespace MiniMaxSdk.Music.Internal.Payloads;

internal sealed record LyricsGenerationResponsePayload(
    [property: JsonPropertyName("song_title")] string? SongTitle,
    [property: JsonPropertyName("style_tags")] string? StyleTags,
    [property: JsonPropertyName("lyrics")] string? Lyrics,
    [property: JsonPropertyName("base_resp")] BaseResponsePayload? BaseResp);
