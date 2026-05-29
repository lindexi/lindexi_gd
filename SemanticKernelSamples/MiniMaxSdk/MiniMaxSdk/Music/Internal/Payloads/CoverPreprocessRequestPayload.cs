using System.Text.Json.Serialization;
using MiniMaxSdk.Music.Models;

namespace MiniMaxSdk.Music.Internal.Payloads;

internal sealed record CoverPreprocessRequestPayload(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("audio_url")] string? AudioUrl,
    [property: JsonPropertyName("audio_base64")] string? AudioBase64)
{
    public static CoverPreprocessRequestPayload FromRequest(MiniMaxCoverPreprocessRequest request)
    {
        return new CoverPreprocessRequestPayload(request.Model, request.AudioUrl, request.AudioBase64);
    }
}
