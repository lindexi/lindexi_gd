using System.Text.Json.Serialization;
using MiniMaxSdk.Music.Models;

namespace MiniMaxSdk.Music.Internal.Payloads;

internal sealed record MusicGenerationRequestPayload(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string? Prompt,
    [property: JsonPropertyName("lyrics")] string? Lyrics,
    [property: JsonPropertyName("stream")] bool Stream,
    [property: JsonPropertyName("output_format")] string OutputFormat,
    [property: JsonPropertyName("audio_setting")] MusicAudioSettingPayload? AudioSetting,
    [property: JsonPropertyName("aigc_watermark")] bool? AigcWatermark,
    [property: JsonPropertyName("lyrics_optimizer")] bool LyricsOptimizer,
    [property: JsonPropertyName("is_instrumental")] bool IsInstrumental,
    [property: JsonPropertyName("audio_url")] string? AudioUrl,
    [property: JsonPropertyName("audio_base64")] string? AudioBase64,
    [property: JsonPropertyName("cover_feature_id")] string? CoverFeatureId)
{
    public static MusicGenerationRequestPayload FromRequest(MiniMaxMusicGenerationRequest request)
    {
        return new MusicGenerationRequestPayload(
            request.Model,
            request.Prompt,
            request.Lyrics,
            request.Stream,
            request.OutputFormat,
            request.AudioSetting is null ? null : MusicAudioSettingPayload.FromModel(request.AudioSetting),
            request.AigcWatermark,
            request.LyricsOptimizer,
            request.IsInstrumental,
            request.AudioUrl,
            request.AudioBase64,
            request.CoverFeatureId);
    }
}
