using System.Text.Json.Serialization;
using MiniMaxSdk.Music.Models;

namespace MiniMaxSdk.Music.Internal.Payloads;

internal sealed record MusicAudioSettingPayload(
    [property: JsonPropertyName("sample_rate")] int? SampleRate,
    [property: JsonPropertyName("bitrate")] int? Bitrate,
    [property: JsonPropertyName("format")] string? Format)
{
    public static MusicAudioSettingPayload FromModel(MiniMaxMusicAudioSetting setting)
    {
        return new MusicAudioSettingPayload(setting.SampleRate, setting.Bitrate, setting.Format);
    }
}
