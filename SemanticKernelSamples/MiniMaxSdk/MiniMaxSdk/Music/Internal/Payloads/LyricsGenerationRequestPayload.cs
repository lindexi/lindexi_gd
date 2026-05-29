using System.Text.Json.Serialization;
using MiniMaxSdk.Music.Models;

namespace MiniMaxSdk.Music.Internal.Payloads;

internal sealed record LyricsGenerationRequestPayload(
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("prompt")] string? Prompt,
    [property: JsonPropertyName("lyrics")] string? Lyrics,
    [property: JsonPropertyName("title")] string? Title)
{
    public static LyricsGenerationRequestPayload FromRequest(MiniMaxLyricsGenerationRequest request)
    {
        return new LyricsGenerationRequestPayload(request.Mode, request.Prompt, request.Lyrics, request.Title);
    }
}
